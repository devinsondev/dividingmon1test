using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;

[assembly: DefaultDllImportSearchPaths(DllImportSearchPath.System32)]

namespace DividingMon1Test.Interop;

internal static class NativeMethods
{
    internal const uint EventSystemMoveSizeStart = 0x000A;
    internal const uint EventSystemMoveSizeEnd = 0x000B;
    internal const uint WinEventOutOfContext = 0x0000;
    internal const uint WinEventSkipOwnProcess = 0x0002;
    internal const int ObjIdWindow = 0;
    internal const int ChildIdSelf = 0;
    internal const uint GwOwner = 4;

    internal const int SwRestore = 9;
    internal const uint SwpNoZOrder = 0x0004;
    internal const uint SwpNoActivate = 0x0010;
    internal const uint SwpNoOwnerZOrder = 0x0200;
    internal const uint SwpAsyncWindowPos = 0x4000;

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate void WinEventProc(
        IntPtr hook,
        uint eventType,
        IntPtr window,
        int objectId,
        int childId,
        uint eventThread,
        uint eventTime);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal delegate bool EnumWindowsProc(IntPtr window, IntPtr parameter);

    [StructLayout(LayoutKind.Sequential)]
    internal struct NativePoint
    {
        internal int X;
        internal int Y;

        internal readonly Point ToPoint() => new(X, Y);
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct NativeRect
    {
        internal int Left;
        internal int Top;
        internal int Right;
        internal int Bottom;

        internal readonly Rectangle ToRectangle() => Rectangle.FromLTRB(Left, Top, Right, Bottom);

        internal static NativeRect FromRectangle(Rectangle rectangle)
        {
            return new NativeRect
            {
                Left = rectangle.Left,
                Top = rectangle.Top,
                Right = rectangle.Right,
                Bottom = rectangle.Bottom
            };
        }
    }

    [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
    internal static extern IntPtr SetWinEventHook(
        uint eventMin,
        uint eventMax,
        IntPtr eventHookModule,
        WinEventProc eventProc,
        uint processId,
        uint threadId,
        uint flags);

    [DllImport("user32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool UnhookWinEvent(IntPtr hook);

    [DllImport("user32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool EnumWindows(EnumWindowsProc callback, IntPtr parameter);

    [DllImport("user32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetCursorPos(out NativePoint point);

    [DllImport("user32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetWindowRect(IntPtr window, out NativeRect rect);

    [DllImport("user32.dll", ExactSpelling = true)]
    internal static extern IntPtr GetWindow(IntPtr window, uint command);

    [DllImport("user32.dll", ExactSpelling = true)]
    internal static extern int GetWindowTextLengthW(IntPtr window);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    internal static extern int GetWindowTextW(
        IntPtr window,
        StringBuilder text,
        int maximumCount);

    [DllImport("user32.dll", ExactSpelling = true)]
    internal static extern uint GetWindowThreadProcessId(
        IntPtr window,
        out uint processId);

    [DllImport("user32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool IsWindow(IntPtr window);

    [DllImport("user32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool IsWindowVisible(IntPtr window);

    [DllImport("user32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool IsZoomed(IntPtr window);

    [DllImport("user32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool ShowWindow(IntPtr window, int command);

    [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetWindowPos(
        IntPtr window,
        IntPtr insertAfter,
        int x,
        int y,
        int width,
        int height,
        uint flags);
}
