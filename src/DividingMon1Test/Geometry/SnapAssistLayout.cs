using System.Drawing;

namespace DividingMon1Test.Geometry;

public static class SnapAssistLayout
{
    private static readonly SnapKind[] QuarterKinds =
    [
        SnapKind.TopLeftQuarter,
        SnapKind.TopRightQuarter,
        SnapKind.BottomLeftQuarter,
        SnapKind.BottomRightQuarter
    ];

    public static IReadOnlyList<Rectangle> GetRemainingSlots(SnapTarget target)
    {
        var area = target.SubmonitorBounds;

        return target.Kind switch
        {
            SnapKind.LeftHalf => [SnapLayout.GetSlice(area, SnapKind.RightHalf)],
            SnapKind.RightHalf => [SnapLayout.GetSlice(area, SnapKind.LeftHalf)],
            SnapKind.TopHalf => [SnapLayout.GetSlice(area, SnapKind.BottomHalf)],
            SnapKind.BottomHalf => [SnapLayout.GetSlice(area, SnapKind.TopHalf)],
            SnapKind.TopLeftQuarter or
            SnapKind.TopRightQuarter or
            SnapKind.BottomLeftQuarter or
            SnapKind.BottomRightQuarter => GetRemainingQuarters(area, target.Kind),
            _ => []
        };
    }

    private static Rectangle[] GetRemainingQuarters(Rectangle area, SnapKind occupied)
    {
        return QuarterKinds
            .Where(kind => kind != occupied)
            .Select(kind => SnapLayout.GetSlice(area, kind))
            .ToArray();
    }
}
