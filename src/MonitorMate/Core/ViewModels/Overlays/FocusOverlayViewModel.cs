using MonitorMate.Core.Models;
using MonitorMate.Core.Mvvm;

namespace MonitorMate.Core.ViewModels.Overlays;

public sealed class FocusOverlayViewModel : ObservableObject
{
    private int _dimPercent = 58;
    private int _bandHeight = 280;
    private bool _followPointer = true;
    private FocusMode _mode = FocusMode.Reading;

    public FocusMode Mode
    {
        get => _mode;
        set
        {
            if (SetProperty(ref _mode, value))
            {
                OnPropertyChanged(nameof(HintText));
            }
        }
    }

    public int DimPercent
    {
        get => _dimPercent;
        set
        {
            if (SetProperty(ref _dimPercent, value))
            {
                OnPropertyChanged(nameof(DimOpacity));
            }
        }
    }

    public int BandHeight
    {
        get => _bandHeight;
        set => SetProperty(ref _bandHeight, value);
    }

    public bool FollowPointer
    {
        get => _followPointer;
        set => SetProperty(ref _followPointer, value);
    }

    public double DimOpacity => Math.Clamp(DimPercent / 100.0, 0.15, 0.9);
    public string HintText => Mode == FocusMode.Reading ? "Reading focus is on / Esc to finish" : "Spotlight focus is on / Esc to finish";
}
