using System.Diagnostics;
using System.Reflection;

namespace MonitorMate.Core.Services;

/// <summary>
/// Uses the native apphost for self-contained packages and a hidden launcher
/// for framework-dependent DLL builds.
/// </summary>
public static class AppLaunch
{
    public static bool IsDotnetHost =>
        Path.GetFileNameWithoutExtension(Environment.ProcessPath ?? string.Empty)
            .Equals("dotnet", StringComparison.OrdinalIgnoreCase);

    public static string HostPath => Environment.ProcessPath ?? "dotnet.exe";
    public static string? EntryDll => Assembly.GetEntryAssembly()?.Location;

    private static string PowerShellPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.Windows),
        "System32", "WindowsPowerShell", "v1.0", "powershell.exe");

    private static string? LauncherPath
    {
        get
        {
            var launcher = Path.Combine(AppContext.BaseDirectory, "Launch-MonitorMate.ps1");
            return File.Exists(launcher) ? launcher : null;
        }
    }

    public static string StartupCommand()
    {
        if (!IsDotnetHost) return $"\"{HostPath}\" --tray";
        if (LauncherPath is { } launcher)
        {
            return $"\"{PowerShellPath}\" -NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File \"{launcher}\" --tray";
        }

        if (EntryDll is null)
        {
            return $"\"{HostPath}\" --tray";
        }

        return $"\"{PowerShellPath}\" -NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -Command \"& '{HostPath}' '{EntryDll}' --tray\"";
    }

    public static ProcessStartInfo CreateRestartStartInfo()
    {
        if (!IsDotnetHost)
        {
            return new ProcessStartInfo(HostPath)
            {
                UseShellExecute = false,
                WorkingDirectory = AppContext.BaseDirectory
            };
        }
        if (LauncherPath is { } launcher)
        {
            return new ProcessStartInfo(PowerShellPath,
                $"-NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File \"{launcher}\"")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Normal,
                WorkingDirectory = AppContext.BaseDirectory
            };
        }

        if (EntryDll is not null)
        {
            return new ProcessStartInfo(HostPath, $"\"{EntryDll}\"")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Normal,
                WorkingDirectory = AppContext.BaseDirectory
            };
        }

        return new ProcessStartInfo(HostPath)
        {
            UseShellExecute = true,
            WorkingDirectory = AppContext.BaseDirectory
        };
    }

    public static string? IconPath
    {
        get
        {
            var icon = Path.Combine(AppContext.BaseDirectory, "MonitorMate.ico");
            return File.Exists(icon) ? icon : null;
        }
    }
}
