using System.Drawing;
using DividingMon1Test.Geometry;

namespace DividingMon1Test.SelfTest;

internal static class Program
{
    private static int Main()
    {
        var monitor = new Rectangle(100, 200, 1920, 1080);

        AssertSnap(monitor, new Point(580, 470), 1, SnapKind.Full, new Rectangle(100, 200, 960, 540));
        AssertSnap(monitor, new Point(101, 470), 1, SnapKind.LeftHalf, new Rectangle(100, 200, 480, 540));
        AssertSnap(monitor, new Point(580, 201), 1, SnapKind.TopHalf, new Rectangle(100, 200, 960, 270));
        AssertSnap(monitor, new Point(101, 201), 1, SnapKind.TopLeftQuarter, new Rectangle(100, 200, 480, 270));

        AssertSnap(monitor, new Point(1540, 470), 2, SnapKind.Full, new Rectangle(1060, 200, 960, 540));
        AssertSnap(monitor, new Point(580, 1010), 3, SnapKind.Full, new Rectangle(100, 740, 960, 540));
        AssertSnap(monitor, new Point(1540, 1010), 4, SnapKind.Full, new Rectangle(1060, 740, 960, 540));

        AssertNoSnap(monitor, new Point(99, 470));
        AssertNoSnap(monitor, new Point(2020, 470));

        AssertOddPartitionCoversMonitor();

        Console.WriteLine("Geometry self-test passed.");
        return 0;
    }

    private static void AssertSnap(
        Rectangle monitor,
        Point cursor,
        int expectedZone,
        SnapKind expectedKind,
        Rectangle expectedDestination)
    {
        if (!SnapLayout.TryResolve(
                monitor,
                cursor,
                SnapLayout.DefaultEdgeThreshold,
                out var target))
        {
            throw new InvalidOperationException($"Expected snap at {cursor}.");
        }

        if (target.SubmonitorIndex != expectedZone ||
            target.Kind != expectedKind ||
            target.Destination != expectedDestination)
        {
            throw new InvalidOperationException(
                $"Unexpected snap at {cursor}. " +
                $"Got zone={target.SubmonitorIndex}, kind={target.Kind}, rect={target.Destination}.");
        }
    }

    private static void AssertNoSnap(Rectangle monitor, Point cursor)
    {
        if (SnapLayout.TryResolve(
                monitor,
                cursor,
                SnapLayout.DefaultEdgeThreshold,
                out _))
        {
            throw new InvalidOperationException($"Unexpected snap outside monitor at {cursor}.");
        }
    }

    private static void AssertOddPartitionCoversMonitor()
    {
        var monitor = new Rectangle(-1919, -1079, 1919, 1079);
        var zones = Enumerable.Range(1, 4)
            .Select(index => SnapLayout.GetSubmonitor(monitor, index))
            .ToArray();

        var totalArea = zones.Sum(zone => (long)zone.Width * zone.Height);
        var monitorArea = (long)monitor.Width * monitor.Height;

        if (totalArea != monitorArea)
        {
            throw new InvalidOperationException(
                $"Odd-size partition lost pixels. zones={totalArea}, monitor={monitorArea}.");
        }

        if (zones[0].Right != zones[1].Left ||
            zones[0].Bottom != zones[2].Top ||
            zones[2].Right != zones[3].Left ||
            zones[1].Bottom != zones[3].Top)
        {
            throw new InvalidOperationException("Odd-size partition contains a gap or overlap.");
        }
    }
}
