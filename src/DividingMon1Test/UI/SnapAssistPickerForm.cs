using System.Drawing;
using DividingMon1Test.Interop;
using DividingMon1Test.Windows;

namespace DividingMon1Test.UI;

internal sealed class SnapAssistPickerForm : Form
{
    private const int HeaderHeight = 34;
    private const int OuterPadding = 8;
    private const int CardGap = 8;
    private const int CardTitleHeight = 24;
    private const int MaxCardsPerPage = 4;

    private readonly List<Card> _cards = [];
    private readonly List<IntPtr> _thumbnails = [];
    private IReadOnlyList<WindowCandidate> _candidates = [];
    private int _page;
    private Rectangle _previousButton;
    private Rectangle _nextButton;

    internal event Action<IntPtr>? WindowChosen;
    internal event Action? CancelRequested;

    internal SnapAssistPickerForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;
        KeyPreview = true;
        BackColor = Color.FromArgb(28, 28, 28);
        ForeColor = Color.White;
        DoubleBuffered = true;

        KeyDown += OnPickerKeyDown;
        MouseDown += OnPickerMouseDown;
        MouseWheel += OnPickerMouseWheel;
    }

    internal void ShowCandidates(
        Rectangle destinationSlot,
        IReadOnlyList<WindowCandidate> candidates)
    {
        ReleaseThumbnails();

        _candidates = candidates;
        _page = 0;
        Bounds = destinationSlot;

        if (!Visible)
        {
            Show();
        }

        Activate();
        RebuildPage();
    }

    internal void HidePicker()
    {
        ReleaseThumbnails();
        _cards.Clear();
        _candidates = [];
        _page = 0;

        if (Visible)
        {
            Hide();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        using var headerFont = new Font(SystemFonts.MessageBoxFont, FontStyle.Bold);
        TextRenderer.DrawText(
            e.Graphics,
            GetHeaderText(),
            headerFont,
            new Rectangle(OuterPadding, 4, Math.Max(0, ClientSize.Width - 90), HeaderHeight - 8),
            ForeColor,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

        DrawPageButtons(e.Graphics);

        foreach (var card in _cards)
        {
            using var background = new SolidBrush(Color.FromArgb(48, 48, 48));
            using var border = new Pen(Color.FromArgb(96, 96, 96));
            e.Graphics.FillRectangle(background, card.Bounds);
            e.Graphics.DrawRectangle(border, card.Bounds);

            var titleBounds = new Rectangle(
                card.Bounds.Left + 6,
                card.Bounds.Top + 2,
                Math.Max(0, card.Bounds.Width - 12),
                CardTitleHeight - 4);

            TextRenderer.DrawText(
                e.Graphics,
                card.Candidate.Title,
                SystemFonts.MessageBoxFont,
                titleBounds,
                ForeColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }
    }

    private void RebuildPage()
    {
        ReleaseThumbnails();
        _cards.Clear();

        var pageCount = GetPageCount();
        if (pageCount == 0)
        {
            Invalidate();
            return;
        }

        _page = Math.Clamp(_page, 0, pageCount - 1);
        var pageCandidates = _candidates
            .Skip(_page * MaxCardsPerPage)
            .Take(MaxCardsPerPage)
            .ToArray();

        var content = new Rectangle(
            OuterPadding,
            HeaderHeight,
            Math.Max(1, ClientSize.Width - (OuterPadding * 2)),
            Math.Max(1, ClientSize.Height - HeaderHeight - OuterPadding));

        var columns = content.Width >= 300 ? 2 : 1;
        var rows = (int)Math.Ceiling(pageCandidates.Length / (double)columns);
        var cardWidth = Math.Max(1, (content.Width - ((columns - 1) * CardGap)) / columns);
        var cardHeight = Math.Max(1, (content.Height - ((rows - 1) * CardGap)) / Math.Max(1, rows));

        for (var index = 0; index < pageCandidates.Length; index++)
        {
            var column = index % columns;
            var row = index / columns;
            var bounds = new Rectangle(
                content.Left + (column * (cardWidth + CardGap)),
                content.Top + (row * (cardHeight + CardGap)),
                cardWidth,
                cardHeight);

            var card = new Card(pageCandidates[index], bounds);
            _cards.Add(card);
            RegisterThumbnail(card);
        }

        Invalidate();
    }

    private void RegisterThumbnail(Card card)
    {
        if (DwmMethods.DwmRegisterThumbnail(Handle, card.Candidate.Handle, out var thumbnail) < 0 ||
            thumbnail == IntPtr.Zero)
        {
            return;
        }

        _thumbnails.Add(thumbnail);

        var previewBounds = new Rectangle(
            card.Bounds.Left + 5,
            card.Bounds.Top + CardTitleHeight,
            Math.Max(1, card.Bounds.Width - 10),
            Math.Max(1, card.Bounds.Height - CardTitleHeight - 5));

        var fitted = FitThumbnail(thumbnail, previewBounds);
        var properties = new DwmMethods.ThumbnailProperties
        {
            Flags =
                DwmMethods.DwmTnpRectDestination |
                DwmMethods.DwmTnpOpacity |
                DwmMethods.DwmTnpVisible |
                DwmMethods.DwmTnpSourceClientAreaOnly,
            Destination = NativeMethods.NativeRect.FromRectangle(fitted),
            Source = default,
            Opacity = 255,
            Visible = true,
            SourceClientAreaOnly = false
        };

        DwmMethods.DwmUpdateThumbnailProperties(thumbnail, ref properties);
    }

    private static Rectangle FitThumbnail(IntPtr thumbnail, Rectangle area)
    {
        if (DwmMethods.DwmQueryThumbnailSourceSize(thumbnail, out var source) < 0 ||
            source.Width <= 0 ||
            source.Height <= 0)
        {
            return area;
        }

        var scale = Math.Min(
            area.Width / (double)source.Width,
            area.Height / (double)source.Height);

        var width = Math.Max(1, (int)Math.Round(source.Width * scale));
        var height = Math.Max(1, (int)Math.Round(source.Height * scale));
        var x = area.Left + ((area.Width - width) / 2);
        var y = area.Top + ((area.Height - height) / 2);
        return new Rectangle(x, y, width, height);
    }

    private void DrawPageButtons(Graphics graphics)
    {
        _previousButton = Rectangle.Empty;
        _nextButton = Rectangle.Empty;

        if (GetPageCount() <= 1)
        {
            return;
        }

        _previousButton = new Rectangle(ClientSize.Width - 68, 5, 28, 24);
        _nextButton = new Rectangle(ClientSize.Width - 36, 5, 28, 24);

        TextRenderer.DrawText(
            graphics,
            "‹",
            SystemFonts.MessageBoxFont,
            _previousButton,
            ForeColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

        TextRenderer.DrawText(
            graphics,
            "›",
            SystemFonts.MessageBoxFont,
            _nextButton,
            ForeColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }

    private string GetHeaderText()
    {
        var pageCount = GetPageCount();
        return pageCount <= 1
            ? "Choose a window  •  Esc cancels"
            : $"Choose a window  •  page {_page + 1}/{pageCount}";
    }

    private int GetPageCount()
    {
        return _candidates.Count == 0
            ? 0
            : (int)Math.Ceiling(_candidates.Count / (double)MaxCardsPerPage);
    }

    private void OnPickerMouseDown(object? sender, MouseEventArgs e)
    {
        _ = sender;

        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        if (_previousButton.Contains(e.Location))
        {
            ChangePage(-1);
            return;
        }

        if (_nextButton.Contains(e.Location))
        {
            ChangePage(1);
            return;
        }

        var card = _cards.FirstOrDefault(item => item.Bounds.Contains(e.Location));
        if (card is not null)
        {
            WindowChosen?.Invoke(card.Candidate.Handle);
        }
    }

    private void OnPickerMouseWheel(object? sender, MouseEventArgs e)
    {
        _ = sender;
        ChangePage(e.Delta < 0 ? 1 : -1);
    }

    private void OnPickerKeyDown(object? sender, KeyEventArgs e)
    {
        _ = sender;

        if (e.KeyCode == Keys.Escape)
        {
            e.Handled = true;
            CancelRequested?.Invoke();
        }
    }

    private void ChangePage(int delta)
    {
        var pageCount = GetPageCount();
        if (pageCount <= 1)
        {
            return;
        }

        _page = (_page + delta + pageCount) % pageCount;
        RebuildPage();
    }

    private void ReleaseThumbnails()
    {
        foreach (var thumbnail in _thumbnails)
        {
            DwmMethods.DwmUnregisterThumbnail(thumbnail);
        }

        _thumbnails.Clear();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ReleaseThumbnails();
        }

        base.Dispose(disposing);
    }

    private sealed record Card(WindowCandidate Candidate, Rectangle Bounds);
}
