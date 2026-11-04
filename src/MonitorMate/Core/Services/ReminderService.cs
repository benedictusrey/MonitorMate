using System.Media;
using System.Windows.Threading;
using MonitorMate.Core.Interop;
using MonitorMate.Core.Models;
using MonitorMate.Core.Mvvm;
using MonitorMate.Core.ViewModels.Overlays;

namespace MonitorMate.Core.Services;

public sealed class ReminderService : ObservableObject, IDisposable
{
    private readonly SettingsService _settings;
    private readonly OverlayService _overlays;
    private readonly UsageStore _usage;
    private readonly LogService _log;
    private readonly DispatcherTimer _timer;
    private readonly Random _random = new();
    private ReminderPhase _phase = ReminderPhase.Stopped;
    private int _remainingSeconds;
    private int _totalSeconds;
    private bool _isLongBreak;
    private bool _preBreakShown;
    private bool _idlePaused;
    private bool _fullscreenPaused;
    private bool _suspended;
    private int _breakCountSinceLong;
    private DateTime _lastTickAt = DateTime.Now;
    private BreakOverlayViewModel? _currentBreak;
    private bool _disposed;

    public ReminderService(SettingsService settings, OverlayService overlays, UsageStore usage, LogService log)
    {
        _settings = settings;
        _overlays = overlays;
        _usage = usage;
        _log = log;
        _timer = new DispatcherTimer(DispatcherPriority.Normal) { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) => Tick();
    }

    public ReminderPhase Phase => _phase;
    public bool IsEnabled => _settings.Settings.Breaks.Enabled;
    public bool IsRunning => _phase is ReminderPhase.Working or ReminderPhase.Breaking;
    public bool IsPaused => _phase == ReminderPhase.Paused;
    public int RemainingSeconds => _remainingSeconds;
    public int TotalSeconds => _totalSeconds;
    public bool IsLongBreak => _isLongBreak;
    public bool IsBreakOpen => _phase == ReminderPhase.Breaking;
    public BreakOverlayViewModel? CurrentBreak => _currentBreak;
    public bool IsIdlePaused => _idlePaused;
    public bool IsFullscreenPaused => _fullscreenPaused;
    public int CompletedToday => _usage.Today.BreaksCompleted;
    public int SkippedToday => _usage.Today.BreaksSkipped;

    public string PhaseLabel => _phase switch
    {
        ReminderPhase.Working => "Next break",
        ReminderPhase.Breaking => _isLongBreak ? "Long break" : "Short break",
        ReminderPhase.Paused => "Reminders paused",
        _ => "Reminders off"
    };

    public string CountdownText
    {
        get
        {
            var span = TimeSpan.FromSeconds(Math.Max(0, _remainingSeconds));
            return _phase == ReminderPhase.Breaking
                ? span.ToString(@"m\:ss")
                : span.TotalHours >= 1
                    ? span.ToString(@"h\:mm\:ss")
                    : span.ToString(@"mm\:ss");
        }
    }

    public string NextBreakText => _phase switch
    {
        ReminderPhase.Breaking => $"Break ends in {CountdownText}",
        ReminderPhase.Working => $"Next break in {CountdownText}",
        ReminderPhase.Paused => "Break reminders paused",
        _ => "Break reminders are off"
    };

    public double Progress => _totalSeconds <= 0 ? 0 : 1.0 - (double)_remainingSeconds / _totalSeconds;

    public event EventHandler? StateChanged;
    public event EventHandler? BreakStarted;
    public event EventHandler<string>? BreakEnded;

    public void Initialize()
    {
        if (_settings.Settings.Breaks.Enabled && _settings.Settings.Breaks.AutoStart)
        {
            Start();
        }
    }

    public void Start()
    {
        if (_disposed || !IsEnabled)
        {
            return;
        }

        if (_phase == ReminderPhase.Breaking)
        {
            return;
        }

        _phase = ReminderPhase.Working;
        _remainingSeconds = WorkSeconds;
        _totalSeconds = _remainingSeconds;
        _isLongBreak = false;
        _preBreakShown = false;
        _lastTickAt = DateTime.Now;
        _timer.Start();
        RaiseState();
    }

