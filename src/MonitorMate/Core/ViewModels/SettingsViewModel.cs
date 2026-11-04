using System.Reflection;
using System.Windows;
using System.Windows.Input;
using MonitorMate.Core.Models;
using MonitorMate.Core.Mvvm;
using MonitorMate.Core.Services;

namespace MonitorMate.Core.ViewModels;

public sealed class HotkeyRowViewModel : ObservableObject
{
    private readonly Action<HotkeyRowViewModel> _changed;
    private string _gesture;
    private string _error = string.Empty;

    public HotkeyRowViewModel(string key, string name, string description, string gesture, Action<HotkeyRowViewModel> changed)
    {
        Key = key;
        Name = name;
        Description = description;
        _gesture = gesture;
        _changed = changed;
    }

    public string Key { get; }
    public string Name { get; }
    public string Description { get; }

    public string Gesture
    {
        get => _gesture;
        set
        {
            if (!SetProperty(ref _gesture, value)) return;
            _changed(this);
        }
    }

    public string Error { get => _error; set => SetProperty(ref _error, value); }
}

public sealed class DiagnosticRow
{
    public required string Name { get; init; }
    public required string Detail { get; init; }
    public required string ControlPath { get; init; }
    public required string Status { get; init; }
}

public sealed class SettingsViewModel : ObservableObject
{
    private readonly AppServices _services;
    private readonly Func<string, string, string, bool> _confirm;
    private readonly Action _requestRestart;
    private bool _startWithWindows;
    private bool _startMinimized;
    private bool _closeToTray;
    private bool _soundEnabled;
    private bool _reducedMotion;
    private bool _restoreOnLaunch;
    private bool _hotkeysEnabled;
    private string _startupError = string.Empty;
    private string _hotkeyConflicts = string.Empty;
    private string _message = string.Empty;
    private string _diagnosticsText = string.Empty;
    private bool _isBusy;
    private bool _isDiagnosticsOpen;

    private readonly Func<bool> _requestAutomaticControl;

    public SettingsViewModel(AppServices services, Func<string, string, string, bool> confirm, Action requestRestart, Func<bool> requestAutomaticControl)
    {
        _services = services;
        _requestAutomaticControl = requestAutomaticControl;
        _confirm = confirm;
        _requestRestart = requestRestart;
        _startWithWindows = services.Startup.IsEnabled();
        LoadFromSettings();
        BuildHotkeyRows();
        RefreshDiagnostics();
        services.Comfort.StatesRebuilt += (_, _) => RefreshDiagnostics();
        CopyDiagnosticsCommand = new RelayCommand(CopyDiagnostics);
        ExportDiagnosticsCommand = new RelayCommand(ExportDiagnostics);
        OpenDataFolderCommand = new RelayCommand(OpenDataFolder);
        RecaptureBaselinesCommand = new RelayCommand(RecaptureBaselines);
        RefreshDisplaysCommand = new AsyncRelayCommand(RefreshDisplaysAsync);
        ResetHotkeysCommand = new RelayCommand(ResetHotkeys);
        ResetHistoryCommand = new RelayCommand(ResetHistory);
        ResetSettingsCommand = new RelayCommand(ResetSettings);
        ToggleDiagnosticsCommand = new RelayCommand(() => IsDiagnosticsOpen = !IsDiagnosticsOpen);
        _services.Hotkeys.ConflictsChanged += (_, _) => UpdateConflicts();
    }

