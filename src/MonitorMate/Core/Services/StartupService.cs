using Microsoft.Win32;

namespace MonitorMate.Core.Services;

public sealed class StartupService
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "MonitorMate";
    private readonly LogService _log;

    public StartupService(LogService log) => _log = log;

    public bool IsEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: false);
            return key?.GetValue(ValueName) is string value && !string.IsNullOrWhiteSpace(value);
        }
        catch (Exception ex)
        {
            _log.Warn($"Could not read startup setting: {ex.Message}");
            return false;
        }
    }

    public bool SetEnabled(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(RunKey, writable: true);
            if (enabled)
            {
                var command = AppLaunch.StartupCommand();
                if (string.IsNullOrWhiteSpace(command))
                {
                    return false;
                }

                key.SetValue(ValueName, command);
            }
            else
            {
                key.DeleteValue(ValueName, throwOnMissingValue: false);
            }

            return true;
        }
        catch (Exception ex)
        {
            _log.Error("Could not update the Windows startup entry.", ex);
            return false;
        }
    }
}
