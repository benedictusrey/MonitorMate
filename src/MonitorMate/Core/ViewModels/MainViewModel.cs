using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using MonitorMate.Core.Models;
using MonitorMate.Core.Mvvm;
using MonitorMate.Core.Services;

namespace MonitorMate.Core.ViewModels;

public sealed class MainViewModel : ObservableObject, IDisposable
{
    private readonly AppServices _services;
    private readonly Action _showWindow;
    private readonly Action _hideWindow;
    private readonly Action _requestExit;
    private readonly Func<string, string, string, bool> _confirm;
    private readonly Action _requestRestart;
    private NavItem? _selectedNav;
    private object? _currentPage;
    private bool _isBusy = true;
    private bool _isInitialized;
    private string _loadingText = "Reading your displaysâ€¦";
    private string _statusHeadline = "Getting ready";
    private string _statusDetail = "MonitorMate is checking the display control paths.";
    private string _nextBreakText = "Reminders are starting";
    private string _pauseLabel = "Pause care";
    private string _trayTooltip = "MonitorMate";
    private string? _initializationError;
    private bool _hiddenForFocus;

    public MainViewModel(AppServices services, Action showWindow, Action hideWindow, Action requestExit,
        Func<string, string, string, bool> confirm, Action requestRestart)
    {
        _services = services;
        _showWindow = showWindow;
        _hideWindow = hideWindow;
        _requestExit = requestExit;
        _confirm = confirm;
        _requestRestart = requestRestart;

        TodayPage = new TodayViewModel(services, Navigate, OnSetupFinished, ConfirmAutomaticDisplayControl);
        DisplaysPage = new DisplaysViewModel(services);
        ComfortPage = new ComfortViewModel(services, ConfirmAutomaticDisplayControl);
        BreaksPage = new BreaksViewModel(services);
        FocusPage = new FocusViewModel(services);
        ActivityPage = new ActivityViewModel(services, RequestResetHistory);
        SettingsPage = new SettingsViewModel(services, confirm, requestRestart, ConfirmAutomaticDisplayControl);

        NavItems = new ObservableCollection<NavItem>
        {
            new("today", "Today", Icon("IconSun"), TodayPage),
            new("displays", "Displays", Icon("IconMonitor"), DisplaysPage),
            new("comfort", "Comfort", Icon("IconSliders"), ComfortPage),
            new("breaks", "Breaks", Icon("IconCoffee"), BreaksPage),
            new("focus", "Focus", Icon("IconFocus"), FocusPage),
            new("activity", "Activity", Icon("IconChart"), ActivityPage),
            new("settings", "Settings", Icon("IconGear"), SettingsPage)
        };

        SelectedNav = NavItems[0];
        TogglePauseCommand = new RelayCommand(TogglePause);
        TakeBreakCommand = new RelayCommand(TakeBreak);
        ShowDisplaysCommand = new RelayCommand(() => Navigate("displays"));
        OpenSettingsCommand = new RelayCommand(() => Navigate("settings"));
        OpenActivityCommand = new RelayCommand(() => Navigate("activity"));
        DismissErrorCommand = new RelayCommand(() => InitializationError = null);
        ToggleFocusCommand = new RelayCommand(ToggleFocus);

        _services.Comfort.StateChanged += (_, _) => UpdateHeader();
        _services.Reminders.StateChanged += (_, _) => UpdateHeader();
        _services.Schedule.SnapshotChanged += (_, _) => UpdateHeader();
        _services.Focus.Started += (_, _) => OnFocusStarted();
        _services.Focus.Ended += (_, _) => OnFocusEnded();
        _services.Focus.StateChanged += (_, _) => UpdateHeader();

        SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
        SystemEvents.PowerModeChanged += OnPowerModeChanged;
        SystemEvents.SessionSwitch += OnSessionSwitch;
        UpdateHeader();
    }

    public ObservableCollection<NavItem> NavItems { get; }
    public TodayViewModel TodayPage { get; }
    public DisplaysViewModel DisplaysPage { get; }
    public ComfortViewModel ComfortPage { get; }
    public BreaksViewModel BreaksPage { get; }
    public FocusViewModel FocusPage { get; }
    public ActivityViewModel ActivityPage { get; }
    public SettingsViewModel SettingsPage { get; }

    public ICommand TogglePauseCommand { get; }
    public ICommand TakeBreakCommand { get; }
    public ICommand ShowDisplaysCommand { get; }
    public ICommand OpenSettingsCommand { get; }
    public ICommand OpenActivityCommand { get; }
    public ICommand DismissErrorCommand { get; }
    public ICommand ToggleFocusCommand { get; }

    public NavItem? SelectedNav
    {
        get => _selectedNav;
        set
        {
            if (!SetProperty(ref _selectedNav, value)) return;
            CurrentPage = value?.Page;
            RefreshCurrentPage();
        }
    }

    public object? CurrentPage
    {
        get => _currentPage;
        private set => SetProperty(ref _currentPage, value);
    }

