using System.Windows.Input;
using System.Windows.Threading;
using MonitorMate.Core.Models;
using MonitorMate.Core.Mvvm;
using MonitorMate.Core.Services;
using MonitorMate.Core.ViewModels.Overlays;

namespace MonitorMate.Core.ViewModels;

public sealed class RhythmOptionViewModel : ObservableObject
{
    private bool _isCurrent;

    public RhythmOptionViewModel(string id, string name, string detail, ICommand command)
    {
        Id = id;
        Name = name;
        Detail = detail;
        ApplyCommand = command;
    }

    public string Id { get; }
    public string Name { get; }
    public string Detail { get; }
    public ICommand ApplyCommand { get; }
    public bool IsCurrent { get => _isCurrent; set => SetProperty(ref _isCurrent, value); }
}

public sealed class BreaksViewModel : ObservableObject, IDisposable
{
    private readonly AppServices _services;
    private readonly DispatcherTimer _previewTimer;
    private bool _suppressApply;
    private int _workMinutes;
    private int _shortBreakSeconds;
    private int _longBreakMinutes;
    private int _longBreakEvery;
    private int _preBreakSeconds;
    private int _idleMinutes;
    private BreakStyle _style;
    private bool _soundEnabled;
    private bool _pauseWhenIdle;
    private bool _restartAfterIdle;
    private bool _pauseInFullscreen;
    private bool _holdToSkip;
    private bool _lockAtEnd;
    private bool _isEnabled;
    private bool _isPreviewing;
    private string? _message;
    private string _countdownText = "20:00";
    private string _phaseLabel = "Next break";
    private string _nextBreakText = "Break reminders are off";
    private string _adherenceText = "No breaks yet";
    private double _progress;
    private int _completedToday;
    private int _skippedToday;
    private bool _isIdlePaused;
    private bool _isFullscreenPaused;

    public BreaksViewModel(AppServices services)
    {
        _services = services;
        TakeBreakCommand = new RelayCommand(TakeBreak);
        ResetCommand = new RelayCommand(ResetTimer);
        PreviewCommand = new RelayCommand(PreviewBreak);
        ApplyRhythmCommand = new RelayCommand<string>(ApplyRhythm);
        _services.Reminders.StateChanged += (_, _) => RefreshLive();
        _previewTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
        _previewTimer.Tick += (_, _) => EndPreview();
        LoadFromSettings();
        RefreshLive();
    }

    public ICommand TakeBreakCommand { get; }
    public ICommand ResetCommand { get; }
    public ICommand PreviewCommand { get; }
    public ICommand ApplyRhythmCommand { get; }

