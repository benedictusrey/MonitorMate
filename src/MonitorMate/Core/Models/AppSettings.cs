namespace MonitorMate.Core.Models;

public enum ScheduleMode
{
    Fixed,
    Solar
}

public enum BreakStyle
{
    Gentle,
    Fullscreen,
    Strict
}

public enum FocusMode
{
    Reading,
    Spotlight
}

public enum ThemeChoice
{
    Dark,
    System
}

public sealed class AppSettings
{
    public int SchemaVersion { get; set; } = 1;
    public bool SetupCompleted { get; set; }
    public bool StartWithWindows { get; set; }
    public bool StartMinimized { get; set; }
    public bool CloseToTray { get; set; } = true;
    public bool RestoreOnLaunch { get; set; }
    public bool AutomaticDisplayControlAllowed { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public bool AutomaticDisplayControlEnabled => AutomaticDisplayControlAllowed && (ScheduleEnabled || RestoreOnLaunch);
    public bool SoundEnabled { get; set; } = true;
    public bool ReducedMotion { get; set; }
    public bool IsPaused { get; set; }
    public string LastPresetId { get; set; } = "day";
    public DateTime? LastSavedUtc { get; set; }

    public bool ScheduleEnabled { get; set; }
    public ScheduleMode ScheduleMode { get; set; } = ScheduleMode.Fixed;
    public string DayStart { get; set; } = "07:00";
    public string EveningStart { get; set; } = "19:00";
    public string NightStart { get; set; } = "22:30";
    public string DayPresetId { get; set; } = "day";
    public string EveningPresetId { get; set; } = "evening";
    public string NightPresetId { get; set; } = "night";
    public bool SmoothTransitions { get; set; } = true;
    public int TransitionMinutes { get; set; } = 30;
    public double Latitude { get; set; } = 0;
    public double Longitude { get; set; } = 0;
    public bool HasLocation { get; set; }

    public BreakSettings Breaks { get; set; } = new();
    public FocusSettings Focus { get; set; } = new();
    public HotkeySettings Hotkeys { get; set; } = new();
    public Dictionary<string, DisplayPreference> DisplayPreferences { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public DisplayPreference GetDisplayPreference(string displayId)
    {
        if (!DisplayPreferences.TryGetValue(displayId, out var preference) || preference is null)
        {
            preference = new DisplayPreference();
            DisplayPreferences[displayId] = preference;
        }

        return preference;
    }
}

public sealed class DisplayPreference
{
    public bool Enabled { get; set; } = true;
    public bool Initialized { get; set; }
    public bool ContrastTouched { get; set; }
    public int Brightness { get; set; } = 100;
    public int TemperatureK { get; set; } = 6500;
    public int Contrast { get; set; } = 50;
    public int SoftwareDim { get; set; }
    public string LastPresetId { get; set; } = "day";
}

public sealed class BreakSettings
{
    public bool Enabled { get; set; } = true;
    public bool AutoStart { get; set; } = true;
    public string Rhythm { get; set; } = "20-20-20";
    public int WorkMinutes { get; set; } = 20;
    public int ShortBreakSeconds { get; set; } = 20;
    public int LongBreakMinutes { get; set; } = 5;
    public int LongBreakEvery { get; set; } = 4;
    public int PreBreakSeconds { get; set; } = 30;
    public BreakStyle Style { get; set; } = BreakStyle.Fullscreen;
    public bool SoundEnabled { get; set; } = true;
    public bool PauseWhenIdle { get; set; } = true;
    public int IdleMinutes { get; set; } = 3;
    public bool RestartAfterIdle { get; set; } = true;
    public bool PauseInFullscreen { get; set; } = true;
    public bool HoldToSkip { get; set; }
    public bool LockAtEnd { get; set; }

    public BreakSettings Clone() => (BreakSettings)MemberwiseClone();
}

public sealed class FocusSettings
{
    public FocusMode Mode { get; set; } = FocusMode.Reading;
    public int DimPercent { get; set; } = 58;
    public int BandHeight { get; set; } = 280;
    public bool FollowPointer { get; set; } = true;
    public int DefaultMinutes { get; set; } = 25;
    public bool HideMainWindow { get; set; } = true;
}

public sealed class HotkeySettings
{
    public bool Enabled { get; set; } = true;
    public string ToggleCare { get; set; } = "Ctrl+Alt+F";
    public string BrightnessUp { get; set; } = "Ctrl+Alt+Up";
    public string BrightnessDown { get; set; } = "Ctrl+Alt+Down";
    public string Warmer { get; set; } = "Ctrl+Alt+Left";
    public string Cooler { get; set; } = "Ctrl+Alt+Right";
    public string TakeBreak { get; set; } = "Ctrl+Alt+B";
    public string ShowWindow { get; set; } = "Ctrl+Alt+M";
    public string ToggleFocus { get; set; } = "Ctrl+Alt+L";
}

public sealed class AppSettingsEnvelope
{
    public AppSettings Settings { get; set; } = new();
}
