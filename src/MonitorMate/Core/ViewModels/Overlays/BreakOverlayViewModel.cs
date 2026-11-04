using MonitorMate.Core.Models;
using MonitorMate.Core.Mvvm;

namespace MonitorMate.Core.ViewModels.Overlays;

public sealed class BreakOverlayViewModel : ObservableObject
{
    private int _remainingSeconds;
    private int _totalSeconds;
    private string _prompt = string.Empty;
    private string _subPrompt = string.Empty;
    private string _sessionSummary = string.Empty;

    public int RemainingSeconds
    {
        get => _remainingSeconds;
        set
        {
            if (SetProperty(ref _remainingSeconds, value))
            {
                OnPropertyChanged(nameof(CountdownText));
                OnPropertyChanged(nameof(Progress));
                OnPropertyChanged(nameof(RemainingDescription));
            }
        }
    }

    public int TotalSeconds
    {
        get => _totalSeconds;
        set
        {
            if (SetProperty(ref _totalSeconds, value))
            {
                OnPropertyChanged(nameof(Progress));
            }
        }
    }

    public string Prompt
    {
        get => _prompt;
        set => SetProperty(ref _prompt, value);
    }

    public string SubPrompt
    {
        get => _subPrompt;
        set => SetProperty(ref _subPrompt, value);
    }

    public string SessionSummary
    {
        get => _sessionSummary;
        set => SetProperty(ref _sessionSummary, value);
    }

    public bool IsLongBreak { get; init; }
    public bool CanSkip { get; init; } = true;
    public bool HoldToSkip { get; init; }
    public bool IsPrimary { get; init; }
    public BreakStyle Style { get; init; }

    public string CountdownText => TimeSpan.FromSeconds(Math.Max(0, RemainingSeconds)).ToString(RemainingSeconds >= 3600 ? @"h\:mm\:ss" : @"m\:ss");
    public double Progress => TotalSeconds <= 0 ? 0 : 1.0 - (double)RemainingSeconds / TotalSeconds;
    public string RemainingDescription => RemainingSeconds <= 1 ? "One more breath" : $"{RemainingSeconds} seconds";
    public string LengthLabel => IsLongBreak ? "A longer pause" : "A short pause";
}
