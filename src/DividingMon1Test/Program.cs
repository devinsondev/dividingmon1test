using DividingMon1Test.UI;

namespace DividingMon1Test;

internal static class Program
{
    private const string MutexName = @"Local\DividingMon1Test.Singleton";

    [STAThread]
    private static void Main()
    {
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        using var mutex = new Mutex(initiallyOwned: true, MutexName, out var createdNew);
        if (!createdNew)
        {
            MessageBox.Show(
                "DividingMon1Test is already running in the system tray.",
                "DividingMon1Test",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        Application.Run(new TrayApplicationContext());
    }
}
