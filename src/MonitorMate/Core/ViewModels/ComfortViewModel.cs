using System.Diagnostics;
using System.Windows.Input;
using System.Windows.Threading;
using MonitorMate.Core.Models;
using MonitorMate.Core.Mvvm;
using MonitorMate.Core.Services;

namespace MonitorMate.Core.ViewModels;

public sealed class ComfortViewModel : ObservableObject
{
    private readonly AppServices _services;
    private bool _scheduleEnabled;
    private bool _useSolar;
    private string _dayStartText = "07:00";
    private string _eveningStartText = "19:00";
    private string _nightStartText = "22:30";
    private string _latitudeText = string.Empty;
    private string _longitudeText = string.Empty;
    private string _scheduleError = string.Empty;
    private string _scheduleStatus = string.Empty;
    private string _nextChange = string.Empty;
    private string _solarPreview = "Enter a latitude and longitude to estimate sunrise and sunset.";
    private string _scheduleWarning = string.Empty;
    private string _sunriseText = "07:00";
    private string _sunsetText = "19:00";
    private string? _message;
    private bool _isBusy;

    private readonly Func<bool> _requestAutomaticControl;

    public ComfortViewModel(AppServices services, Func<bool> requestAutomaticControl)
    {
        _services = services;
        _requestAutomaticControl = requestAutomaticControl;
        ApplyPresetCommand = new AsyncRelayCommand<string>(ApplyPresetAsync);
        ApplyScheduleCommand = new AsyncRelayCommand(ApplyScheduleAsync);
        UseFixedCommand = new RelayCommand(() => UseSolar = false);
        UseSolarCommand = new RelayCommand(() => UseSolar = true);
        OpenNightLightCommand = new RelayCommand(() => OpenSettings("ms-settings:nightlight"));
        OpenDisplaySettingsCommand = new RelayCommand(() => OpenSettings("ms-settings:display"));
        OpenColorManagementCommand = new RelayCommand(() => OpenSettings("ms-settings:display-advanced"));
        _services.Comfort.StateChanged += (_, _) => RefreshPresetBadges();
        _services.Schedule.SnapshotChanged += (_, snapshot) => ApplySnapshot(snapshot);
        LoadFromSettings();
        ApplySnapshot(_services.Schedule.GetSnapshot());
        _clock = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) };
        _clock.Tick += (_, _) => OnPropertyChanged(nameof(NowMinutes));
        _clock.Start();
    }

    private readonly DispatcherTimer _clock;

    public IReadOnlyList<PresetOptionViewModel> Presets { get; private set; } = Array.Empty<PresetOptionViewModel>();
    public ICommand ApplyPresetCommand { get; }
    public ICommand ApplyScheduleCommand { get; }
    public ICommand UseFixedCommand { get; }
    public ICommand UseSolarCommand { get; }
    public ICommand OpenNightLightCommand { get; }
    public ICommand OpenDisplaySettingsCommand { get; }
    public ICommand OpenColorManagementCommand { get; }

    public string CurrentPresetName => _services.Comfort.CurrentPresetName;
    public bool IsCustomMix => _services.Comfort.IsCustomMix;
    public bool IsScheduleEnabled => _scheduleEnabled;
    public bool UseSolar
    {
        get => _useSolar;
        set
        {
            if (!SetProperty(ref _useSolar, value)) return;
            _services.Settings.Settings.ScheduleMode = value ? ScheduleMode.Solar : ScheduleMode.Fixed;
            _services.Settings.SaveDebounced();
            OnPropertyChanged(nameof(IsFixed));
            OnPropertyChanged(nameof(ModeDescription));
            RefreshSchedulePreview();
            _services.Schedule.ResetAppliedKey();
            _ = _services.Schedule.ApplyCurrentAsync(force: true);
        }
    }

    public bool IsFixed => !UseSolar;
    public string ModeDescription => UseSolar
        ? "MonitorMate estimates sunrise and sunset from your coordinates, entirely offline."
        : "Use fixed local hours. No location is required.";
    public string DayStartText
    {
        get => _dayStartText;
        set
        {
            if (SetProperty(ref _dayStartText, value))
            {
                OnPropertyChanged(nameof(DayStartMinutes));
            }
        }
    }

    public string EveningStartText
    {
        get => _eveningStartText;
        set
        {
            if (SetProperty(ref _eveningStartText, value))
            {
                OnPropertyChanged(nameof(EveningStartMinutes));
            }
        }
    }

    public string NightStartText
    {
        get => _nightStartText;
        set
        {
            if (SetProperty(ref _nightStartText, value))
            {
                OnPropertyChanged(nameof(NightStartMinutes));
            }
        }
    }

    public double DayStartMinutes => ParseMinutes(SunriseText, 7 * 60);
    public double EveningStartMinutes => ParseMinutes(SunsetText, 19 * 60);
    public double NightStartMinutes => ParseMinutes(NightStartText, 22 * 60 + 30);
    public double NowMinutes => DateTime.Now.TimeOfDay.TotalMinutes;
    public string SunriseText { get => _sunriseText; private set => SetProperty(ref _sunriseText, value); }
    public string SunsetText { get => _sunsetText; private set => SetProperty(ref _sunsetText, value); }

    public string LatitudeText
    {
        get => _latitudeText;
        set => SetProperty(ref _latitudeText, value);
    }

    public string LongitudeText
    {
        get => _longitudeText;
        set => SetProperty(ref _longitudeText, value);
    }

    public string ScheduleError { get => _scheduleError; private set => SetProperty(ref _scheduleError, value); }
    public string ScheduleStatus { get => _scheduleStatus; private set => SetProperty(ref _scheduleStatus, value); }
    public string NextChange { get => _nextChange; private set => SetProperty(ref _nextChange, value); }
    public string SolarPreview { get => _solarPreview; private set => SetProperty(ref _solarPreview, value); }
    public string ScheduleWarning { get => _scheduleWarning; private set => SetProperty(ref _scheduleWarning, value); }
    public string? Message { get => _message; private set => SetProperty(ref _message, value); }
    public bool IsBusy { get => _isBusy; private set => SetProperty(ref _isBusy, value); }
    public bool HasSolarWarning => !string.IsNullOrWhiteSpace(ScheduleWarning);
    public bool HasError => !string.IsNullOrWhiteSpace(ScheduleError);

    public bool ScheduleEnabled
    {
        get => _scheduleEnabled;
        set
        {
            if (_scheduleEnabled == value) return;
            if (value && !_requestAutomaticControl())
            {
                OnPropertyChanged(nameof(ScheduleEnabled));
                Message = "Automatic changes remain off. Manual controls are available.";
                return;
            }

            SetProperty(ref _scheduleEnabled, value);
            var settings = _services.Settings.Settings;
            settings.AutomaticDisplayControlAllowed = value;
            settings.ScheduleEnabled = value;
            if (value) settings.SetupCompleted = true;
            else settings.RestoreOnLaunch = false;
            _services.Settings.SaveDebounced();
            OnPropertyChanged(nameof(IsScheduleEnabled));
            OnPropertyChanged(nameof(ScheduleDescription));
            _services.Schedule.ResetAppliedKey();
            if (value) _services.Schedule.Start();
            else _services.Schedule.Stop();
        }
    }

    public string ScheduleDescription => ScheduleEnabled
        ? "MonitorMate follows the rhythm below while it is running."
        : "Your current preset stays put until you change it.";

    private void LoadFromSettings()
    {
        var settings = _services.Settings.Settings;
        _scheduleEnabled = settings.AutomaticDisplayControlAllowed && settings.ScheduleEnabled;
        _useSolar = settings.ScheduleMode == ScheduleMode.Solar;
        _dayStartText = settings.DayStart;
        _eveningStartText = settings.EveningStart;
        _nightStartText = settings.NightStart;
        _latitudeText = settings.HasLocation ? settings.Latitude.ToString("0.####") : string.Empty;
        _longitudeText = settings.HasLocation ? settings.Longitude.ToString("0.####") : string.Empty;
        OnPropertyChanged(nameof(ScheduleEnabled));
        OnPropertyChanged(nameof(UseSolar));
        OnPropertyChanged(nameof(IsFixed));
        OnPropertyChanged(nameof(ModeDescription));
        OnPropertyChanged(nameof(ScheduleDescription));
        OnPropertyChanged(nameof(DayStartMinutes));
        OnPropertyChanged(nameof(EveningStartMinutes));
        OnPropertyChanged(nameof(NightStartMinutes));
        RefreshPresetBadges();
        RefreshSchedulePreview();
    }

    public void RefreshPresetBadges()
    {
        if (Presets.Count == 0)
        {
            Presets = PresetCatalog.All.Select(p => new PresetOptionViewModel(p, ApplyPresetCommand)).ToList();
            OnPropertyChanged(nameof(Presets));
        }

        foreach (var preset in Presets)
        {
            preset.IsCurrent = string.Equals(preset.Id, _services.Comfort.CurrentPresetId, StringComparison.OrdinalIgnoreCase)
                && !_services.Comfort.IsCustomMix;
        }

        OnPropertyChanged(nameof(CurrentPresetName));
        OnPropertyChanged(nameof(IsCustomMix));
    }

    private async Task ApplyPresetAsync(string? presetId)
    {
        if (string.IsNullOrWhiteSpace(presetId)) return;
        IsBusy = true;
        try
        {
            await _services.Comfort.ApplyPresetAsync(presetId);
            RefreshPresetBadges();
            Message = $"{PresetCatalog.Get(presetId).Name} is now active on every display.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ApplyScheduleAsync()
    {
        ScheduleError = string.Empty;
        if (!TryParseTime(DayStartText, out var day) || !TryParseTime(EveningStartText, out var evening) || !TryParseTime(NightStartText, out var night))
        {
            ScheduleError = "Use a 24-hour time such as 07:00 for each field.";
            return;
        }

        if (!UseSolar && (day >= evening || evening >= night))
        {
            ScheduleError = "Daylight, evening and night must use distinct times in that order.";
            return;
        }

        if (UseSolar)
        {
            if (!TryParseCoordinate(LatitudeText, -90, 90, out var latitude) ||
                !TryParseCoordinate(LongitudeText, -180, 180, out var longitude) ||
                (Math.Abs(latitude) < 0.0001 && Math.Abs(longitude) < 0.0001))
            {
                ScheduleError = "Enter a valid latitude and longitude, or switch to fixed hours.";
                return;
            }

            _services.Settings.Settings.Latitude = latitude;
            _services.Settings.Settings.Longitude = longitude;
            _services.Settings.Settings.HasLocation = true;
        }

        var settings = _services.Settings.Settings;
        settings.ScheduleMode = UseSolar ? ScheduleMode.Solar : ScheduleMode.Fixed;
        settings.DayStart = day.ToString("HH:mm");
        settings.EveningStart = evening.ToString("HH:mm");
        settings.NightStart = night.ToString("HH:mm");
        settings.ScheduleEnabled = ScheduleEnabled;
        _services.Settings.SaveDebounced();
        RefreshSchedulePreview();
        _services.Schedule.ResetAppliedKey();
        await _services.Schedule.ApplyCurrentAsync(force: true);
        Message = ScheduleEnabled ? "Schedule saved. The next boundary will apply automatically." : "Schedule saved. Automatic changes remain off.";
    }

    private void ApplySnapshot(ScheduleSnapshot snapshot)
    {
        SunriseText = snapshot.SunriseText;
        SunsetText = snapshot.SunsetText;
        ScheduleStatus = snapshot.Enabled ? $"Now: {snapshot.PeriodName}" : "Schedule off";
        NextChange = snapshot.Enabled ? $"Next change {snapshot.NextChangeText}" : "No automatic changes";
        ScheduleWarning = snapshot.Warning ?? string.Empty;
        OnPropertyChanged(nameof(HasSolarWarning));
        OnPropertyChanged(nameof(CurrentPresetName));
        OnPropertyChanged(nameof(DayStartMinutes));
        OnPropertyChanged(nameof(EveningStartMinutes));
    }

    private void RefreshSchedulePreview()
    {
        if (!UseSolar)
        {
            SolarPreview = "Fixed hours are in use. Your location is not needed.";
            return;
        }

        if (!TryParseCoordinate(LatitudeText, -90, 90, out var latitude) ||
            !TryParseCoordinate(LongitudeText, -180, 180, out var longitude))
        {
            SolarPreview = "Enter a latitude and longitude to estimate sunrise and sunset.";
            return;
        }

        var solar = SolarCalculator.Calculate(DateOnly.FromDateTime(DateTime.Now), latitude, longitude);
        SolarPreview = solar is null
            ? "Sunrise and sunset cannot be estimated at this location today. Fixed hours will be used."
            : $"Estimated today: sunrise {solar.Sunrise:HH:mm}, sunset {solar.Sunset:HH:mm}.";
    }

    private static bool TryParseTime(string text, out TimeOnly value)
        => TimeOnly.TryParse(text, out value);

    private static double ParseMinutes(string? text, double fallback)
        => TimeOnly.TryParse(text, out var value) ? value.ToTimeSpan().TotalMinutes : fallback;

    private static bool TryParseCoordinate(string text, double min, double max, out double value)
        => double.TryParse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.CurrentCulture, out value)
           && value >= min && value <= max;

    private static void OpenSettings(string uri)
    {
        try
        {
            Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
        }
        catch
        {
            // Windows Settings is optional.
        }
    }

    public void RefreshPage()
    {
        _scheduleEnabled = _services.Settings.Settings.AutomaticDisplayControlAllowed && _services.Settings.Settings.ScheduleEnabled;
        OnPropertyChanged(nameof(ScheduleEnabled));
        OnPropertyChanged(nameof(IsScheduleEnabled));
        OnPropertyChanged(nameof(ScheduleDescription));
        RefreshPresetBadges();
        ApplySnapshot(_services.Schedule.GetSnapshot());
        RefreshSchedulePreview();
    }
}