    public void Pause()
    {
        if (_phase is not (ReminderPhase.Working or ReminderPhase.PreBreak))
        {
            return;
        }

        _phase = ReminderPhase.Paused;
        _timer.Stop();
        RaiseState();
    }

    public void Resume()
    {
        if (_phase != ReminderPhase.Paused || !IsEnabled)
        {
            return;
        }

        _phase = ReminderPhase.Working;
        _lastTickAt = DateTime.Now;
        _timer.Start();
        RaiseState();
    }

    public void Stop()
    {
        _timer.Stop();
        CloseBreak();
        _phase = ReminderPhase.Stopped;
        _remainingSeconds = 0;
        _totalSeconds = 0;
        _preBreakShown = false;
        _currentBreak = null;
        RaiseState();
    }

    public void ResetTimer()
    {
        if (_phase == ReminderPhase.Breaking)
        {
            CloseBreak();
        }

        _phase = ReminderPhase.Working;
        _remainingSeconds = WorkSeconds;
        _totalSeconds = _remainingSeconds;
        _isLongBreak = false;
        _preBreakShown = false;
        _lastTickAt = DateTime.Now;
        _timer.Start();
        RaiseState();
    }

    public void SetEnabled(bool enabled)
    {
        _settings.Settings.Breaks.Enabled = enabled;
        _settings.SaveDebounced();
        if (enabled)
        {
            Start();
        }
        else
        {
            Stop();
        }

        OnPropertyChanged(nameof(IsEnabled));
        RaiseState();
    }

    public void TakeBreakNow()
    {
        if (_phase == ReminderPhase.Breaking || !IsEnabled)
        {
            return;
        }

        BeginBreak(manual: true);
    }

    public void DelayBreak(int minutes)
    {
        if (_phase == ReminderPhase.Breaking)
        {
            return;
        }

        _overlays.CloseToast();
        _phase = ReminderPhase.Working;
        _remainingSeconds = Math.Clamp(minutes, 1, 30) * 60;
        _totalSeconds = _remainingSeconds;
        _preBreakShown = false;
        _lastTickAt = DateTime.Now;
        _timer.Start();
        RaiseState();
    }

    public void ApplySettings(bool restartTimer = false)
    {
        if (restartTimer && _phase != ReminderPhase.Breaking)
        {
            ResetTimer();
            return;
        }

        if (_phase == ReminderPhase.Working)
        {
            var work = WorkSeconds;
            if (_remainingSeconds > work)
            {
                _remainingSeconds = work;
                _totalSeconds = work;
            }
        }

        _settings.SaveDebounced();
        RaiseState();
    }

    public void HandleSuspend()
    {
        _suspended = true;
        _lastTickAt = DateTime.Now;
    }

    public void HandleResume()
    {
        _suspended = false;
        _lastTickAt = DateTime.Now;
        if (_phase == ReminderPhase.Working && _settings.Settings.Breaks.RestartAfterIdle)
        {
            _remainingSeconds = WorkSeconds;
            _totalSeconds = _remainingSeconds;
            _preBreakShown = false;
        }

        RaiseState();
    }

    private int WorkSeconds => Math.Max(60, _settings.Settings.Breaks.WorkMinutes * 60);