    public IReadOnlyList<RhythmOptionViewModel> RhythmOptions { get; private set; } = Array.Empty<RhythmOptionViewModel>();

    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (!SetProperty(ref _isEnabled, value)) return;
            _services.Reminders.SetEnabled(value);
            _services.Settings.SaveDebounced();
            RefreshLive();
        }
    }

    public int WorkMinutes
    {
        get => _workMinutes;
        set
        {
            if (!SetProperty(ref _workMinutes, Math.Clamp(value, 5, 90))) return;
            ApplyNumericSetting(() => _services.Settings.Settings.Breaks.WorkMinutes = _workMinutes);
        }
    }

    public int ShortBreakSeconds
    {
        get => _shortBreakSeconds;
        set
        {
            if (!SetProperty(ref _shortBreakSeconds, Math.Clamp(value, 5, 300))) return;
            ApplyNumericSetting(() => _services.Settings.Settings.Breaks.ShortBreakSeconds = _shortBreakSeconds);
        }
    }

    public int LongBreakMinutes
    {
        get => _longBreakMinutes;
        set
        {
            if (!SetProperty(ref _longBreakMinutes, Math.Clamp(value, 1, 30))) return;
            ApplyNumericSetting(() => _services.Settings.Settings.Breaks.LongBreakMinutes = _longBreakMinutes);
        }
    }

    public int LongBreakEvery
    {
        get => _longBreakEvery;
        set
        {
            if (!SetProperty(ref _longBreakEvery, Math.Clamp(value, 2, 12))) return;
            ApplyNumericSetting(() => _services.Settings.Settings.Breaks.LongBreakEvery = _longBreakEvery);
        }
    }

    public int PreBreakSeconds
    {
        get => _preBreakSeconds;
        set
        {
            if (!SetProperty(ref _preBreakSeconds, Math.Clamp(value, 0, 60))) return;
            ApplyNumericSetting(() => _services.Settings.Settings.Breaks.PreBreakSeconds = _preBreakSeconds);
        }
    }

    public int IdleMinutes
    {
        get => _idleMinutes;
        set
        {
            if (!SetProperty(ref _idleMinutes, Math.Clamp(value, 1, 15))) return;
            ApplyNumericSetting(() => _services.Settings.Settings.Breaks.IdleMinutes = _idleMinutes);
        }
    }

    public BreakStyle Style
    {
        get => _style;
        set
        {
            if (!SetProperty(ref _style, value)) return;
            _services.Settings.Settings.Breaks.Style = value;
            _services.Settings.SaveDebounced();
            OnPropertyChanged(nameof(StyleDescription));
        }
    }

    public string StyleDescription => Style switch
    {
        BreakStyle.Gentle => "A small reminder that never covers your work.",
        BreakStyle.Fullscreen => "A calm full-screen pause you can skip.",
        _ => "A full-screen pause that cannot be skipped."
    };

    public bool SoundEnabled
    {
        get => _soundEnabled;
        set
        {
            if (!SetProperty(ref _soundEnabled, value)) return;
            _services.Settings.Settings.Breaks.SoundEnabled = value;
            _services.Settings.SaveDebounced();
        }
    }

    public bool PauseWhenIdle
    {
        get => _pauseWhenIdle;
        set
        {
            if (!SetProperty(ref _pauseWhenIdle, value)) return;
            _services.Settings.Settings.Breaks.PauseWhenIdle = value;
            _services.Settings.SaveDebounced();
        }
    }

    public bool RestartAfterIdle
    {
        get => _restartAfterIdle;
        set
        {
            if (!SetProperty(ref _restartAfterIdle, value)) return;
            _services.Settings.Settings.Breaks.RestartAfterIdle = value;
            _services.Settings.SaveDebounced();
        }
    }

    public bool PauseInFullscreen
    {
        get => _pauseInFullscreen;
        set
        {
            if (!SetProperty(ref _pauseInFullscreen, value)) return;
            _services.Settings.Settings.Breaks.PauseInFullscreen = value;
            _services.Settings.SaveDebounced();
        }
    }

    public bool HoldToSkip
    {
        get => _holdToSkip;
        set
        {
            if (!SetProperty(ref _holdToSkip, value)) return;
            _services.Settings.Settings.Breaks.HoldToSkip = value;
            _services.Settings.SaveDebounced();
        }
    }

    public bool LockAtEnd
    {
        get => _lockAtEnd;
        set
        {
            if (!SetProperty(ref _lockAtEnd, value)) return;
            _services.Settings.Settings.Breaks.LockAtEnd = value;
            _services.Settings.SaveDebounced();
        }
    }

    public string CountdownText { get => _countdownText; private set => SetProperty(ref _countdownText, value); }
    public string PhaseLabel { get => _phaseLabel; private set => SetProperty(ref _phaseLabel, value); }
    public string NextBreakText { get => _nextBreakText; private set => SetProperty(ref _nextBreakText, value); }
    public string AdherenceText { get => _adherenceText; private set => SetProperty(ref _adherenceText, value); }
    public double Progress { get => _progress; private set => SetProperty(ref _progress, value); }
    public int CompletedToday { get => _completedToday; private set => SetProperty(ref _completedToday, value); }
    public int SkippedToday { get => _skippedToday; private set => SetProperty(ref _skippedToday, value); }
    public bool IsIdlePaused { get => _isIdlePaused; private set => SetProperty(ref _isIdlePaused, value); }
    public bool IsFullscreenPaused { get => _isFullscreenPaused; private set => SetProperty(ref _isFullscreenPaused, value); }
    public bool IsPreviewing { get => _isPreviewing; private set => SetProperty(ref _isPreviewing, value); }
    public string? Message { get => _message; private set => SetProperty(ref _message, value); }

    private void LoadFromSettings()
    {
        var breaks = _services.Settings.Settings.Breaks;
        _isEnabled = breaks.Enabled;
        _workMinutes = breaks.WorkMinutes;
        _shortBreakSeconds = breaks.ShortBreakSeconds;
        _longBreakMinutes = breaks.LongBreakMinutes;
        _longBreakEvery = breaks.LongBreakEvery;
        _preBreakSeconds = breaks.PreBreakSeconds;
        _idleMinutes = breaks.IdleMinutes;
        _style = breaks.Style;
        _soundEnabled = breaks.SoundEnabled;
        _pauseWhenIdle = breaks.PauseWhenIdle;
        _restartAfterIdle = breaks.RestartAfterIdle;
        _pauseInFullscreen = breaks.PauseInFullscreen;
        _holdToSkip = breaks.HoldToSkip;
        _lockAtEnd = breaks.LockAtEnd;
        BuildRhythmOptions();
        OnPropertyChanged(nameof(IsEnabled));
        OnPropertyChanged(nameof(WorkMinutes));
        OnPropertyChanged(nameof(ShortBreakSeconds));
        OnPropertyChanged(nameof(LongBreakMinutes));
        OnPropertyChanged(nameof(LongBreakEvery));
        OnPropertyChanged(nameof(PreBreakSeconds));
        OnPropertyChanged(nameof(IdleMinutes));
        OnPropertyChanged(nameof(Style));
        OnPropertyChanged(nameof(SoundEnabled));
        OnPropertyChanged(nameof(PauseWhenIdle));
        OnPropertyChanged(nameof(RestartAfterIdle));
        OnPropertyChanged(nameof(PauseInFullscreen));
        OnPropertyChanged(nameof(HoldToSkip));
        OnPropertyChanged(nameof(LockAtEnd));
    }

    private void BuildRhythmOptions()
    {
        RhythmOptions = new[]
        {
            new RhythmOptionViewModel("20-20-20", "20-20-20", "20 min work / 20 sec look away", ApplyRhythmCommand),
            new RhythmOptionViewModel("pomodoro", "Pomodoro", "25 min work / 5 min break", ApplyRhythmCommand),
            new RhythmOptionViewModel("deep", "Deep work", "50 min work / 10 min break", ApplyRhythmCommand),
            new RhythmOptionViewModel("custom", "Custom", "Tune every interval yourself", ApplyRhythmCommand)
        };
        OnPropertyChanged(nameof(RhythmOptions));
        UpdateRhythmSelection();
    }

    private void ApplyRhythm(string? id)
    {
        _suppressApply = true;
        switch (id)
        {
            case "20-20-20":
                WorkMinutes = 20;
                ShortBreakSeconds = 20;
                LongBreakMinutes = 5;
                LongBreakEvery = 4;
                break;
            case "pomodoro":
                WorkMinutes = 25;
                ShortBreakSeconds = 300;
                LongBreakMinutes = 15;
                LongBreakEvery = 4;
                break;
            case "deep":
                WorkMinutes = 50;
                ShortBreakSeconds = 600;
                LongBreakMinutes = 15;
                LongBreakEvery = 4;
                break;
        }

        _suppressApply = false;
        ApplyAllBreakSettings();
        _services.Reminders.ResetTimer();
        UpdateRhythmSelection();
        Message = id == "custom" ? "Adjust the intervals below to make your own rhythm." : "Rhythm applied. The timer has restarted.";
        RefreshLive();
    }

    private void ApplyNumericSetting(Action apply)
    {
        apply();
        if (_suppressApply) return;
        _services.Settings.SaveDebounced();
        _services.Reminders.ApplySettings();
        UpdateRhythmSelection();
    }

    private void ApplyAllBreakSettings()
    {
        var breaks = _services.Settings.Settings.Breaks;
        breaks.WorkMinutes = WorkMinutes;
        breaks.ShortBreakSeconds = ShortBreakSeconds;
        breaks.LongBreakMinutes = LongBreakMinutes;
        breaks.LongBreakEvery = LongBreakEvery;
        breaks.PreBreakSeconds = PreBreakSeconds;
        breaks.IdleMinutes = IdleMinutes;
        breaks.Style = Style;
        breaks.SoundEnabled = SoundEnabled;
        breaks.PauseWhenIdle = PauseWhenIdle;
        breaks.RestartAfterIdle = RestartAfterIdle;
        breaks.PauseInFullscreen = PauseInFullscreen;
        breaks.HoldToSkip = HoldToSkip;
        breaks.LockAtEnd = LockAtEnd;
        _services.Settings.SaveDebounced();
    }

    private void UpdateRhythmSelection()
    {
        if (RhythmOptions.Count == 0) return;
        var matches2020 = WorkMinutes == 20 && ShortBreakSeconds == 20 && LongBreakMinutes == 5 && LongBreakEvery == 4;
        var matchesPomodoro = WorkMinutes == 25 && ShortBreakSeconds == 300 && LongBreakMinutes == 15 && LongBreakEvery == 4;
        var matchesDeep = WorkMinutes == 50 && ShortBreakSeconds == 600 && LongBreakMinutes == 15 && LongBreakEvery == 4;
        foreach (var option in RhythmOptions)
        {
            option.IsCurrent = option.Id switch
            {
                "20-20-20" => matches2020,
                "pomodoro" => matchesPomodoro,
                "deep" => matchesDeep,
                _ => !matches2020 && !matchesPomodoro && !matchesDeep
            };
        }
    }

    private void TakeBreak()
    {
        if (!_services.Reminders.IsEnabled)
        {
            _services.Reminders.SetEnabled(true);
        }

        _services.Reminders.TakeBreakNow();
    }

    private void ResetTimer()
    {
        _services.Reminders.ResetTimer();
        Message = "The work interval has restarted.";
        RefreshLive();
    }

    private void PreviewBreak()
    {
        if (IsPreviewing)
        {
            EndPreview();
            return;
        }

        IsPreviewing = true;
        var style = Style == BreakStyle.Gentle ? BreakStyle.Gentle : BreakStyle.Fullscreen;
        var vm = new BreakOverlayViewModel
        {
            TotalSeconds = 10,
            RemainingSeconds = 10,
            IsLongBreak = false,
            CanSkip = true,
            HoldToSkip = false,
            Style = style,
            Prompt = "Look at something 20 feet away",
            SubPrompt = "This is a preview. Your real break will follow the rhythm above.",
            SessionSummary = "Preview only"
        };
        if (style == BreakStyle.Gentle)
        {
            _services.Overlays.ShowGentleBreak(vm, EndPreview);
        }
        else
        {
            _services.Overlays.ShowBreak(vm, EndPreview);
        }

        _previewTimer.Start();
    }

    private void EndPreview()
    {
        _previewTimer.Stop();
        _services.Overlays.CloseBreak();
        IsPreviewing = false;
    }

    private void RefreshLive()
    {
        var reminders = _services.Reminders;
        CountdownText = reminders.CountdownText;
        PhaseLabel = reminders.PhaseLabel;
        NextBreakText = reminders.NextBreakText;
        Progress = reminders.Progress;
        CompletedToday = reminders.CompletedToday;
        SkippedToday = reminders.SkippedToday;
        IsIdlePaused = reminders.IsIdlePaused;
        IsFullscreenPaused = reminders.IsFullscreenPaused;
        var total = CompletedToday + SkippedToday;
        AdherenceText = total == 0
            ? "No breaks yet today"
            : $"{CompletedToday} of {total} reminders completed";
        OnPropertyChanged(nameof(IsEnabled));
    }

    public void Dispose()
    {
        _previewTimer.Stop();
        _services.Overlays.CloseBreak();
    }

    public void RefreshPage() => RefreshLive();
}