    public bool IsBusy { get => _isBusy; private set => SetProperty(ref _isBusy, value); }
    public bool IsInitialized { get => _isInitialized; private set => SetProperty(ref _isInitialized, value); }
    public string LoadingText { get => _loadingText; private set => SetProperty(ref _loadingText, value); }
    public string StatusHeadline { get => _statusHeadline; private set => SetProperty(ref _statusHeadline, value); }
    public string StatusDetail { get => _statusDetail; private set => SetProperty(ref _statusDetail, value); }
    public string NextBreakText { get => _nextBreakText; private set => SetProperty(ref _nextBreakText, value); }
    public string PauseLabel { get => _pauseLabel; private set => SetProperty(ref _pauseLabel, value); }
    public string TrayTooltip { get => _trayTooltip; private set => SetProperty(ref _trayTooltip, value); }
    public string? InitializationError { get => _initializationError; private set => SetProperty(ref _initializationError, value); }
    public bool IsPaused => _services.Comfort.IsPaused;
    public bool IsSetupComplete => _services.Settings.Settings.SetupCompleted;
    public bool IsFocusActive => _services.Focus.IsActive;
    public bool ReducedMotion => _services.Settings.Settings.ReducedMotion;
    public string MateLine => !IsSetupComplete
        ? "Let's get your desk comfortable."
        : IsPaused
            ? "I'm resting. Your screen is yours."
            : IsFocusActive
                ? "One thing at a time."
                : "I'm watching the light and the clock.";

