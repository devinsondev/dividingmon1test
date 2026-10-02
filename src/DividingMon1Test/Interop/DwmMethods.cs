using System.Runtime.InteropServices;

namespace DividingMon1Test.Interop;

internal static class DwmMethods
{
    internal const uint DwmTnpRectDestination = 0x00000001;
    internal const uint DwmTnpOpacity = 0x00000004;
    internal const uint DwmTnpVisible = 0x00000008;
    internal const uint DwmTnpSourceClientAreaOnly = 0x00000010;
    internal const uint DwmWaCloaked = 14;

    [StructLayout(LayoutKind.Sequential)]
    internal struct NativeSize
    {
        internal int Width;
        internal int Height;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct ThumbnailProperties
    {
        internal uint Flags;
        internal NativeMethods.NativeRect Destination;
        internal NativeMethods.NativeRect Source;
        internal byte Opacity;

        [MarshalAs(UnmanagedType.Bool)]
        internal bool Visible;

        [MarshalAs(UnmanagedType.Bool)]
        internal bool SourceClientAreaOnly;
    }

    [DllImport("dwmapi.dll", ExactSpelling = true)]
    internal static extern int DwmRegisterThumbnail(
        IntPtr destination,
        IntPtr source,
        out IntPtr thumbnail);

    [DllImport("dwmapi.dll", ExactSpelling = true)]
    internal static extern int DwmUnregisterThumbnail(IntPtr thumbnail);

    [DllImport("dwmapi.dll", ExactSpelling = true)]
    internal static extern int DwmUpdateThumbnailProperties(
        IntPtr thumbnail,
        ref ThumbnailProperties properties);

    [DllImport("dwmapi.dll", ExactSpelling = true)]
    internal static extern int DwmQueryThumbnailSourceSize(
        IntPtr thumbnail,
        out NativeSize size);

    [DllImport("dwmapi.dll", ExactSpelling = true)]
    internal static extern int DwmGetWindowAttribute(
        IntPtr window,
        uint attribute,
        out int value,
        int valueSize);
}
