using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Threading;
using MonitorMate.Core.Models;

namespace MonitorMate.Core.Services;

public sealed class SettingsService : IDisposable
{
    private readonly LogService _log;
    private readonly JsonSerializerOptions _json;
    private readonly DispatcherTimer _saveTimer;
    private readonly object _gate = new();
    private AppSettings _settings = new();
    private bool _dirty;
    private bool _disposed;

    public SettingsService(LogService log)
    {
        _log = log;
        AppPaths.EnsureCreated();
        _json = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };
        _saveTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(900)
        };
        _saveTimer.Tick += (_, _) => Flush();
        Load();
    }

    public AppSettings Settings => _settings;
    public string? LoadWarning { get; private set; }
    public string FilePath => AppPaths.SettingsFile;

    private void Load()
    {
        try
        {
            if (!File.Exists(AppPaths.SettingsFile))
            {
                _settings = new AppSettings();
                return;
            }

            var text = File.ReadAllText(AppPaths.SettingsFile);
            var loaded = JsonSerializer.Deserialize<AppSettings>(text, _json);
            if (loaded is null)
            {
                throw new JsonException("Settings file was empty.");
            }

            _settings = Normalize(loaded);
        }
        catch (Exception ex)
        {
            LoadWarning = "MonitorMate could not read your settings. It kept a backup and started with safe defaults.";
            _log.Error("Could not load settings.", ex);
            TryBackupCorruptFile();
            _settings = new AppSettings();
        }
    }

    private static AppSettings Normalize(AppSettings settings)
    {
        if (!settings.AutomaticDisplayControlAllowed)
        {
            settings.ScheduleEnabled = false;
            settings.RestoreOnLaunch = false;
        }

        settings.Breaks ??= new BreakSettings();
        settings.Focus ??= new FocusSettings();
        settings.Hotkeys ??= new HotkeySettings();
        settings.DisplayPreferences ??= new Dictionary<string, DisplayPreference>(StringComparer.OrdinalIgnoreCase);
        if (!PresetCatalog.Exists(settings.LastPresetId))
        {
            settings.LastPresetId = "day";
        }

        if (!PresetCatalog.Exists(settings.DayPresetId)) settings.DayPresetId = "day";
        if (!PresetCatalog.Exists(settings.EveningPresetId)) settings.EveningPresetId = "evening";
        if (!PresetCatalog.Exists(settings.NightPresetId)) settings.NightPresetId = "night";
        settings.Breaks.WorkMinutes = Math.Clamp(settings.Breaks.WorkMinutes, 1, 240);
        settings.Breaks.ShortBreakSeconds = Math.Clamp(settings.Breaks.ShortBreakSeconds, 5, 600);
        settings.Breaks.LongBreakMinutes = Math.Clamp(settings.Breaks.LongBreakMinutes, 1, 60);
        settings.Breaks.LongBreakEvery = Math.Clamp(settings.Breaks.LongBreakEvery, 2, 12);
        settings.Breaks.PreBreakSeconds = Math.Clamp(settings.Breaks.PreBreakSeconds, 0, 120);
        settings.Breaks.IdleMinutes = Math.Clamp(settings.Breaks.IdleMinutes, 1, 60);
        settings.Focus.DimPercent = Math.Clamp(settings.Focus.DimPercent, 15, 85);
        settings.Focus.BandHeight = Math.Clamp(settings.Focus.BandHeight, 100, 700);
        settings.Focus.DefaultMinutes = Math.Clamp(settings.Focus.DefaultMinutes, 5, 180);
        settings.TransitionMinutes = Math.Clamp(settings.TransitionMinutes, 0, 120);
        settings.Latitude = Math.Clamp(settings.Latitude, -90, 90);
        settings.Longitude = Math.Clamp(settings.Longitude, -180, 180);
        return settings;
    }

    private void TryBackupCorruptFile()
    {
        try
        {
            if (!File.Exists(AppPaths.SettingsFile))
            {
                return;
            }

            var backup = AppPaths.SettingsFile + $".corrupt-{DateTime.Now:yyyyMMdd-HHmmss}.bak";
            File.Copy(AppPaths.SettingsFile, backup, overwrite: false);
        }
        catch (Exception ex)
        {
            _log.Warn($"Could not back up unreadable settings: {ex.Message}");
        }
    }

    public void SaveDebounced()
    {
        if (_disposed)
        {
            return;
        }

        _dirty = true;
        _saveTimer.Stop();
        _saveTimer.Start();
    }

    public void Flush()
    {
        _saveTimer.Stop();
        if (_disposed)
        {
            return;
        }

        lock (_gate)
        {
            if (!_dirty && File.Exists(AppPaths.SettingsFile))
            {
                return;
            }

            try
            {
                _settings.LastSavedUtc = DateTime.UtcNow;
                var json = JsonSerializer.Serialize(_settings, _json);
                var temp = AppPaths.SettingsFile + ".tmp";
                File.WriteAllText(temp, json);
                File.Move(temp, AppPaths.SettingsFile, overwrite: true);
                _dirty = false;
            }
            catch (Exception ex)
            {
                _log.Error("Could not save settings.", ex);
            }
        }
    }

    public void Replace(AppSettings settings)
    {
        _settings = Normalize(settings);
        _dirty = true;
        Flush();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Flush();
        _disposed = true;
        _saveTimer.Stop();
    }
}
