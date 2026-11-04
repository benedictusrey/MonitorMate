namespace MonitorMate.Core.Services;

public static class AppPaths
{
    public static string DataDirectory { get; } = ResolveDataDirectory();

    public static string LogsDirectory => Path.Combine(DataDirectory, "logs");
    public static string SettingsFile => Path.Combine(DataDirectory, "settings.json");
    public static string UsageFile => Path.Combine(DataDirectory, "usage.json");
    public static string GammaFile => Path.Combine(DataDirectory, "gamma-baselines.json");
    public static string DiagnosticsFile => Path.Combine(DataDirectory, "diagnostics.txt");
    public static string LogFile => Path.Combine(LogsDirectory, "monitormate.log");

    private static string ResolveDataDirectory()
    {
        // A portable/test override keeps the app self-contained when the user
        // wants it beside the executable. Normal installs use %APPDATA%.
        var overrideDirectory = Environment.GetEnvironmentVariable("MONITOR_MATE_DATA_DIR");
        if (!string.IsNullOrWhiteSpace(overrideDirectory))
        {
            return Path.GetFullPath(overrideDirectory);
        }

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MonitorMate");
    }

    public static void EnsureCreated()
    {
        Directory.CreateDirectory(DataDirectory);
        Directory.CreateDirectory(LogsDirectory);
    }
}
