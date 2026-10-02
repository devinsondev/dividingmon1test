using System.Drawing.Drawing2D;
using DividingMon1Test.Geometry;

namespace DividingMon1Test.UI;

internal sealed class PreviewOverlayForm : Form
{
    private const int WsExTransparent = 0x00000020;
    private const int WsExToolWindow = 0x00000080;
    private const int WsExNoActivate = 0x08000000;

    private SnapTarget? _target;

    internal PreviewOverlayForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;
        BackColor = Color.Black;
        Opacity = 0.22;
        DoubleBuffered = true;
    }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            var parameters = base.CreateParams;
            parameters.ExStyle |= WsExTransparent | WsExToolWindow | WsExNoActivate;
            return parameters;
        }
    }

    internal void ShowPreview(Rectangle monitorBounds, SnapTarget target)
    {
        _target = target;

        if (Bounds != monitorBounds)
        {
            Bounds = monitorBounds;
        }

        if (!Visible)
        {
            Show();
        }

        Invalidate();
    }

    internal void HidePreview()
    {
        _target = null;
        if (Visible)
        {
            Hide();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        if (_target is not { } target)
        {
            return;
        }

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        using var gridPen = new Pen(Color.White, 2f)
        {
            DashStyle = DashStyle.Dash
        };

        var middleX = ClientSize.Width / 2;
        var middleY = ClientSize.Height / 2;

        e.Graphics.DrawLine(gridPen, middleX, 0, middleX, ClientSize.Height);
        e.Graphics.DrawLine(gridPen, 0, middleY, ClientSize.Width, middleY);

        var local = new Rectangle(
            target.Destination.X - Bounds.Left,
            target.Destination.Y - Bounds.Top,
            target.Destination.Width,
            target.Destination.Height);

        local.Inflate(-4, -4);

        using var fillBrush = new SolidBrush(SystemColors.Highlight);
        using var borderPen = new Pen(Color.White, 4f);

        e.Graphics.FillRectangle(fillBrush, local);
        e.Graphics.DrawRectangle(borderPen, local);

        TextRenderer.DrawText(
            e.Graphics,
            target.Label,
            SystemFonts.MessageBoxFont,
            local,
            Color.White,
            TextFormatFlags.HorizontalCenter |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.EndEllipsis);
    }
}
