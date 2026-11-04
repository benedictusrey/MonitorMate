using System.Collections.Concurrent;
using MonitorMate.Core.Models;
using MonitorMate.Core.Mvvm;

namespace MonitorMate.Core.Services;

public sealed class ComfortService : ObservableObject, IDisposable
{
    private readonly DisplayService _displays;
    private readonly GammaService _gamma;
    private readonly SettingsService _settings;
    private readonly LogService _log;
    private readonly SemaphoreSlim _applyGate = new(1, 1);
    private readonly SemaphoreSlim _refreshGate = new(1, 1);
    private readonly ConcurrentDictionary<string, int> _versions = new();
    private List<DisplayState> _states = new();
    private bool _previewOriginal;
    private bool _disposed;

    public ComfortService(DisplayService displays, GammaService gamma, SettingsService settings, LogService log)
    {
        _displays = displays;
        _gamma = gamma;
        _settings = settings;
        _log = log;
    }

    public IReadOnlyList<DisplayState> States => _states;
    public bool IsPaused => _settings.Settings.IsPaused;
    public bool IsPreviewingOriginal => _previewOriginal;
    public bool IsCustomMix { get; private set; }
    public DateTime ManualHoldUntil { get; private set; } = DateTime.MinValue;
    public string CurrentPresetId => _settings.Settings.LastPresetId;
    public ComfortPreset CurrentPreset => PresetCatalog.Get(CurrentPresetId);
    public string CurrentPresetName => IsCustomMix ? "Custom mix" : CurrentPreset.Name;

    public event EventHandler? StatesRebuilt;
    public event EventHandler<DisplayState>? StateUpdated;
    public event EventHandler? StateChanged;

    public async Task InitializeAsync()
    {
        await RebuildStatesAsync(applyAfter: false);
        if (IsPaused)
        {
            if (_settings.Settings.AutomaticDisplayControlEnabled) _gamma.RestoreAll();
            foreach (var state in _states)
            {
                state.Status = "Paused";
            }
        }
    }

    public async Task RefreshAsync(bool applyAfter = true)
    {
        await _refreshGate.WaitAsync();
        try
        {
            var automatic = applyAfter && _settings.Settings.AutomaticDisplayControlEnabled;
            if (automatic) _gamma.RestoreAll();
            await RebuildStatesAsync(applyAfter: false);
            if (automatic && !IsPaused)
            {
                await ApplyAllAsync(automatic: true);
            }
        }
        catch (Exception ex)
        {
            _log.Error("Display refresh failed.", ex);
        }
        finally
        {
            _refreshGate.Release();
        }
    }

    private async Task RebuildStatesAsync(bool applyAfter)
    {
        var infos = await Task.Run(_displays.Scan);
        var states = new List<DisplayState>(infos.Count);
        foreach (var info in infos)
        {
            var preference = _settings.Settings.GetDisplayPreference(info.Id);
            if (!preference.Initialized)
            {
                preference.Brightness = info.HardwareBrightnessSupported ? OriginalBrightnessPercent(info) : 100;
                preference.Contrast = info.HardwareContrastSupported ? OriginalContrastPercent(info) : 50;
                preference.TemperatureK = 6500;
                preference.SoftwareDim = 0;
                preference.Initialized = true;
                _settings.SaveDebounced();
            }

            preference.Brightness = Math.Clamp(preference.Brightness, 10, 100);
            preference.TemperatureK = Math.Clamp(preference.TemperatureK, 2700, 6500);
            preference.Contrast = Math.Clamp(preference.Contrast, 0, 100);
            preference.SoftwareDim = Math.Clamp(preference.SoftwareDim, 0, 50);
            states.Add(new DisplayState
            {
                Info = info,
                Preference = preference,
                AppliedBrightness = info.HardwareBrightnessSupported
                    ? OriginalBrightnessPercent(info)
                    : preference.Brightness,
                AppliedTemperatureK = preference.TemperatureK,
                AppliedSoftwareDim = preference.SoftwareDim,
                Status = info.ControlPath == DisplayControlPath.Unavailable
                    ? "No controllable display path"
                    : "Ready"
            });
        }

        _states = states;
        StatesRebuilt?.Invoke(this, EventArgs.Empty);
        OnPropertyChanged(nameof(States));
        OnPropertyChanged(nameof(CurrentPresetName));
        if (applyAfter && !IsPaused)
        {
            await ApplyAllAsync();
        }
    }

