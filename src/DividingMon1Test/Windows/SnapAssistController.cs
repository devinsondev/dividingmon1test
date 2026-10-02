using System.Drawing;
using DividingMon1Test.Geometry;
using DividingMon1Test.UI;

namespace DividingMon1Test.Windows;

internal sealed class SnapAssistController : IDisposable
{
    private readonly SnapAssistPickerForm _picker = new();
    private readonly HashSet<IntPtr> _usedWindows = [];

    private IReadOnlyList<Rectangle> _slots = [];
    private int _slotIndex;
    private bool _enabled = true;
    private bool _disposed;

    internal SnapAssistController()
    {
        _picker.WindowChosen += OnWindowChosen;
        _picker.CancelRequested += Cancel;
    }

    internal bool Enabled
    {
        get => _enabled;
        set
        {
            _enabled = value;
            if (!value)
            {
                Cancel();
            }
        }
    }

    internal void Start(SnapTarget sourceTarget, IntPtr sourceWindow)
    {
        Cancel();

        if (!_enabled || _disposed || sourceWindow == IntPtr.Zero)
        {
            return;
        }

        _slots = SnapAssistLayout.GetRemainingSlots(sourceTarget);
        if (_slots.Count == 0)
        {
            return;
        }

        _usedWindows.Add(sourceWindow);
        _slotIndex = 0;
        ShowCurrentSlot();
    }

    internal void Cancel()
    {
        _picker.HidePicker();
        _slots = [];
        _slotIndex = 0;
        _usedWindows.Clear();
    }

    private void OnWindowChosen(IntPtr window)
    {
        if (_disposed || _slotIndex >= _slots.Count || window == IntPtr.Zero)
        {
            Cancel();
            return;
        }

        var destination = _slots[_slotIndex];
        _usedWindows.Add(window);

        if (WindowSnapper.TrySnap(window, destination))
        {
            _slotIndex++;
        }

        ShowCurrentSlot();
    }

    private void ShowCurrentSlot()
    {
        if (_slotIndex >= _slots.Count)
        {
            Cancel();
            return;
        }

        var candidates = WindowCatalog.GetCandidates(_usedWindows);
        if (candidates.Count == 0)
        {
            Cancel();
            return;
        }

        _picker.ShowCandidates(_slots[_slotIndex], candidates);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _picker.WindowChosen -= OnWindowChosen;
        _picker.CancelRequested -= Cancel;
        Cancel();
        _picker.Dispose();
    }
}
