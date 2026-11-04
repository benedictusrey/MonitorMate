using System.Windows.Input;
using MonitorMate.Core.Models;
using MonitorMate.Core.Mvvm;
using MonitorMate.Core.Services;

namespace MonitorMate.Core.ViewModels;

public sealed class FocusViewModel : ObservableObject
{
    private readonly AppServices _services;
    private FocusMode _mode;
    private int _dimPercent;
    private int _bandHeight;
    private bool _followPointer;
    private int _defaultMinutes;
    private bool _hideMainWindow;
    private bool _isActive;
    private string _remainingText = "25:00";
    private double _progress;
    private string _summary = string.Empty;

    public FocusViewModel(AppServices services)
    {
        _services = services;
        StartStopCommand = new RelayCommand(StartOrStop);
        SetDurationCommand = new RelayCommand<int>(SetDuration);
        _services.Focus.StateChanged += (_, _) => RefreshLive();
        _services.Focus.Ended += (_, reason) => OnEnded(reason);
        LoadFromSettings();
        RefreshLive();
    }

    public ICommand StartStopCommand { get; }
    public ICommand SetDurationCommand { get; }
    public IReadOnlyList<int> DurationOptions { get; } = [15, 25, 45, 60];

    public FocusMode Mode
    {
        get => _mode;
        set
        {
            if (!SetProperty(ref _mode, value)) return;
            _services.Settings.Settings.Focus.Mode = value;
            _services.Settings.SaveDebounced();
            _services.Focus.UpdateSettings();
            OnPropertyChanged(nameof(ModeDescription));
        }
    }

    public int DimPercent
    {
        get => _dimPercent;
        set
        {
            if (!SetProperty(ref _dimPercent, Math.Clamp(value, 20, 85))) return;
            _services.Settings.Settings.Focus.DimPercent = _dimPercent;
            _services.Settings.SaveDebounced();
            _services.Focus.UpdateSettings();
        }
    }

    public int BandHeight
    {
        get => _bandHeight;
        set
        {
            if (!SetProperty(ref _bandHeight, Math.Clamp(value, 120, 500))) return;
            _services.Settings.Settings.Focus.BandHeight = _bandHeight;
            _services.Settings.SaveDebounced();
            _services.Focus.UpdateSettings();
        }
    }

    public bool FollowPointer
    {
        get => _followPointer;
        set
        {
            if (!SetProperty(ref _followPointer, value)) return;
            _services.Settings.Settings.Focus.FollowPointer = value;
            _services.Settings.SaveDebounced();
            _services.Focus.UpdateSettings();
        }
    }

    public int DefaultMinutes
    {
        get => _defaultMinutes;
        set
        {
            if (!SetProperty(ref _defaultMinutes, Math.Clamp(value, 5, 180))) return;
            _services.Settings.Settings.Focus.DefaultMinutes = _defaultMinutes;
            _services.Settings.SaveDebounced();
            OnPropertyChanged(nameof(DurationText));
        }
    }

    public bool HideMainWindow
    {
        get => _hideMainWindow;
        set
        {
            if (!SetProperty(ref _hideMainWindow, value)) return;
            _services.Settings.Settings.Focus.HideMainWindow = value;
            _services.Settings.SaveDebounced();
        }
    }

    public bool IsActive { get => _isActive; private set => SetProperty(ref _isActive, value); }
    public string RemainingText { get => _remainingText; private set => SetProperty(ref _remainingText, value); }
    public double Progress { get => _progress; private set => SetProperty(ref _progress, value); }
    public string Summary { get => _summary; private set => SetProperty(ref _summary, value); }
    public string StartStopLabel => IsActive ? "End focus" : "Start focus";
    public string DurationText => $"{DefaultMinutes} minutes";
    public string ModeDescription => Mode == FocusMode.Reading
        ? "Keeps a horizontal band clear while the rest of the screen recedes."
        : "Keeps a soft spotlight around your pointer. Useful for a single task.";
    public string DimText => $"{DimPercent}% dim";

    private void LoadFromSettings()
    {
        var focus = _services.Settings.Settings.Focus;
        _mode = focus.Mode;
        _dimPercent = focus.DimPercent;
        _bandHeight = focus.BandHeight;
        _followPointer = focus.FollowPointer;
        _defaultMinutes = focus.DefaultMinutes;
        _hideMainWindow = focus.HideMainWindow;
        OnPropertyChanged(nameof(Mode));
        OnPropertyChanged(nameof(DimPercent));
        OnPropertyChanged(nameof(BandHeight));
        OnPropertyChanged(nameof(FollowPointer));
        OnPropertyChanged(nameof(DefaultMinutes));
        OnPropertyChanged(nameof(HideMainWindow));
        OnPropertyChanged(nameof(ModeDescription));
        OnPropertyChanged(nameof(DurationText));
        OnPropertyChanged(nameof(DimText));
    }

    private void StartOrStop()
    {
        if (IsActive)
        {
            _services.Focus.Stop("user");
        }
        else
        {
            _services.Focus.Start();
        }

        RefreshLive();
    }

    private void SetDuration(int minutes)
    {
        if (minutes <= 0) return;
        DefaultMinutes = minutes;
    }

    private void RefreshLive()
    {
        IsActive = _services.Focus.IsActive;
        var remaining = IsActive
            ? TimeSpan.FromSeconds(Math.Max(0, _services.Focus.RemainingSeconds))
            : TimeSpan.FromMinutes(Math.Max(1, DefaultMinutes));
        RemainingText = remaining.ToString(@"mm\:ss");
        Progress = IsActive && _services.Focus.TotalSeconds > 0
            ? 1.0 - (double)_services.Focus.RemainingSeconds / _services.Focus.TotalSeconds
            : 0;
        Summary = _services.Usage.Today.FocusMinutes <= 0
            ? "Focus time is counted while a session is active."
            : $"{_services.Usage.Today.FocusMinutes} minutes focused today.";
        OnPropertyChanged(nameof(StartStopLabel));
        OnPropertyChanged(nameof(IsActive));
        OnPropertyChanged(nameof(DurationText));
        OnPropertyChanged(nameof(DimText));
    }

    private void OnEnded(string reason)
    {
        RefreshLive();
        var message = reason switch
        {
            "finished" => "Focus session complete. Take a slow look away from the screen.",
            "escape" => "Focus ended. Your full screen is back.",
            _ => "Focus ended."
        };
        _services.Overlays.ShowToast(new ToastRequest("Focus finished", message));
    }

    public void RefreshPage() => RefreshLive();
}