    public async Task ApplyPresetAsync(string presetId, bool fromSchedule = false)
    {
        if (!_settings.Settings.SetupCompleted) return;
        if (!PresetCatalog.Exists(presetId))
        {
            return;
        }

        if (fromSchedule && (IsPaused || !_settings.Settings.AutomaticDisplayControlAllowed || !_settings.Settings.ScheduleEnabled))
        {
            return;
        }

        var preset = PresetCatalog.Get(presetId);
        if (IsPaused && !fromSchedule)
        {
            SetPaused(false);
        }

        IsCustomMix = false;
        _settings.Settings.LastPresetId = preset.Id;
        if (!fromSchedule)
        {
            ManualHoldUntil = DateTime.Now.AddMinutes(15);
        }

        foreach (var state in _states.Where(s => s.Preference.Enabled))
        {
            var brightness = preset.Brightness;
            if (state.Info.HardwareBrightnessSupported)
            {
                // Never raise a monitor above the brightness it had before
                // MonitorMate touched it. A comfort preset may dim, not surprise.
                brightness = Math.Min(brightness, OriginalBrightnessPercent(state.Info));
            }

            state.Preference.Brightness = Math.Clamp(brightness, 10, 100);
            state.Preference.TemperatureK = preset.TemperatureK;
            state.Preference.SoftwareDim = preset.SoftwareDim;
            state.Preference.LastPresetId = preset.Id;
        }

        _settings.SaveDebounced();
        OnPropertyChanged(nameof(CurrentPresetId));
        OnPropertyChanged(nameof(CurrentPreset));
        OnPropertyChanged(nameof(CurrentPresetName));
        StateChanged?.Invoke(this, EventArgs.Empty);
        await ApplyAllAsync(automatic: fromSchedule);
    }

    public async Task ApplyAllAsync(bool automatic = false)
    {
        await _applyGate.WaitAsync();
        try
        {
            if (_disposed || (automatic && !_settings.Settings.AutomaticDisplayControlEnabled))
            {
                return;
            }

            if (IsPaused)
            {
                _gamma.RestoreAll();
                foreach (var state in _states)
                {
                    state.Status = "Paused";
                    StateUpdated?.Invoke(this, state);
                }

                return;
            }

            if (_previewOriginal)
            {
                _gamma.RestoreAll();
                foreach (var state in _states)
                {
                    state.Status = "Previewing original";
                    StateUpdated?.Invoke(this, state);
                }

                return;
            }

            foreach (var state in _states)
            {
                if (automatic && !_settings.Settings.AutomaticDisplayControlEnabled) break;
                await ApplyStateCoreAsync(state);
            }
        }
        finally
        {
            _applyGate.Release();
        }
    }

    public async Task ApplyStateAsync(DisplayState state)
    {
        if (_disposed) return;
        var version = _versions.AddOrUpdate(state.Info.Id, 1, (_, current) => current + 1);
        await _applyGate.WaitAsync();
        try
        {
            if (_versions.TryGetValue(state.Info.Id, out var latest) && latest != version)
            {
                return;
            }

            if (IsPaused)
            {
                _gamma.Restore(state.Info);
                state.Status = "Paused";
                StateUpdated?.Invoke(this, state);
                return;
            }

            if (_previewOriginal)
            {
                _gamma.Restore(state.Info);
                state.Status = "Previewing original";
                StateUpdated?.Invoke(this, state);
                return;
            }

            await ApplyStateCoreAsync(state);
        }
        finally
        {
            _applyGate.Release();
        }
    }

