using System.Reflection;
using System.IO;
using MonitorMate.Core.Services;

internal static class Program
{
    [STAThread]
    public static int Main()
    {
        var directory = Path.Combine(Path.GetTempPath(), "MonitorMate-restart-" + Guid.NewGuid().ToString("N"));
        Environment.SetEnvironmentVariable("MONITOR_MATE_DATA_DIR", directory);
        Directory.CreateDirectory(directory);
        // Only the test output's launcher is replaced. No replacement app or display scan runs.
        File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "Launch-MonitorMate.ps1"), "exit 0\n");
        var app = new MonitorMate.App();
        using var services = new AppServices();
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        typeof(MonitorMate.App).GetField("_services", flags)!.SetValue(app, services);
        typeof(MonitorMate.App).GetMethod("Restart", flags)!.Invoke(app, null);
        var disposed = (bool)typeof(AppServices).GetField("_disposed", flags)!.GetValue(services)!;
        Console.WriteLine(disposed
            ? "PASS Services were disposed before restart returned and ownership was handed off."
            : "FAIL Restart returned before service/gamma cleanup; replacement launch can overlap cleanup.");
        return disposed ? 0 : 1;
    }
}
