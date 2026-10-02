using System.ComponentModel;
using DividingMon1Test.Display;
using DividingMon1Test.Windows;

namespace DividingMon1Test.UI;

internal sealed class TrayApplicationContext : ApplicationContext
{
    private readonly TargetDisplayService _displays = new();
    private readonly PreviewOverlayForm _overlay = new();
    private readonly ContextMenuStrip _menu = new();
    private readonly ToolStripMenuItem _enabledItem;
    private readonly ToolStripMenuItem _targetMenu;
    private readonly ToolStripMenuItem _sensitivityMenu;
    private readonly NotifyIcon _notifyIcon;

    private WindowDragController? _controller;

    internal TrayApplicationContext()
    {
        var title = new ToolStripMenuItem("DividingMon1Test — 4 logical zones")
        {
            Enabled = false
        };

        _enabledItem = new ToolStripMenuItem("Enabled")
        {
            CheckOnClick = true,
            Checked = true
        };
        _enabledItem.CheckedChanged += EnabledItemOnCheckedChanged;

        _targetMenu = new ToolStripMenuItem("Target display");
        _targetMenu.DropDownOpening += TargetMenuOnDropDownOpening;

        _sensitivityMenu = new ToolStripMenuItem("Edge sensitivity");
        AddSensitivityItem("24 px — precise", 24);
        AddSensitivityItem("48 px — normal", 48);
        AddSensitivityItem("72 px — easy", 72);

        var exitItem = new ToolStripMenuItem("Exit");
        exitItem.Click += ExitItemOnClick;

        _menu.Items.Add(title);
        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add(_enabledItem);
        _menu.Items.Add(_targetMenu);
        _menu.Items.Add(_sensitivityMenu);
        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add(exitItem);

        _notifyIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "DividingMon1Test",
            ContextMenuStrip = _menu,
            Visible = true
        };

        try
        {
            _controller = new WindowDragController(_displays, _overlay);
            _controller.EdgeThreshold = 48;
            UpdateNotifyText();
        }
        catch (Win32Exception exception)
        {
            _enabledItem.Checked = false;
            _enabledItem.Enabled = false;
            _notifyIcon.BalloonTipTitle = "DividingMon1Test could not start";
            _notifyIcon.BalloonTipText = exception.Message;
            _notifyIcon.ShowBalloonTip(5000);
        }
    }

    private void AddSensitivityItem(string text, int threshold)
    {
        var item = new ToolStripMenuItem(text)
        {
            Tag = threshold,
            Checked = threshold == 48
        };

        item.Click += (_, _) =>
        {
            if (_controller is null)
            {
                return;
            }

            _controller.EdgeThreshold = threshold;
            foreach (ToolStripMenuItem sibling in _sensitivityMenu.DropDownItems.OfType<ToolStripMenuItem>())
            {
                sibling.Checked = ReferenceEquals(sibling, item);
            }
        };

        _sensitivityMenu.DropDownItems.Add(item);
    }

    private void EnabledItemOnCheckedChanged(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;

        if (_controller is not null)
        {
            _controller.Enabled = _enabledItem.Checked;
        }
    }

    private void TargetMenuOnDropDownOpening(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;

        DisposeTargetMenuItems();

        var selected = _displays.GetTarget()?.DeviceName;
        foreach (var screen in TargetDisplayService.GetScreens())
        {
            var deviceName = screen.DeviceName;
            var item = new ToolStripMenuItem(TargetDisplayService.Describe(screen))
            {
                Checked = string.Equals(
                    selected,
                    deviceName,
                    StringComparison.OrdinalIgnoreCase)
            };

            item.Click += (_, _) =>
            {
                if (_displays.TrySelect(deviceName))
                {
                    UpdateNotifyText();
                }
            };

            _targetMenu.DropDownItems.Add(item);
        }

        if (_targetMenu.DropDownItems.Count == 0)
        {
            _targetMenu.DropDownItems.Add(new ToolStripMenuItem("No displays found")
            {
                Enabled = false
            });
        }
    }

    private void UpdateNotifyText()
    {
        var target = _displays.GetTarget();
        _notifyIcon.Text = target is null
            ? "DividingMon1Test - no target"
            : $"DividingMon1Test - {target.DeviceName}";
    }

    private void ExitItemOnClick(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        ExitThread();
    }

    private void DisposeTargetMenuItems()
    {
        foreach (ToolStripItem item in _targetMenu.DropDownItems)
        {
            item.Dispose();
        }

        _targetMenu.DropDownItems.Clear();
    }

    protected override void ExitThreadCore()
    {
        _notifyIcon.Visible = false;
        _controller?.Dispose();
        _overlay.Dispose();
        DisposeTargetMenuItems();
        _menu.Dispose();
        _notifyIcon.Dispose();
        base.ExitThreadCore();
    }
}