    private async Task ApplyStateCoreAsync(DisplayState state)
    {
        if (!_settings.Settings.SetupCompleted)
        {
            state.Status = "Finish setup before applying display changes";
            StateUpdated?.Invoke(this, state);
            return;
        }
        var preference = state.Preference;
        if (!preference.Enabled)
        {
            state.Status = "Display control disabled";
            StateUpdated?.Invoke(this, state);
            return;
        }
        state.Status = "Applying…";
        StateUpdated?.Invoke(this, state);

        var gammaResult = await Task.Run(() =>
            _gamma.Apply(state.Info, preference.Brightness, preference.TemperatureK, preference.SoftwareDim));

        var hardwareMessage = string.Empty;
        var hardwareError = false;

        if (state.Info.HardwareBrightnessSupported &&
            state.AppliedBrightness != preference.Brightness)
        {
            var result = await _displays.SetBrightnessAsync(state.Info, preference.Brightness);
            if (result.Supported)
            {
                if (result.Confirmed) state.AppliedBrightness = result.ActualPercent;
                if (!result.Confirmed)
                {
                    hardwareMessage = result.Error ?? "Brightness sent, not confirmed.";
                }
            }
            else
            {
                hardwareError = true;
                hardwareMessage = result.Error ?? "Hardware brightness failed.";
            }
        }

        if (state.Info.HardwareContrastSupported &&
            state.Preference.ContrastTouched &&
            state.AppliedContrast != preference.Contrast)
        {
            var result = await _displays.SetContrastAsync(state.Info, preference.Contrast);
            if (result.Supported)
            {
                if (result.Confirmed) state.AppliedContrast = result.ActualPercent;
                if (!result.Confirmed)
                {
                    hardwareMessage = string.IsNullOrWhiteSpace(hardwareMessage)
                        ? result.Error ?? "Contrast sent, not confirmed."
                        : hardwareMessage;
                }
            }
        }

        state.AppliedTemperatureK = preference.TemperatureK;
        state.AppliedSoftwareDim = preference.SoftwareDim;

        if (!state.Info.GammaSupported && !state.Info.HardwareBrightnessSupported)
        {
            state.Status = "This display has no usable control path";
            state.LastError = state.Info.GammaWarning ?? "Windows exposes neither DDC/CI nor a gamma ramp.";
            state.IsUnconfirmed = false;
        }
        else if (!gammaResult.Success && !state.Info.HardwareBrightnessSupported)
        {
            state.Status = gammaResult.Message ?? "Colour adjustment failed";
            state.LastError = gammaResult.Message;
            state.IsUnconfirmed = false;
        }
        else if (!gammaResult.Success && state.Info.HardwareBrightnessSupported)
        {
            state.Status = "Brightness works / colour control unavailable";
            state.LastError = gammaResult.Message;
            state.IsUnconfirmed = false;
        }
        else if (gammaResult.Unconfirmed || !string.IsNullOrWhiteSpace(hardwareMessage) || hardwareError)
        {
            state.Status = hardwareError
                ? "Adjustment not confirmed"
                : "Request sent / driver did not confirm";
            state.LastError = hardwareMessage.Length > 0 ? hardwareMessage : gammaResult.Message;
            state.IsUnconfirmed = true;
        }
        else
        {
            state.Status = state.Info.ControlPath switch
            {
                DisplayControlPath.HardwareDdc => "Applied / hardware + colour",
                DisplayControlPath.SoftwareGamma => "Applied / software control",
                _ => "Applied"
            };
            state.LastError = null;
            state.IsUnconfirmed = false;
        }

        StateUpdated?.Invoke(this, state);
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SetPaused(bool paused)
    {
        _settings.Settings.IsPaused = paused;
        _settings.SaveDebounced();
        OnPropertyChanged(nameof(IsPaused));
        StateChanged?.Invoke(this, EventArgs.Empty);
        if (paused)
        {
            _gamma.RestoreAll();
            foreach (var state in _states)
            {
                state.Status = "Paused";
                StateUpdated?.Invoke(this, state);
            }
        }
        else
        {
            _ = ApplyAllAsync();
        }
    }

    public void PreviewOriginal(bool preview)
    {
        _previewOriginal = preview;
        if (preview)
        {
            _gamma.RestoreAll();
            foreach (var state in _states)
            {
                state.Status = "Previewing original";
                StateUpdated?.Invoke(this, state);
            }
        }
        else
        {
            _ = ApplyAllAsync();
        }
    }

    public void NoteManualAdjustment(DisplayState state)
    {
        IsCustomMix = true;
        ManualHoldUntil = DateTime.Now.AddMinutes(15);
        state.Preference.LastPresetId = "custom";
        _settings.SaveDebounced();
        OnPropertyChanged(nameof(IsCustomMix));
        OnPropertyChanged(nameof(CurrentPresetName));
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    private static int OriginalBrightnessPercent(DisplayInfo info)
    {
        if (!info.HardwareBrightnessSupported || info.HardwareBrightnessMax == 0)
        {
            return 100;
        }

        return Math.Clamp((int)Math.Round(info.OriginalBrightness * 100.0 / info.HardwareBrightnessMax), 10, 100);
    }

    private static int OriginalContrastPercent(DisplayInfo info)
    {
        if (!info.HardwareContrastSupported || info.HardwareContrastMax == 0)
        {
            return 50;
        }

        return Math.Clamp((int)Math.Round(info.OriginalContrast * 100.0 / info.HardwareContrastMax), 0, 100);
    }

    public bool ShouldScheduleYield() => DateTime.Now < ManualHoldUntil;

    public void RestoreAll()
    {
        _gamma.RestoreAll();
        foreach (var state in _states)
        {
            state.Status = "Original colour";
            StateUpdated?.Invoke(this, state);
        }
    }

    public void ResetManualHold()
    {
        ManualHoldUntil = DateTime.MinValue;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _gamma.RestoreAll();
        _applyGate.Dispose();
        _refreshGate.Dispose();
    }
}
