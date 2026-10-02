using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using DividingMon1Test.Display;
using DividingMon1Test.Geometry;
using DividingMon1Test.Interop;
using DividingMon1Test.UI;

namespace DividingMon1Test.Windows;

internal sealed class WindowDragController : IDisposable
{
    private const int SideResizeBand = 8;
    private const int TopResizeBand = 4;

    private readonly TargetDisplayService _displays;
    private readonly PreviewOverlayForm _overlay;
    private readonly NativeMethods.WinEventProc _winEventProc;
    private readonly System.Windows.Forms.Timer _pollTimer;

    private IntPtr _hook;
    private IntPtr _movingWindow;
    private bool _enabled = true;
    private bool _disposed;
    private int _edgeThreshold = SnapLayout.DefaultEdgeThreshold;

    internal WindowDragController(
        TargetDisplayService displays,
        PreviewOverlayForm overlay)
    {
        _displays = displays;
        _overlay = overlay;

        _pollTimer = new System.Windows.Forms.Timer
        {
            Interval = 16
        };
        _pollTimer.Tick += PollTimerOnTick;

        _winEventProc = OnWinEvent;
        _hook = NativeMethods.SetWinEventHook(
            NativeMethods.EventSystemMoveSizeStart,
            NativeMethods.EventSystemMoveSizeEnd,
            IntPtr.Zero,
            _winEventProc,
            0,
            0,
            NativeMethods.WinEventOutOfContext | NativeMethods.WinEventSkipOwnProcess);

        if (_hook == IntPtr.Zero)
        {
            _pollTimer.Dispose();
            throw new Win32Exception(Marshal.GetLastWin32Error(), "SetWinEventHook failed.");
        }
    }

    internal bool Enabled
    {
        get => _enabled;
        set
        {
            if (_enabled == value)
            {
                return;
            }

            _enabled = value;
            if (!value)
            {
                CancelTracking();
            }
        }
    }

    internal int EdgeThreshold
    {
        get => _edgeThreshold;
        set => _edgeThreshold = Math.Clamp(value, 16, 96);
    }

    private void OnWinEvent(
        IntPtr hook,
        uint eventType,
        IntPtr window,
        int objectId,
        int childId,
        uint eventThread,
        uint eventTime)
    {
        _ = hook;
        _ = eventThread;
        _ = eventTime;

        if (_disposed ||
            !_enabled ||
            window == IntPtr.Zero ||
            objectId != NativeMethods.ObjIdWindow ||
            childId != NativeMethods.ChildIdSelf)
        {
            return;
        }

        if (eventType == NativeMethods.EventSystemMoveSizeStart)
        {
            BeginTracking(window);
        }
        else if (eventType == NativeMethods.EventSystemMoveSizeEnd && window == _movingWindow)
        {
            EndTracking(window);
        }
    }

    private void BeginTracking(IntPtr window)
    {
        CancelTracking();

        if (!NativeMethods.IsWindowVisible(window) || LooksLikeBorderResize(window))
        {
            return;
        }

        _movingWindow = window;
        _pollTimer.Start();
        UpdatePreview();
    }

    private void EndTracking(IntPtr window)
    {
        _pollTimer.Stop();

        var snap = ResolveCurrentTarget(out _);
        _overlay.HidePreview();
        _movingWindow = IntPtr.Zero;

        if (snap is { } target)
        {
            WindowSnapper.TrySnap(window, target.Destination);
        }
    }

    private void PollTimerOnTick(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        var snap = ResolveCurrentTarget(out var monitorBounds);
        if (snap is not { } target)
        {
            _overlay.HidePreview();
            return;
        }

        _overlay.ShowPreview(monitorBounds, target);
    }

    private SnapTarget? ResolveCurrentTarget(out Rectangle monitorBounds)
    {
        monitorBounds = Rectangle.Empty;

        if (_movingWindow == IntPtr.Zero ||
            !NativeMethods.IsWindow(_movingWindow) ||
            !NativeMethods.GetCursorPos(out var nativePoint))
        {
            return null;
        }

        var screen = _displays.GetTarget();
        if (screen is null)
        {
            return null;
        }

        monitorBounds = screen.Bounds;
        return SnapLayout.TryResolve(
            monitorBounds,
            nativePoint.ToPoint(),
            _edgeThreshold,
            out var target)
            ? target
            : null;
    }

    private static bool LooksLikeBorderResize(IntPtr window)
    {
        if (!NativeMethods.GetCursorPos(out var nativePoint) ||
            !NativeMethods.GetWindowRect(window, out var nativeRect))
        {
            return true;
        }

        var point = nativePoint.ToPoint();
        var rect = nativeRect.ToRectangle();

        var nearLeft = Math.Abs(point.X - rect.Left) <= SideResizeBand;
        var nearRight = Math.Abs(point.X - (rect.Right - 1)) <= SideResizeBand;
        var nearBottom = Math.Abs(point.Y - (rect.Bottom - 1)) <= SideResizeBand;
        var nearTop = Math.Abs(point.Y - rect.Top) <= TopResizeBand;

        return nearLeft || nearRight || nearBottom || nearTop;
    }

    private void CancelTracking()
    {
        _pollTimer.Stop();
        _overlay.HidePreview();
        _movingWindow = IntPtr.Zero;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        CancelTracking();
        _pollTimer.Dispose();

        if (_hook != IntPtr.Zero)
        {
            NativeMethods.UnhookWinEvent(_hook);
            _hook = IntPtr.Zero;
        }
    }
}
