using System.Drawing;

namespace DividingMon1Test.Geometry;

public enum SnapKind
{
    Full,
    LeftHalf,
    RightHalf,
    TopHalf,
    BottomHalf,
    TopLeftQuarter,
    TopRightQuarter,
    BottomLeftQuarter,
    BottomRightQuarter
}

public readonly record struct SnapTarget(
    int SubmonitorIndex,
    Rectangle SubmonitorBounds,
    Rectangle Destination,
    SnapKind Kind)
{
    public string Label => Kind switch
    {
        SnapKind.Full => $"Zone {SubmonitorIndex}: full",
        SnapKind.LeftHalf => $"Zone {SubmonitorIndex}: left half",
        SnapKind.RightHalf => $"Zone {SubmonitorIndex}: right half",
        SnapKind.TopHalf => $"Zone {SubmonitorIndex}: top half",
        SnapKind.BottomHalf => $"Zone {SubmonitorIndex}: bottom half",
        SnapKind.TopLeftQuarter => $"Zone {SubmonitorIndex}: top-left quarter",
        SnapKind.TopRightQuarter => $"Zone {SubmonitorIndex}: top-right quarter",
        SnapKind.BottomLeftQuarter => $"Zone {SubmonitorIndex}: bottom-left quarter",
        SnapKind.BottomRightQuarter => $"Zone {SubmonitorIndex}: bottom-right quarter",
        _ => $"Zone {SubmonitorIndex}"
    };
}

public static class SnapLayout
{
    public const int DefaultEdgeThreshold = 48;
    public const int DefaultPhysicalEdgeGuard = 72;

    public static bool TryResolve(
        Rectangle monitorBounds,
        Point cursor,
        int edgeThreshold,
        out SnapTarget target)
    {
        return TryResolve(
            monitorBounds,
            cursor,
            edgeThreshold,
            physicalEdgeGuard: 0,
            out target);
    }

    public static bool TryResolve(
        Rectangle monitorBounds,
        Point cursor,
        int edgeThreshold,
        int physicalEdgeGuard,
        out SnapTarget target)
    {
        target = default;

        if (monitorBounds.Width < 2 ||
            monitorBounds.Height < 2 ||
            !monitorBounds.Contains(cursor))
        {
            return false;
        }

        var guard = ClampPhysicalEdgeGuard(monitorBounds, physicalEdgeGuard);
        if (guard > 0 && IsInPhysicalEdgeGuard(monitorBounds, cursor, guard))
        {
            return false;
        }

        var index = GetSubmonitorIndex(monitorBounds, cursor);
        var submonitor = GetSubmonitor(monitorBounds, index);
        var maxThreshold = Math.Max(1, Math.Min(submonitor.Width, submonitor.Height) / 3);
        var threshold = Math.Clamp(edgeThreshold, 1, maxThreshold);

        var leftEdge = submonitor.Left == monitorBounds.Left
            ? submonitor.Left + guard
            : submonitor.Left;
        var rightEdge = submonitor.Right == monitorBounds.Right
            ? submonitor.Right - 1 - guard
            : submonitor.Right - 1;
        var topEdge = submonitor.Top == monitorBounds.Top
            ? submonitor.Top + guard
            : submonitor.Top;
        var bottomEdge = submonitor.Bottom == monitorBounds.Bottom
            ? submonitor.Bottom - 1 - guard
            : submonitor.Bottom - 1;

        var nearLeft = cursor.X >= leftEdge && cursor.X - leftEdge < threshold;
        var nearRight = cursor.X <= rightEdge && rightEdge - cursor.X < threshold;
        var nearTop = cursor.Y >= topEdge && cursor.Y - topEdge < threshold;
        var nearBottom = cursor.Y <= bottomEdge && bottomEdge - cursor.Y < threshold;

        var kind = ResolveKind(nearLeft, nearRight, nearTop, nearBottom);
        var destination = GetSlice(submonitor, kind);
        target = new SnapTarget(index, submonitor, destination, kind);
        return true;
    }

