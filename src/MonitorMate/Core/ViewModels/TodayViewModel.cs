using System.Windows.Input;
using System.Windows.Threading;
using MonitorMate.Core.Models;
using MonitorMate.Core.Mvvm;
using MonitorMate.Core.Services;

namespace MonitorMate.Core.ViewModels;

public sealed class TodayViewModel : ObservableObject, IDisposable
{
    private readonly AppServices _services;
    private readonly Action<string> _navigate;
    private readonly Action _onSetupFinished;
    private readonly Func<bool> _requestAutomaticControl;
    private readonly DispatcherTimer _clock;
    private string _greeting = "Good morning";
    private string _dateText = string.Empty;
    private string _screenTimeText = "0 m";
    private string _breaksText = "0";
    private string _focusText = "0 m";
    private string _currentPeriod = "Daylight";
    private string _currentPresetName = "Daylight";
    private string _scheduleNextText = string.Empty;
    private string _sunriseText = "07:00";
    private string _sunsetText = "19:00";
    private string _nightStartText = "22:30";
    private string _nowTimeText = string.Empty;
    private string _displaySummary = "No displays";
    private bool _isSetupVisible;
    private string _scheduleWarning = string.Empty;

    public TodayViewModel(AppServices services, Action<string> navigate, Action onSetupFinished, Func<bool> requestAutomaticControl)
    {
        _services = services;
        _navigate = navigate;
        _onSetupFinished = onSetupFinished;
        _requestAutomaticControl = requestAutomaticControl;
        TakeBreakCommand = new RelayCommand(TakeBreak);
        IdentifyCommand = new RelayCommand(() => _services.Overlays.ShowIdentify());
        OpenDisplaysCommand = new RelayCommand(() => _navigate("displays"));
        OpenBreaksCommand = new RelayCommand(() => _navigate("breaks"));
        OpenComfortCommand = new RelayCommand(() => _navigate("comfort"));
        OpenFocusCommand = new RelayCommand(() => _navigate("focus"));
        OpenActivityCommand = new RelayCommand(() => _navigate("activity"));
        OpenSettingsCommand = new RelayCommand(() => _navigate("settings"));
        FinishSetupCommand = new RelayCommand(() => FinishSetup(false));
        EnableAutomaticCommand = new RelayCommand(() => FinishSetup(_requestAutomaticControl()));

        _services.Reminders.StateChanged += (_, _) => RefreshLive();
        _services.Comfort.StateChanged += (_, _) => RefreshLive();
        _services.Focus.StateChanged += (_, _) => RefreshLive();
        _services.Schedule.SnapshotChanged += (_, snapshot) => ApplySchedule(snapshot);
        _clock = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromSeconds(30) };
        _clock.Tick += (_, _) => RefreshLive();
        _clock.Start();
        RefreshAll();
    }

    public string Greeting { get => _greeting; private set => SetProperty(ref _greeting, value); }
    public string DateText { get => _dateText; private set => SetProperty(ref _dateText, value); }
    public string ScreenTimeText { get => _screenTimeText; private set => SetProperty(ref _screenTimeText, value); }
    public string BreaksText { get => _breaksText; private set => SetProperty(ref _breaksText, value); }
    public string FocusText { get => _focusText; private set => SetProperty(ref _focusText, value); }
    public string FocusStatusText => _services.Focus.IsActive ? "Session in progress." : "No session today.";
    public string CurrentPeriod { get => _currentPeriod; private set => SetProperty(ref _currentPeriod, value); }
    public string CurrentPresetName { get => _currentPresetName; private set => SetProperty(ref _currentPresetName, value); }
    public string ScheduleNextText { get => _scheduleNextText; private set => SetProperty(ref _scheduleNextText, value); }
    public string SunriseText { get => _sunriseText; private set => SetProperty(ref _sunriseText, value); }
    public string SunsetText { get => _sunsetText; private set => SetProperty(ref _sunsetText, value); }
    public string NightStartText { get => _nightStartText; private set => SetProperty(ref _nightStartText, value); }
    public string NowTimeText { get => _nowTimeText; private set => SetProperty(ref _nowTimeText, value); }
    public string DisplaySummary { get => _displaySummary; private set => SetProperty(ref _displaySummary, value); }
    public bool IsSetupVisible { get => _isSetupVisible; private set => SetProperty(ref _isSetupVisible, value); }
    public string ScheduleWarning { get => _scheduleWarning; private set => SetProperty(ref _scheduleWarning, value); }
    public double DayStartMinutes => ParseMinutes(SunriseText, 7 * 60);
    public double EveningStartMinutes => ParseMinutes(SunsetText, 19 * 60);
    public double NightStartMinutes => ParseMinutes(_services.Settings.Settings.NightStart, 22 * 60 + 30);
    public double NowMinutes => DateTime.Now.TimeOfDay.TotalMinutes;
    public string ShortcutHint
    {
        get
        {
            var hotkeys = _services.Settings.Settings.Hotkeys;
            return hotkeys.Enabled
                ? $"Pause {hotkeys.ToggleCare} / Break {hotkeys.TakeBreak} / Focus {hotkeys.ToggleFocus}"
                : "Global shortcuts are off.";
        }
    }

    public ICommand TakeBreakCommand { get; }
    public ICommand IdentifyCommand { get; }
    public ICommand OpenDisplaysCommand { get; }
    public ICommand OpenBreaksCommand { get; }
    public ICommand OpenComfortCommand { get; }
    public ICommand OpenFocusCommand { get; }
    public ICommand OpenActivityCommand { get; }
    public ICommand OpenSettingsCommand { get; }
    public ICommand FinishSetupCommand { get; }
    public ICommand EnableAutomaticCommand { get; }

    public void RefreshAll()
    {
        Greeting = DateTime.Now.Hour switch
        {
            < 5 => "Still up?",
            < 12 => "Good morning",
            < 18 => "Good afternoon",
            _ => "Good evening"
        };
        DateText = DateTime.Now.ToString("dddd, d MMMM");
        IsSetupVisible = !_services.Settings.Settings.SetupCompleted;
        NightStartText = _services.Settings.Settings.NightStart;
        ApplySchedule(_services.Schedule.GetSnapshot());
        RefreshLive();
    }

    public void RefreshLive()
    {
        var today = _services.Usage.Today;
        ScreenTimeText = FormatMinutes(today.ScreenMinutes);
        BreaksText = today.BreaksCompleted.ToString();
        FocusText = FormatMinutes(today.FocusMinutes);
        CurrentPresetName = _services.Comfort.CurrentPresetName;
        DisplaySummary = _services.Comfort.States.Count switch
        {
            0 => "No displays",
            1 => "1 display",
            _ => $"{_services.Comfort.States.Count} displays"
        };

        if (_services.Comfort.States.Count > 0)
        {
            var average = (int)Math.Round(_services.Comfort.States.Average(s => s.Preference.TemperatureK));
            DisplaySummary += $" / {average} K";
        }

        NowTimeText = DateTime.Now.ToString("HH:mm");
        OnPropertyChanged(nameof(ShortcutHint));
        OnPropertyChanged(nameof(FocusStatusText));
        OnPropertyChanged(nameof(NowMinutes));
        OnPropertyChanged(nameof(DayStartMinutes));
        OnPropertyChanged(nameof(EveningStartMinutes));
        OnPropertyChanged(nameof(NightStartMinutes));
    }

    private void ApplySchedule(ScheduleSnapshot snapshot)
    {
        CurrentPeriod = snapshot.Enabled ? snapshot.PeriodName : "Manual";
        ScheduleNextText = snapshot.Enabled ? $"Next change {snapshot.NextChangeText}" : "Schedule is off";
        SunriseText = snapshot.SunriseText;
        SunsetText = snapshot.SunsetText;
        ScheduleWarning = snapshot.Warning ?? string.Empty;
    }

    private void TakeBreak()
    {
        if (!_services.Reminders.IsEnabled)
        {
            _services.Reminders.SetEnabled(true);
        }

        _services.Reminders.TakeBreakNow();
    }

    private void FinishSetup(bool automatic)
    {
        var settings = _services.Settings.Settings;
        settings.SetupCompleted = true;
        settings.AutomaticDisplayControlAllowed = automatic;
        settings.ScheduleEnabled = automatic;
        settings.RestoreOnLaunch = false;
        _services.Settings.SaveDebounced();
        IsSetupVisible = false;
        _onSetupFinished();
    }

    private static string FormatMinutes(int minutes)
    {
        if (minutes < 60) return $"{minutes} m";
        return $"{minutes / 60} h {minutes % 60} m";
    }

    private static double ParseMinutes(string? value, double fallback)
        => TimeSpan.TryParse(value, out var time) ? time.TotalMinutes : fallback;

    public void Dispose()
    {
        _clock.Stop();
    }
}