    private void Tick()
    {
        if (_disposed || _phase is ReminderPhase.Stopped or ReminderPhase.Paused || _suspended)
        {
            return;
        }

        var now = DateTime.Now;
        var elapsed = now - _lastTickAt;
        _lastTickAt = now;
        if (elapsed > TimeSpan.FromSeconds(8))
        {
            // The machine was asleep or the UI thread was blocked. Do not
            // subtract a large block of time from the user's rhythm.
            return;
        }

        if (_phase == ReminderPhase.Breaking)
        {
            _remainingSeconds = Math.Max(0, _remainingSeconds - 1);
            if (_currentBreak is not null)
            {
                _currentBreak.RemainingSeconds = _remainingSeconds;
            }

            if (_remainingSeconds <= 0)
            {
                CompleteBreak();
            }
            else
            {
                RaiseState();
            }

            return;
        }

        var breaks = _settings.Settings.Breaks;
        if (breaks.PauseWhenIdle)
        {
            var idle = ActivityProbe.GetIdleTime();
            if (idle >= TimeSpan.FromMinutes(Math.Clamp(breaks.IdleMinutes, 1, 60)))
            {
                if (!_idlePaused)
                {
                    _idlePaused = true;
                    RaiseState();
                }

                return;
            }

            if (_idlePaused)
            {
                _idlePaused = false;
                if (breaks.RestartAfterIdle)
                {
                    _remainingSeconds = WorkSeconds;
                    _totalSeconds = _remainingSeconds;
                    _preBreakShown = false;
                }
            }
        }

        if (breaks.PauseInFullscreen && ActivityProbe.IsFullscreenOrPresenting())
        {
            if (!_fullscreenPaused)
            {
                _fullscreenPaused = true;
                RaiseState();
            }

            return;
        }

        if (_fullscreenPaused)
        {
            _fullscreenPaused = false;
        }

        _remainingSeconds = Math.Max(0, _remainingSeconds - 1);

        if (!_preBreakShown && breaks.PreBreakSeconds > 0 && _remainingSeconds <= breaks.PreBreakSeconds)
        {
            ShowPreBreakPrompt(breaks.PreBreakSeconds);
        }

        if (_remainingSeconds <= 0)
        {
            BeginBreak(manual: false);
        }
        else
        {
            RaiseState();
        }
    }

    private void ShowPreBreakPrompt(int seconds)
    {
        _preBreakShown = true;
        _overlays.ShowToast(new ToastRequest(
            "Time to look away",
            $"Your {(IsLongBreak ? "long " : string.Empty)}break starts in {seconds} seconds. Look across the room and let your eyes soften.",
            "Take break now",
            TakeBreakNow,
            "Later 5 min",
            () => DelayBreak(5)));
    }

    private void BeginBreak(bool manual)
    {
        _lastTickAt = DateTime.Now;
        _timer.Start();
        var breaks = _settings.Settings.Breaks;
        var longBreak = !manual && _breakCountSinceLong + 1 >= Math.Clamp(breaks.LongBreakEvery, 2, 12);
        var duration = longBreak
            ? Math.Max(60, breaks.LongBreakMinutes * 60)
            : Math.Max(5, breaks.ShortBreakSeconds);
        var style = manual ? (breaks.Style == BreakStyle.Gentle ? BreakStyle.Gentle : BreakStyle.Fullscreen) : breaks.Style;

        _phase = ReminderPhase.Breaking;
        _isLongBreak = longBreak;
        _totalSeconds = duration;
        _remainingSeconds = duration;
        _preBreakShown = false;
        _overlays.CloseToast();

        var (prompt, subPrompt) = PickPrompt(longBreak);
        var viewModel = new BreakOverlayViewModel
        {
            TotalSeconds = duration,
            RemainingSeconds = duration,
            IsLongBreak = longBreak,
            CanSkip = style != BreakStyle.Strict,
            HoldToSkip = style != BreakStyle.Strict && breaks.HoldToSkip,
            Style = style,
            Prompt = prompt,
            SubPrompt = subPrompt,
            SessionSummary = $"{Math.Max(1, _usage.Today.ScreenMinutes)} minutes of screen time today"
        };

        _currentBreak = viewModel;
        if (style == BreakStyle.Gentle)
        {
            _overlays.ShowGentleBreak(viewModel, SkipBreak);
        }
        else
        {
            _overlays.ShowBreak(viewModel, SkipBreak);
        }

        _usage.RecordReminder();
        PlaySound(start: true);
        BreakStarted?.Invoke(this, EventArgs.Empty);
        RaiseState();
    }

