using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using DividingMon1Test.Interop;

namespace DividingMon1Test.Windows;

internal readonly record struct WindowCandidate(IntPtr Handle, string Title);

internal static class WindowCatalog
{
    private const int MinimumWindowWidth = 80;
    private const int MinimumWindowHeight = 60;

    internal static IReadOnlyList<WindowCandidate> GetCandidates(IReadOnlySet<IntPtr> excluded)
    {
        var result = new List<WindowCandidate>();
        var ownProcessId = (uint)Environment.ProcessId;

        NativeMethods.EnumWindows(
            (window, _) =>
            {
                if (IsCandidate(window, ownProcessId, excluded, out var title))
                {
                    result.Add(new WindowCandidate(window, title));
                }

                return true;
            },
            IntPtr.Zero);

        return result;
    }

    private static bool IsCandidate(
        IntPtr window,
        uint ownProcessId,
        IReadOnlySet<IntPtr> excluded,
        out string title)
    {
        title = string.Empty;

        if (window == IntPtr.Zero ||
            excluded.Contains(window) ||
            !NativeMethods.IsWindow(window) ||
            !NativeMethods.IsWindowVisible(window) ||
            NativeMethods.GetWindow(window, NativeMethods.GwOwner) != IntPtr.Zero)
        {
            return false;
        }

        NativeMethods.GetWindowThreadProcessId(window, out var processId);
        if (processId == 0 || processId == ownProcessId)
        {
            return false;
        }

        if (IsCloaked(window) ||
            !NativeMethods.GetWindowRect(window, out var nativeRect))
        {
            return false;
        }

        var rect = nativeRect.ToRectangle();
        if (rect.Width < MinimumWindowWidth || rect.Height < MinimumWindowHeight)
        {
            return false;
        }

        var length = NativeMethods.GetWindowTextLengthW(window);
        if (length <= 0)
        {
            return false;
        }

        var builder = new StringBuilder(length + 1);
        if (NativeMethods.GetWindowTextW(window, builder, builder.Capacity) <= 0)
        {
            return false;
        }

        title = builder.ToString().Trim();
        return title.Length > 0;
    }

    private static bool IsCloaked(IntPtr window)
    {
        var result = DwmMethods.DwmGetWindowAttribute(
            window,
            DwmMethods.DwmWaCloaked,
            out var cloaked,
            Marshal.SizeOf<int>());

        return result >= 0 && cloaked != 0;
    }
}
