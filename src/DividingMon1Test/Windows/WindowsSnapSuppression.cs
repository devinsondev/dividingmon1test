using DividingMon1Test.Interop;

namespace DividingMon1Test.Windows;

internal sealed class WindowsSnapSuppression : IDisposable
{
    private bool _enabled = true;
    private bool _active;
    private bool _restoreDockMoving;
    private bool _apiAvailable = true;
    private bool _disposed;

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
                Release();
            }
        }
    }

    internal void Update(bool pointerOnTargetDisplay)
    {
        if (_disposed || !_enabled || !pointerOnTargetDisplay)
        {
            Release();
            return;
        }

        Acquire();
    }

    internal void Release()
    {
        if (!_active)
        {
            return;
        }

        if (_restoreDockMoving && _apiAvailable)
        {
            _apiAvailable = NativeMethods.SystemParametersInfoSet(
                NativeMethods.SpiSetDockMoving,
                0,
                new IntPtr(1),
                0);
        }

        _active = false;
        _restoreDockMoving = false;
    }

    private void Acquire()
    {
        if (_active || !_apiAvailable)
        {
            return;
        }

        var dockMoving = 0;
        _apiAvailable = NativeMethods.SystemParametersInfoGet(
            NativeMethods.SpiGetDockMoving,
            0,
            ref dockMoving,
            0);

        if (!_apiAvailable)
        {
            return;
        }

        _restoreDockMoving = dockMoving != 0;
        if (_restoreDockMoving)
        {
            _apiAvailable = NativeMethods.SystemParametersInfoSet(
                NativeMethods.SpiSetDockMoving,
                0,
                IntPtr.Zero,
                0);

            if (!_apiAvailable)
            {
                _restoreDockMoving = false;
                return;
            }
        }

        _active = true;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Release();
        _disposed = true;
    }
}
