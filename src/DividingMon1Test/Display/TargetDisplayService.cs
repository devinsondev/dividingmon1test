namespace DividingMon1Test.Display;

internal sealed class TargetDisplayService
{
    private string? _selectedDeviceName;

    public string? SelectedDeviceName => _selectedDeviceName;

    public IReadOnlyList<Screen> GetScreens()
    {
        return Screen.AllScreens
            .OrderBy(screen => screen.DeviceName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public Screen? GetTarget()
    {
        var screens = Screen.AllScreens;

        if (_selectedDeviceName is not null)
        {
            var selected = screens.FirstOrDefault(
                screen => string.Equals(
                    screen.DeviceName,
                    _selectedDeviceName,
                    StringComparison.OrdinalIgnoreCase));

            if (selected is not null)
            {
                return selected;
            }
        }

        var fallback = screens.FirstOrDefault(screen => !screen.Primary);
        if (fallback is null)
        {
            return null;
        }

        _selectedDeviceName = fallback.DeviceName;
        return fallback;
    }

    public bool TrySelect(string deviceName)
    {
        if (string.IsNullOrWhiteSpace(deviceName))
        {
            return false;
        }

        var exists = Screen.AllScreens.Any(
            screen => string.Equals(
                screen.DeviceName,
                deviceName,
                StringComparison.OrdinalIgnoreCase));

        if (!exists)
        {
            return false;
        }

        _selectedDeviceName = deviceName;
        return true;
    }

    public static string Describe(Screen screen)
    {
        var bounds = screen.Bounds;
        var primary = screen.Primary ? " (primary)" : string.Empty;
        return $"{screen.DeviceName}{primary} — {bounds.Width}x{bounds.Height} @ {bounds.X},{bounds.Y}";
    }
}
