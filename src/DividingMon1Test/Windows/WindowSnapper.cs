using System.Drawing;
using DividingMon1Test.Interop;

namespace DividingMon1Test.Windows;

internal static class WindowSnapper
{
    internal static bool TrySnap(IntPtr window, Rectangle destination)
    {
        if (window == IntPtr.Zero ||
            destination.Width <= 0 ||
            destination.Height <= 0 ||
            !NativeMethods.IsWindow(window) ||
            !NativeMethods.IsWindowVisible(window))
        {
            return false;
        }

        if (NativeMethods.IsZoomed(window))
        {
            NativeMethods.ShowWindow(window, NativeMethods.SwRestore);
        }

        var flags =
            NativeMethods.SwpNoZOrder |
            NativeMethods.SwpNoActivate |
            NativeMethods.SwpNoOwnerZOrder |
            NativeMethods.SwpAsyncWindowPos;

        return NativeMethods.SetWindowPos(
            window,
            IntPtr.Zero,
            destination.X,
            destination.Y,
            destination.Width,
            destination.Height,
            flags);
    }
}