    private (string Prompt, string SubPrompt) PickPrompt(bool longBreak)
    {
        if (!longBreak && _settings.Settings.Breaks.WorkMinutes == 20 && _settings.Settings.Breaks.ShortBreakSeconds == 20)
        {
            return ("Look at something 20 feet away", "For 20 seconds, let the screen wait.");
        }

        var shortPrompts = new[]
        {
            ("Look at something 20 feet away", "Let the distance do the work. Soften your gaze."),
            ("Let your eyes reset", "Look across the room, then blink slowly."),
            ("Blink slowly and relax", "Drop your shoulders and let your jaw loosen."),
            ("Look away from the screen", "A few slow breaths before you return."),
            ("Rest your eyes and breathe", "Focus on something far away, not the pixels.")
        };
        var longPrompts = new[]
        {
            ("Step back from the screen", "Stretch gently while your eyes rest."),
            ("Look outside or across the room", "Give your focus a wider place to land."),
            ("Refresh your focus", "Roll your shoulders, breathe, and look into the distance."),
            ("Let your eyes rest", "Close them briefly if that feels comfortable.")
        };
        var set = longBreak ? longPrompts : shortPrompts;
        return set[_random.Next(set.Length)];
    }

    private void CompleteBreak()
    {
        var wasLong = _isLongBreak;
        var duration = _totalSeconds;
        CloseBreak();
        _usage.RecordBreakCompleted();
        _usage.AddBreakSeconds(duration);

        _breakCountSinceLong = wasLong ? 0 : _breakCountSinceLong + 1;
        _phase = ReminderPhase.Working;
        _remainingSeconds = WorkSeconds;
        _totalSeconds = _remainingSeconds;
        _isLongBreak = false;
        _preBreakShown = false;
        _lastTickAt = DateTime.Now;
        PlaySound(start: false);
        if (_settings.Settings.Breaks.LockAtEnd)
        {
            try
            {
                NativeMethods.LockWorkStation();
            }
            catch (Exception ex)
            {
                _log.Warn($"Could not lock Windows after the break: {ex.Message}");
            }
        }

        BreakEnded?.Invoke(this, "completed");
        RaiseState();
    }

    private void SkipBreak()
    {
        if (_phase != ReminderPhase.Breaking || _currentBreak is not { CanSkip: true })
        {
            return;
        }

        var duration = Math.Max(0, _totalSeconds - _remainingSeconds);
        CloseBreak();
        _usage.RecordBreakSkipped();
        _usage.AddBreakSeconds(duration);

        _breakCountSinceLong++;
        _phase = ReminderPhase.Working;
        _remainingSeconds = WorkSeconds;
        _totalSeconds = _remainingSeconds;
        _isLongBreak = false;
        _preBreakShown = false;
        _lastTickAt = DateTime.Now;
        BreakEnded?.Invoke(this, "skipped");
        RaiseState();
    }

    private void CloseBreak()
    {
        _overlays.CloseBreak();
        _currentBreak = null;
    }

    private void PlaySound(bool start)
    {
        if (!_settings.Settings.SoundEnabled || !_settings.Settings.Breaks.SoundEnabled)
        {
            return;
        }

        try
        {
            if (start)
            {
                SystemSounds.Asterisk.Play();
            }
            else
            {
                SystemSounds.Exclamation.Play();
            }
        }
        catch
        {
            // Sound is optional.
        }
    }

    private void RaiseState()
    {
        OnPropertyChanged(nameof(Phase));
        OnPropertyChanged(nameof(IsRunning));
        OnPropertyChanged(nameof(IsPaused));
        OnPropertyChanged(nameof(IsBreakOpen));
        OnPropertyChanged(nameof(IsLongBreak));
        OnPropertyChanged(nameof(RemainingSeconds));
        OnPropertyChanged(nameof(TotalSeconds));
        OnPropertyChanged(nameof(PhaseLabel));
        OnPropertyChanged(nameof(CountdownText));
        OnPropertyChanged(nameof(NextBreakText));
        OnPropertyChanged(nameof(Progress));
        OnPropertyChanged(nameof(CompletedToday));
        OnPropertyChanged(nameof(SkippedToday));
        OnPropertyChanged(nameof(IsIdlePaused));
        OnPropertyChanged(nameof(IsFullscreenPaused));
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _timer.Stop();
        _overlays.CloseBreak();
    }
}