    public ICommand CopyDiagnosticsCommand { get; }
    public ICommand ExportDiagnosticsCommand { get; }
    public ICommand OpenDataFolderCommand { get; }
    public ICommand RecaptureBaselinesCommand { get; }
    public ICommand RefreshDisplaysCommand { get; }
    public ICommand ResetHotkeysCommand { get; }
    public ICommand ResetHistoryCommand { get; }
    public ICommand ResetSettingsCommand { get; }
    public ICommand ToggleDiagnosticsCommand { get; }
    public IReadOnlyList<HotkeyRowViewModel> HotkeyRows { get; private set; } = Array.Empty<HotkeyRowViewModel>();
    public IReadOnlyList<DiagnosticRow> DiagnosticRows { get; private set; } = Array.Empty<DiagnosticRow>();
    public string VersionText => $"Version {Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0"}";
    public string DataPath => AppPaths.DataDirectory;
    public string StartupError { get => _startupError; private set => SetProperty(ref _startupError, value); }
    public string HotkeyConflicts { get => _hotkeyConflicts; private set => SetProperty(ref _hotkeyConflicts, value); }
    public string Message { get => _message; private set => SetProperty(ref _message, value); }
    public string DiagnosticsText { get => _diagnosticsText; private set => SetProperty(ref _diagnosticsText, value); }
    public bool IsBusy { get => _isBusy; private set => SetProperty(ref _isBusy, value); }
    public bool IsDiagnosticsOpen { get => _isDiagnosticsOpen; set => SetProperty(ref _isDiagnosticsOpen, value); }
    public bool CanStartWithWindows => !AppEnvironment.IsPackaged;

    public bool StartWithWindows
    {
        get => _startWithWindows;
        set
        {
            if (!SetProperty(ref _startWithWindows, value)) return;
            if (_services.Startup.SetEnabled(value))
            {
                _services.Settings.Settings.StartWithWindows = value;
                _services.Settings.SaveDebounced();
                StartupError = string.Empty;
            }
            else
            {
                _startWithWindows = !value;
                OnPropertyChanged();
                StartupError = "Windows did not accept the startup entry. Try again, or add MonitorMate to Startup apps in Windows Settings.";
            }
        }
    }

    public bool StartMinimized
    {
        get => _startMinimized;
        set
        {
            if (!SetProperty(ref _startMinimized, value)) return;
            _services.Settings.Settings.StartMinimized = value;
            _services.Settings.SaveDebounced();
        }
    }

    public bool CloseToTray
    {
        get => _closeToTray;
        set
        {
            if (!SetProperty(ref _closeToTray, value)) return;
            _services.Settings.Settings.CloseToTray = value;
            _services.Settings.SaveDebounced();
        }
    }

    public bool SoundEnabled
    {
        get => _soundEnabled;
        set
        {
            if (!SetProperty(ref _soundEnabled, value)) return;
            _services.Settings.Settings.SoundEnabled = value;
            _services.Settings.SaveDebounced();
        }
    }

    public bool ReducedMotion
    {
        get => _reducedMotion;
        set
        {
            if (!SetProperty(ref _reducedMotion, value)) return;
            _services.Settings.Settings.ReducedMotion = value;
            _services.Settings.SaveDebounced();
        }
    }

    public bool RestoreOnLaunch
    {
        get => _restoreOnLaunch;
        set
        {
            if (_restoreOnLaunch == value) return;
            if (value && !_requestAutomaticControl())
            {
                OnPropertyChanged(nameof(RestoreOnLaunch));
                Message = "Launch restore remains off. Manual controls are available.";
                return;
            }
            SetProperty(ref _restoreOnLaunch, value);
            var settings = _services.Settings.Settings;
            settings.RestoreOnLaunch = value;
            if (value)
            {
                settings.AutomaticDisplayControlAllowed = true;
                settings.SetupCompleted = true;
            }
            else if (!settings.ScheduleEnabled) settings.AutomaticDisplayControlAllowed = false;
            _services.Settings.SaveDebounced();
        }
    }

    public bool HotkeysEnabled
    {
        get => _hotkeysEnabled;
        set
        {
            if (!SetProperty(ref _hotkeysEnabled, value)) return;
            _services.Settings.Settings.Hotkeys.Enabled = value;
            _services.Settings.SaveDebounced();
            _services.Hotkeys.Rebuild();
            UpdateConflicts();
        }
    }

    private void LoadFromSettings()
    {
        var settings = _services.Settings.Settings;
        _startMinimized = settings.StartMinimized;
        _closeToTray = settings.CloseToTray;
        _soundEnabled = settings.SoundEnabled;
        _reducedMotion = settings.ReducedMotion;
        _restoreOnLaunch = settings.AutomaticDisplayControlAllowed && settings.RestoreOnLaunch;
        _hotkeysEnabled = settings.Hotkeys.Enabled;
        OnPropertyChanged(nameof(StartMinimized));
        OnPropertyChanged(nameof(CloseToTray));
        OnPropertyChanged(nameof(SoundEnabled));
        OnPropertyChanged(nameof(ReducedMotion));
        OnPropertyChanged(nameof(RestoreOnLaunch));
        OnPropertyChanged(nameof(HotkeysEnabled));
    }

    private void BuildHotkeyRows()
    {
        var hotkeys = _services.Settings.Settings.Hotkeys;
        HotkeyRows = new[]
        {
            new HotkeyRowViewModel("toggleCare", "Pause or resume care", "Restores original colour or reapplies your preset.", hotkeys.ToggleCare, ApplyHotkey),
            new HotkeyRowViewModel("brightnessUp", "Brighter", "Increases the selected display's brightness.", hotkeys.BrightnessUp, ApplyHotkey),
            new HotkeyRowViewModel("brightnessDown", "Dimmer", "Decreases the selected display's brightness.", hotkeys.BrightnessDown, ApplyHotkey),
            new HotkeyRowViewModel("warmer", "Warmer", "Lowers the colour temperature by 250 K.", hotkeys.Warmer, ApplyHotkey),
            new HotkeyRowViewModel("cooler", "Cooler", "Raises the colour temperature by 250 K.", hotkeys.Cooler, ApplyHotkey),
            new HotkeyRowViewModel("takeBreak", "Take a break", "Starts a break immediately.", hotkeys.TakeBreak, ApplyHotkey),
            new HotkeyRowViewModel("showWindow", "Open MonitorMate", "Brings the main window back.", hotkeys.ShowWindow, ApplyHotkey),
            new HotkeyRowViewModel("toggleFocus", "Start or end focus", "Turns the focus overlay on or off.", hotkeys.ToggleFocus, ApplyHotkey)
        };
        OnPropertyChanged(nameof(HotkeyRows));
    }

    private void ApplyHotkey(HotkeyRowViewModel row)
    {
        var gesture = HotkeyGesture.Parse(row.Gesture);
        if (gesture is null)
        {
            row.Error = "Use a combination such as Ctrl+Alt+F.";
            return;
        }

        row.Error = string.Empty;
        var hotkeys = _services.Settings.Settings.Hotkeys;
        switch (row.Key)
        {
            case "toggleCare": hotkeys.ToggleCare = gesture.Display; break;
            case "brightnessUp": hotkeys.BrightnessUp = gesture.Display; break;
            case "brightnessDown": hotkeys.BrightnessDown = gesture.Display; break;
            case "warmer": hotkeys.Warmer = gesture.Display; break;
            case "cooler": hotkeys.Cooler = gesture.Display; break;
            case "takeBreak": hotkeys.TakeBreak = gesture.Display; break;
            case "showWindow": hotkeys.ShowWindow = gesture.Display; break;
            case "toggleFocus": hotkeys.ToggleFocus = gesture.Display; break;
        }

        _services.Settings.SaveDebounced();
        _services.Hotkeys.Rebuild();
        UpdateConflicts();
    }

    private void ResetHotkeys()
    {
        var defaults = new HotkeySettings();
        var settings = _services.Settings.Settings;
        settings.Hotkeys = defaults;
        _services.Settings.SaveDebounced();
        BuildHotkeyRows();
        _services.Hotkeys.Rebuild();
        UpdateConflicts();
        Message = "Global shortcuts restored to their defaults.";
    }

    private void UpdateConflicts()
    {
        HotkeyConflicts = _services.Hotkeys.Conflicts.Count == 0
            ? string.Empty
            : string.Join(" ", _services.Hotkeys.Conflicts);
    }

    public void RefreshPage()
    {
        RefreshDiagnostics();
        UpdateConflicts();
    }

    private void RefreshDiagnostics()
    {
        var rows = new List<DiagnosticRow>();
        foreach (var state in _services.Comfort.States)
        {
            rows.Add(new DiagnosticRow
            {
                Name = state.Info.Name,
                Detail = $"{state.Info.Resolution} / {state.Info.AdapterName} / {state.Info.DeviceName}",
                ControlPath = state.Info.ControlPath switch
                {
                    DisplayControlPath.HardwareDdc => "Hardware DDC/CI",
                    DisplayControlPath.SoftwareGamma => "Software gamma",
                    _ => "Unavailable"
                },
                Status = state.Status
            });
        }

        DiagnosticRows = rows;
        DiagnosticsText = BuildDiagnostics(rows);
        OnPropertyChanged(nameof(DiagnosticRows));
        OnPropertyChanged(nameof(DiagnosticsText));
    }

    private string BuildDiagnostics(IReadOnlyList<DiagnosticRow> rows)
    {
        var lines = new List<string>
        {
            "MonitorMate diagnostics",
            $"Version: {VersionText}",
            $"Windows: {Environment.OSVersion}",
            $"Displays: {rows.Count}",
            $"Schedule: {(_services.Settings.Settings.ScheduleEnabled ? "on" : "off")}",
            $"Breaks: {(_services.Settings.Settings.Breaks.Enabled ? "on" : "off")}",
            $"Hotkeys: {(_services.Settings.Settings.Hotkeys.Enabled ? "on" : "off")}",
            ""
        };
        foreach (var row in rows)
        {
            lines.Add($"{row.Name} | {row.Detail} | {row.ControlPath} | {row.Status}");
        }

        lines.Add("");
        lines.Add("MonitorMate never uploads this file. Review it before sharing.");
        return string.Join(Environment.NewLine, lines);
    }

    private void CopyDiagnostics()
    {
        try
        {
            Clipboard.SetText(DiagnosticsText);
            Message = "Diagnostics copied. Review the text before sharing it.";
        }
        catch (Exception)
        {
            Message = "Clipboard access was unavailable.";
        }
    }

    private void ExportDiagnostics()
    {
        try
        {
            File.WriteAllText(AppPaths.DiagnosticsFile, DiagnosticsText);
            Message = $"Diagnostics written to {AppPaths.DiagnosticsFile}";
        }
        catch (Exception ex)
        {
            Message = $"Could not write the diagnostics file: {ex.Message}";
        }
    }

    private void OpenDataFolder()
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(AppPaths.DataDirectory) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Message = $"Could not open the data folder: {ex.Message}";
        }
    }

    private void RecaptureBaselines()
    {
        var count = 0;
        foreach (var state in _services.Comfort.States)
        {
            if (state.Info.GammaSupported)
            {
                _services.Gamma.RecaptureBaseline(state.Info);
                count++;
            }
        }

        Message = count == 0
            ? "No display exposed a readable gamma ramp."
            : $"Captured a fresh colour baseline for {count} display{(count == 1 ? string.Empty : "s")}.";
        RefreshDiagnostics();
    }

    private async Task RefreshDisplaysAsync()
    {
        IsBusy = true;
        try
        {
            await _services.Comfort.RefreshAsync();
            RefreshDiagnostics();
            Message = "Displays refreshed.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ResetHistory()
    {
        if (!_confirm("Reset activity history?", "This removes the local screen-time and break history from MonitorMate. It cannot be undone.", "Reset history"))
        {
            return;
        }

        _services.Usage.ResetAll();
        Message = "Activity history cleared.";
    }

    private void ResetSettings()
    {
        if (!_confirm("Reset all settings?", "MonitorMate will return to its first-run settings. Your local activity history is kept. The app will restart.", "Reset and restart"))
        {
            return;
        }

        _services.Settings.Replace(new AppSettings());
        _requestRestart();
    }
}