    public async Task InitializeAsync()
    {
        if (IsInitialized) return;
        try
        {
            IsBusy = true;
            LoadingText = "Reading your displays and control pathsâ€¦";
            await _services.Comfort.InitializeAsync();
            DisplaysPage.RebuildCards();
            TodayPage.RefreshAll();
            ActivityPage.Refresh();
            SettingsPage.RefreshPage();
            _services.Reminders.Initialize();
            _services.UsageTracker.Start();

            if (_services.Settings.Settings.SetupCompleted && _services.Settings.Settings.AutomaticDisplayControlEnabled)
            {
                if (_services.Comfort.IsPaused)
                {
                    _services.Comfort.RestoreAll();
                }
                else if (_services.Settings.Settings.ScheduleEnabled)
                {
                    _services.Schedule.Start();
                }
                else if (_services.Settings.Settings.RestoreOnLaunch)
                {
                    await _services.Comfort.ApplyPresetAsync(_services.Settings.Settings.LastPresetId);
                }
            }

            if (_services.Settings.LoadWarning is { } warning)
            {
                _services.Overlays.ShowToast(new ToastRequest("Settings recovered", warning));
            }

            UpdateHeader();
            IsInitialized = true;
        }
        catch (Exception ex)
        {
            _services.Log.Error("Initialization failed.", ex);
            InitializationError = "MonitorMate could not finish starting. Your display settings were left alone.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void Navigate(string key)
    {
        var target = NavItems.FirstOrDefault(n => n.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
        if (target is not null)
        {
            SelectedNav = target;
        }
    }

    public void TogglePause()
    {
        var paused = !_services.Comfort.IsPaused;
        _services.Comfort.SetPaused(paused);
        if (!paused)
        {
            _services.Schedule.ResetAppliedKey();
            if (_services.Settings.Settings.ScheduleEnabled && _services.Settings.Settings.SetupCompleted)
            {
                _ = _services.Schedule.ApplyCurrentAsync(force: true);
            }
        }

        UpdateHeader();
    }

    public void TakeBreak()
    {
        if (!_services.Reminders.IsEnabled)
        {
            _services.Reminders.SetEnabled(true);
        }

        _services.Reminders.TakeBreakNow();
        UpdateHeader();
    }

    public void ToggleFocus()
    {
        if (_services.Focus.IsActive)
        {
            _services.Focus.Stop("user");
        }
        else
        {
            _services.Focus.Start();
        }
    }

    public void AdjustBrightness(int delta) => DisplaysPage.AdjustBrightness(delta);
    public void AdjustTemperature(int delta) => DisplaysPage.AdjustTemperature(delta);

    private bool ConfirmAutomaticDisplayControl()
        => _confirm("Allow automatic display changes?",
            "MonitorMate can change brightness and warmth on a schedule, and reapply them after sleep or a display reconnect. " +
            "Launch restore is optional in Settings. Choose Cancel to keep manual controls and reminders. " +
            "This is an app preference; administrator access is not required.", "Allow automatic");

    private void OnSetupFinished()
    {
        _ = ApplyAfterSetupAsync();
    }

    private async Task ApplyAfterSetupAsync()
    {
        try
        {
            if (!_services.Settings.Settings.AutomaticDisplayControlEnabled)
            {
                UpdateHeader();
                return;
            }

            if (_services.Settings.Settings.ScheduleEnabled)
            {
                _services.Schedule.Start();
            }
            else
            {
                await _services.Comfort.ApplyPresetAsync(_services.Settings.Settings.LastPresetId);
            }

            UpdateHeader();
        }
        catch (Exception ex)
        {
            _services.Log.Error("Could not apply the first comfort setup.", ex);
        }
    }

    private async void OnDisplaySettingsChanged(object? sender, EventArgs e)
    {
        await Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            try
            {
                await _services.Comfort.RefreshAsync(applyAfter: _services.Settings.Settings.SetupCompleted && _services.Settings.Settings.AutomaticDisplayControlEnabled && !_services.Comfort.IsPaused);
                DisplaysPage.RebuildCards();
                TodayPage.RefreshAll();
                SettingsPage.RefreshPage();
                UpdateHeader();
            }
            catch (Exception ex)
            {
                _services.Log.Error("Display change refresh failed.", ex);
            }
        });
    }

    private async void OnPowerModeChanged(object? sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode == PowerModes.Suspend)
        {
            _services.Reminders.HandleSuspend();
            _services.UsageTracker.SetSuspended(true);
            return;
        }

        if (e.Mode == PowerModes.Resume)
        {
            _services.Reminders.HandleResume();
            _services.UsageTracker.SetSuspended(false);
            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                if (_services.Settings.Settings.SetupCompleted && _services.Settings.Settings.AutomaticDisplayControlEnabled && !_services.Comfort.IsPaused)
                {
                    await _services.Comfort.ApplyAllAsync(automatic: true);
                }

                UpdateHeader();
            });
        }
    }

    private void OnSessionSwitch(object? sender, SessionSwitchEventArgs e)
    {
        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            var locked = e.Reason is SessionSwitchReason.SessionLock or SessionSwitchReason.SessionLogoff;
            _services.UsageTracker.SetSessionLocked(locked);
            if (locked)
            {
                _services.Reminders.HandleSuspend();
            }
            else if (e.Reason is SessionSwitchReason.SessionUnlock or SessionSwitchReason.SessionLogon)
            {
                _services.Reminders.HandleResume();
            }
        });
    }

    private void OnFocusStarted()
    {
        _hiddenForFocus = false;
        if (_services.Settings.Settings.Focus.HideMainWindow)
        {
            _hideWindow();
            _hiddenForFocus = true;
        }

        UpdateHeader();
    }

    private void OnFocusEnded()
    {
        if (_hiddenForFocus)
        {
            _showWindow();
            _hiddenForFocus = false;
        }

        UpdateHeader();
    }

    private void RefreshCurrentPage()
    {
        switch (SelectedNav?.Key)
        {
            case "today": TodayPage.RefreshAll(); break;
            case "displays": DisplaysPage.RefreshPage(); break;
            case "comfort": ComfortPage.RefreshPage(); break;
            case "breaks": BreaksPage.RefreshPage(); break;
            case "focus": FocusPage.RefreshPage(); break;
            case "activity": ActivityPage.Refresh(); break;
            case "settings": SettingsPage.RefreshPage(); break;
        }
    }

    private void UpdateHeader()
    {
        var setup = _services.Settings.Settings.SetupCompleted;
        var schedule = _services.Schedule.GetSnapshot();
        PauseLabel = _services.Comfort.IsPaused ? "Resume care" : "Pause care";
        NextBreakText = _services.Reminders.NextBreakText;
        if (!setup)
        {
            StatusHeadline = "Let's set up your comfort";
            StatusDetail = "Choose a rhythm and a gentle evening tint. Nothing changes until you do.";
        }
        else if (_services.Comfort.IsPaused)
        {
            StatusHeadline = "Care is paused";
            StatusDetail = "Your displays are showing their original colour.";
        }
        else
        {
            StatusHeadline = schedule.Enabled ? $"{schedule.PeriodName} comfort" : $"{_services.Comfort.CurrentPresetName} comfort";
            var count = _services.Comfort.States.Count;
            var average = count == 0 ? 6500 : (int)Math.Round(_services.Comfort.States.Average(s => s.Preference.TemperatureK));
            StatusDetail = $"{count} display{(count == 1 ? string.Empty : "s")} / {average} K / {_services.Reminders.NextBreakText}";
        }

        TrayTooltip = setup
            ? $"MonitorMate / {StatusHeadline} / {_services.Reminders.NextBreakText}"
            : "MonitorMate / Setup is ready";
        OnPropertyChanged(nameof(IsPaused));
        OnPropertyChanged(nameof(IsSetupComplete));
        OnPropertyChanged(nameof(IsFocusActive));
        OnPropertyChanged(nameof(MateLine));
    }

    private void RequestResetHistory()
    {
        if (!_confirm("Reset activity history?", "This removes the local screen-time and break history. It cannot be undone.", "Reset history"))
        {
            return;
        }

        _services.Usage.ResetAll();
        ActivityPage.Refresh();
        TodayPage.RefreshAll();
    }

    private Geometry Icon(string key)
        => Application.Current.TryFindResource(key) as Geometry ?? Geometry.Empty;

    public void RequestExit() => _requestExit();

    public void Dispose()
    {
        SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
        SystemEvents.PowerModeChanged -= OnPowerModeChanged;
        SystemEvents.SessionSwitch -= OnSessionSwitch;
        TodayPage.Dispose();
        BreaksPage.Dispose();
    }
}

