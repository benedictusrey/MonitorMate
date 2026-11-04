using System.Windows.Input;
using MonitorMate.Core.Mvvm;

namespace MonitorMate.Core.ViewModels.Overlays;

public sealed class ToastViewModel : ObservableObject
{
    private string _title = string.Empty;
    private string _message = string.Empty;
    private string _primaryLabel = string.Empty;
    private string _secondaryLabel = string.Empty;
    private bool _hasPrimary;
    private bool _hasSecondary;

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public string Message
    {
        get => _message;
        set => SetProperty(ref _message, value);
    }

    public string PrimaryLabel
    {
        get => _primaryLabel;
        set => SetProperty(ref _primaryLabel, value);
    }

    public string SecondaryLabel
    {
        get => _secondaryLabel;
        set => SetProperty(ref _secondaryLabel, value);
    }

    public bool HasPrimary
    {
        get => _hasPrimary;
        set => SetProperty(ref _hasPrimary, value);
    }

    public bool HasSecondary
    {
        get => _hasSecondary;
        set => SetProperty(ref _hasSecondary, value);
    }

    public ICommand? PrimaryCommand { get; set; }
    public ICommand? SecondaryCommand { get; set; }
}
