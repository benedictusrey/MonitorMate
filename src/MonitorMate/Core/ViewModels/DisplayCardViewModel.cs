using System.Windows.Input;
using MonitorMate.Core.Models;
using MonitorMate.Core.Mvvm;

namespace MonitorMate.Core.ViewModels;

public sealed class DisplayCardViewModel : ObservableObject
{
    private readonly Action<DisplayCardViewModel> _changed;
    private bool _suppress;
    private int _brightness;
    private int _temperatureK;
    private int _contrast;
    private int _softwareDim;
    private bool _isSelected;
    private bool _isEnabled;
    private string _statusText = "Ready";
    private string _statusDetail = string.Empty;
    private bool _hasError;
    private bool _isUnconfirmed;

    public DisplayCardViewModel(DisplayState state, Action<DisplayCardViewModel> changed)
    {
        State = state;
        _changed = changed;
        _isEnabled = state.Preference.Enabled;
        UpdateFromState();
        ApplyStatus(state);
    }

    public DisplayState State { get; }
    public DisplayInfo Info => State.Info;
    public string Id => Info.Id;
    public string Name => Info.Name;
    public string NumberLabel => Info.MonitorNumber.ToString("00");
    public string Resolution => Info.Resolution;
    public string AdapterName => Info.AdapterName;
    public string PositionLabel => Info.IsPrimary ? "Primary" : "Secondary";
    public bool IsPrimary => Info.IsPrimary;
    public bool HardwareBrightnessSupported => Info.HardwareBrightnessSupported;
    public bool HardwareContrastSupported => Info.HardwareContrastSupported;
    public bool GammaSupported => Info.GammaSupported;
    public string GammaStatusText => Info.GammaSupported ? "Available" : "Unavailable";
    public bool CanAdjustContrast => Info.HardwareContrastSupported;
    public bool ShowExtraDim => Info.HardwareBrightnessSupported;
    public string BrightnessCaption => Info.HardwareBrightnessSupported ? "Hardware brightness" : "Software brightness";
    public string ControlPathLabel => Info.ControlPath switch
    {
        DisplayControlPath.HardwareDdc => "Hardware DDC/CI",
        DisplayControlPath.SoftwareGamma => "Software gamma",
        _ => "Unavailable"
    };
    public string ControlDetail => Info.ControlPath switch
    {
        DisplayControlPath.HardwareDdc => "Brightness is sent to the monitor. Colour warmth is composed on the GPU ramp.",
        DisplayControlPath.SoftwareGamma => "Brightness and warmth are composed on the GPU gamma ramp. Screenshots stay unchanged.",
        _ => "Windows exposes neither DDC/CI nor a gamma ramp for this display."
    };
    public string BrightnessText => $"{Brightness}%";
    public string TemperatureText => $"{TemperatureK} K";
    public string ContrastText => $"{Contrast}%";
    public string ExtraDimText => SoftwareDim == 0 ? "None" : $"{SoftwareDim}%";
    public string TemperatureDescription => TemperatureK >= 6400 ? "Neutral" : TemperatureK >= 5200 ? "Soft" : TemperatureK >= 4200 ? "Warm" : "Deep warm";
    public string StatusTone => HasError ? "error" : IsUnconfirmed ? "warn" : "ok";

    public int Brightness
    {
        get => _brightness;
        set
        {
            var clamped = Math.Clamp(value, 10, 100);
            if (SetProperty(ref _brightness, clamped))
            {
                OnPropertyChanged(nameof(BrightnessText));
                if (!_suppress)
                {
                    State.Preference.Brightness = clamped;
                    _changed(this);
                }
            }
        }
    }

    public int TemperatureK
    {
        get => _temperatureK;
        set
        {
            var clamped = Math.Clamp(value, 2700, 6500);
            if (SetProperty(ref _temperatureK, clamped))
            {
                OnPropertyChanged(nameof(TemperatureText));
                OnPropertyChanged(nameof(TemperatureDescription));
                if (!_suppress)
                {
                    State.Preference.TemperatureK = clamped;
                    _changed(this);
                }
            }
        }
    }

    public int Contrast
    {
        get => _contrast;
        set
        {
            var clamped = Math.Clamp(value, 0, 100);
            if (SetProperty(ref _contrast, clamped))
            {
                OnPropertyChanged(nameof(ContrastText));
                if (!_suppress)
                {
                    State.Preference.Contrast = clamped;
                    State.Preference.ContrastTouched = true;
                    _changed(this);
                }
            }
        }
    }

    public int SoftwareDim
    {
        get => _softwareDim;
        set
        {
            var clamped = Math.Clamp(value, 0, 50);
            if (SetProperty(ref _softwareDim, clamped))
            {
                OnPropertyChanged(nameof(ExtraDimText));
                if (!_suppress)
                {
                    State.Preference.SoftwareDim = clamped;
                    _changed(this);
                }
            }
        }
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (SetProperty(ref _isEnabled, value))
            {
                State.Preference.Enabled = value;
                if (!_suppress)
                {
                    _changed(this);
                }
            }
        }
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public string StatusDetail
    {
        get => _statusDetail;
        private set => SetProperty(ref _statusDetail, value);
    }

    public bool HasError
    {
        get => _hasError;
        private set
        {
            if (SetProperty(ref _hasError, value)) OnPropertyChanged(nameof(StatusTone));
        }
    }

    public bool IsUnconfirmed
    {
        get => _isUnconfirmed;
        private set
        {
            if (SetProperty(ref _isUnconfirmed, value)) OnPropertyChanged(nameof(StatusTone));
        }
    }

    public void UpdateFromState()
    {
        _suppress = true;
        Brightness = State.Preference.Brightness;
        TemperatureK = State.Preference.TemperatureK;
        Contrast = State.Preference.Contrast;
        SoftwareDim = State.Preference.SoftwareDim;
        IsEnabled = State.Preference.Enabled;
        _suppress = false;
        ApplyStatus(State);
    }

    public void ApplyStatus(DisplayState state)
    {
        StatusText = state.Status;
        StatusDetail = state.LastError ?? string.Empty;
        HasError = state.LastError is not null && !state.IsUnconfirmed;
        IsUnconfirmed = state.IsUnconfirmed;
    }

    public void SetValues(int brightness, int temperatureK, int contrast, int softwareDim)
    {
        _suppress = true;
        Brightness = brightness;
        TemperatureK = temperatureK;
        Contrast = contrast;
        SoftwareDim = softwareDim;
        _suppress = false;
        State.Preference.Brightness = Brightness;
        State.Preference.TemperatureK = TemperatureK;
        State.Preference.Contrast = Contrast;
        State.Preference.SoftwareDim = SoftwareDim;
    }

    public void ResetToOriginal()
    {
        var originalBrightness = Info.HardwareBrightnessSupported && Info.HardwareBrightnessMax > 0
            ? (int)Math.Round(Info.OriginalBrightness * 100.0 / Info.HardwareBrightnessMax)
            : 100;
        var originalContrast = Info.HardwareContrastSupported && Info.HardwareContrastMax > 0
            ? (int)Math.Round(Info.OriginalContrast * 100.0 / Info.HardwareContrastMax)
            : 50;
        SetValues(originalBrightness, 6500, originalContrast, 0);
        State.Preference.ContrastTouched = Info.HardwareContrastSupported;
    }
}