    public static Rectangle GetSubmonitor(Rectangle monitorBounds, int index)
    {
        if (index is < 1 or > 4)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        var leftWidth = monitorBounds.Width / 2;
        var topHeight = monitorBounds.Height / 2;
        var rightWidth = monitorBounds.Width - leftWidth;
        var bottomHeight = monitorBounds.Height - topHeight;

        return index switch
        {
            1 => new Rectangle(monitorBounds.Left, monitorBounds.Top, leftWidth, topHeight),
            2 => new Rectangle(monitorBounds.Left + leftWidth, monitorBounds.Top, rightWidth, topHeight),
            3 => new Rectangle(monitorBounds.Left, monitorBounds.Top + topHeight, leftWidth, bottomHeight),
            4 => new Rectangle(
                monitorBounds.Left + leftWidth,
                monitorBounds.Top + topHeight,
                rightWidth,
                bottomHeight),
            _ => throw new InvalidOperationException()
        };
    }

    public static Rectangle GetSlice(Rectangle area, SnapKind kind)
    {
        var leftWidth = area.Width / 2;
        var topHeight = area.Height / 2;
        var rightWidth = area.Width - leftWidth;
        var bottomHeight = area.Height - topHeight;

        return kind switch
        {
            SnapKind.Full => area,
            SnapKind.LeftHalf => new Rectangle(area.Left, area.Top, leftWidth, area.Height),
            SnapKind.RightHalf => new Rectangle(area.Left + leftWidth, area.Top, rightWidth, area.Height),
            SnapKind.TopHalf => new Rectangle(area.Left, area.Top, area.Width, topHeight),
            SnapKind.BottomHalf => new Rectangle(area.Left, area.Top + topHeight, area.Width, bottomHeight),
            SnapKind.TopLeftQuarter => new Rectangle(area.Left, area.Top, leftWidth, topHeight),
            SnapKind.TopRightQuarter => new Rectangle(area.Left + leftWidth, area.Top, rightWidth, topHeight),
            SnapKind.BottomLeftQuarter => new Rectangle(area.Left, area.Top + topHeight, leftWidth, bottomHeight),
            SnapKind.BottomRightQuarter => new Rectangle(
                area.Left + leftWidth,
                area.Top + topHeight,
                rightWidth,
                bottomHeight),
            _ => area
        };
    }

    private static int GetSubmonitorIndex(Rectangle bounds, Point cursor)
    {
        var splitX = bounds.Left + (bounds.Width / 2);
        var splitY = bounds.Top + (bounds.Height / 2);
        var right = cursor.X >= splitX;
        var bottom = cursor.Y >= splitY;

        return (right, bottom) switch
        {
            (false, false) => 1,
            (true, false) => 2,
            (false, true) => 3,
            (true, true) => 4
        };
    }

    private static int ClampPhysicalEdgeGuard(Rectangle bounds, int guard)
    {
        var maxGuard = Math.Max(0, Math.Min(bounds.Width, bounds.Height) / 4);
        return Math.Clamp(guard, 0, maxGuard);
    }

    private static bool IsInPhysicalEdgeGuard(Rectangle bounds, Point cursor, int guard)
    {
        return cursor.X - bounds.Left < guard ||
            bounds.Right - 1 - cursor.X < guard ||
            cursor.Y - bounds.Top < guard ||
            bounds.Bottom - 1 - cursor.Y < guard;
    }

    private static SnapKind ResolveKind(bool left, bool right, bool top, bool bottom)
    {
        if (left && top) return SnapKind.TopLeftQuarter;
        if (right && top) return SnapKind.TopRightQuarter;
        if (left && bottom) return SnapKind.BottomLeftQuarter;
        if (right && bottom) return SnapKind.BottomRightQuarter;
        if (left) return SnapKind.LeftHalf;
        if (right) return SnapKind.RightHalf;
        if (top) return SnapKind.TopHalf;
        if (bottom) return SnapKind.BottomHalf;
        return SnapKind.Full;
    }
}
