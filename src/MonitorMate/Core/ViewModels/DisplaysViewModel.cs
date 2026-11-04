using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using MonitorMate.Core.Models;
using MonitorMate.Core.Mvvm;
using MonitorMate.Core.Services;

namespace MonitorMate.Core.ViewModels;

public sealed class DisplaysViewModel : ObservableObject
{
    private readonly AppServices _services;
    private DisplayCardViewModel? _selectedDisplay;
    private bool _isBusy;
    private bool _previewOriginal;
    private string? _message;

    public DisplaysViewModel(AppServices services)
    {
        _services = services;
        ApplyPresetCommand = new AsyncRelayCommand<string>(ApplyPresetAsync);
        ApplyToAllCommand = new AsyncRelayCommand(ApplyToAllAsync);
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        IdentifyCommand = new RelayCommand(() => _services.Overlays.ShowIdentify());
        ResetSelectedCommand = new AsyncRelayCommand(ResetSelectedAsync);
        PreviewOriginalCommand = new RelayCommand(() => PreviewOriginal = !PreviewOriginal);
        OpenWindowsDisplaySettingsCommand = new RelayCommand(OpenWindowsDisplaySettings);
        _services.Comfort.StatesRebuilt += (_, _) => RebuildCards();
        _services.Comfort.StateUpdated += (_, state) => UpdateCardStatus(state);
        RebuildCards();
    }

    public ObservableCollection<DisplayCardViewModel> Displays { get; } = new();
    public IReadOnlyList<PresetOptionViewModel> Presets { get; private set; } = Array.Empty<PresetOptionViewModel>();
    public ICommand ApplyPresetCommand { get; }
    public ICommand ApplyToAllCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand IdentifyCommand { get; }
    public ICommand ResetSelectedCommand { get; }
    public ICommand PreviewOriginalCommand { get; }
    public ICommand OpenWindowsDisplaySettingsCommand { get; }

    public DisplayCardViewModel? SelectedDisplay
    {
        get => _selectedDisplay;
        set
        {
            if (ReferenceEquals(_selectedDisplay, value)) return;
            if (_selectedDisplay is not null) _selectedDisplay.IsSelected = false;
            _selectedDisplay = value;
            if (_selectedDisplay is not null) _selectedDisplay.IsSelected = true;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasSelection));
            OnPropertyChanged(nameof(SelectedTitle));
        }
    }

    public bool HasSelection => SelectedDisplay is not null;
    public bool HasDisplays => Displays.Count > 0;
    public bool IsEmpty => Displays.Count == 0;
    public string DisplayCountText => Displays.Count == 1 ? "1 display connected" : $"{Displays.Count} displays connected";
    public string SelectedTitle => SelectedDisplay?.Name ?? "Choose a display";
    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    public string? Message
    {
        get => _message;
        private set => SetProperty(ref _message, value);
    }

    public bool PreviewOriginal
    {
        get => _previewOriginal;
        set
        {
            if (!SetProperty(ref _previewOriginal, value)) return;
            _services.Comfort.PreviewOriginal(value);
            OnPropertyChanged(nameof(PreviewLabel));
        }
    }

    public string PreviewLabel => PreviewOriginal ? "Return to comfort" : "Compare with original";

    public void RebuildCards()
    {
        var selectedId = SelectedDisplay?.Id;
        Displays.Clear();
        foreach (var state in _services.Comfort.States)
        {
            var card = new DisplayCardViewModel(state, OnCardChanged);
            Displays.Add(card);
        }

        Presets = PresetCatalog.All.Select(p => new PresetOptionViewModel(p, ApplyPresetCommand)).ToList();
        foreach (var preset in Presets)
        {
            preset.IsCurrent = string.Equals(preset.Id, _services.Comfort.CurrentPresetId, StringComparison.OrdinalIgnoreCase)
                && !_services.Comfort.IsCustomMix;
        }

        SelectedDisplay = Displays.FirstOrDefault(d => d.Id == selectedId) ?? Displays.FirstOrDefault(d => d.IsPrimary) ?? Displays.FirstOrDefault();
        OnPropertyChanged(nameof(Presets));
        OnPropertyChanged(nameof(HasDisplays));
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(DisplayCountText));
        OnPropertyChanged(nameof(SelectedTitle));
        OnPropertyChanged(nameof(HasSelection));
    }

    public void RefreshValues()
    {
        foreach (var card in Displays)
        {
            card.UpdateFromState();
        }

        foreach (var preset in Presets)
        {
            preset.IsCurrent = string.Equals(preset.Id, _services.Comfort.CurrentPresetId, StringComparison.OrdinalIgnoreCase)
                && !_services.Comfort.IsCustomMix;
        }
    }

    private void UpdateCardStatus(DisplayState state)
    {
        var card = Displays.FirstOrDefault(d => d.Id == state.Info.Id);
        card?.ApplyStatus(state);
    }

    private async void OnCardChanged(DisplayCardViewModel card)
    {
        try
        {
            if (_services.Comfort.IsPaused)
            {
                _services.Comfort.SetPaused(false);
            }

            _services.Comfort.NoteManualAdjustment(card.State);
            _services.Settings.SaveDebounced();
            RefreshPresetBadges();
            await _services.Comfort.ApplyStateAsync(card.State);
        }
        catch (Exception ex)
        {
            _services.Log.Error("A display adjustment failed.", ex);
            Message = "That adjustment could not be completed. Check the display connection.";
        }
    }

    private void RefreshPresetBadges()
    {
        foreach (var preset in Presets)
        {
            preset.IsCurrent = string.Equals(preset.Id, _services.Comfort.CurrentPresetId, StringComparison.OrdinalIgnoreCase)
                && !_services.Comfort.IsCustomMix;
        }
    }

    private async Task ApplyPresetAsync(string? presetId)
    {
        if (string.IsNullOrWhiteSpace(presetId)) return;
        IsBusy = true;
        try
        {
            await _services.Comfort.ApplyPresetAsync(presetId);
            RefreshValues();
            Message = $"{PresetCatalog.Get(presetId).Name} applied to all displays.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ApplyToAllAsync()
    {
        if (SelectedDisplay is null) return;
        IsBusy = true;
        try
        {
            var source = SelectedDisplay;
            foreach (var card in Displays)
            {
                card.SetValues(source.Brightness, source.TemperatureK, source.Contrast, source.SoftwareDim);
                _services.Comfort.NoteManualAdjustment(card.State);
            }

            await _services.Comfort.ApplyAllAsync();
            _services.Settings.SaveDebounced();
            RefreshValues();
            Message = "These settings now match on every display.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RefreshAsync()
    {
        IsBusy = true;
        try
        {
            await _services.Comfort.RefreshAsync();
            RebuildCards();
            Message = "Display list refreshed.";
        }
        catch (Exception)
        {
            Message = "Windows did not return a display list. Try again after checking the cable.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ResetSelectedAsync()
    {
        if (SelectedDisplay is null) return;
        if (!_services.Settings.Settings.SetupCompleted || !SelectedDisplay.IsEnabled)
        {
            Message = "Finish setup and enable this display before resetting it.";
            return;
        }
        SelectedDisplay.ResetToOriginal();
        _services.Comfort.NoteManualAdjustment(SelectedDisplay.State);
        await _services.Comfort.ApplyStateAsync(SelectedDisplay.State);
        await _services.Displays.RestoreOriginalBrightnessAsync(SelectedDisplay.Info);
        _services.Settings.SaveDebounced();
        RefreshValues();
        Message = "Original colour and hardware brightness restored for this display.";
    }

    public void AdjustBrightness(int delta)
    {
        var card = SelectedDisplay ?? Displays.FirstOrDefault();
        if (card is null) return;
        card.Brightness += delta;
    }

    public void AdjustTemperature(int delta)
    {
        var card = SelectedDisplay ?? Displays.FirstOrDefault();
        if (card is null) return;
        card.TemperatureK += delta;
    }

    private static void OpenWindowsDisplaySettings()
    {
        try
        {
            Process.Start(new ProcessStartInfo("ms-settings:display") { UseShellExecute = true });
        }
        catch
        {
            // Windows settings is optional.
        }
    }

    public void RefreshPage()
    {
        RefreshValues();
        OnPropertyChanged(nameof(DisplayCountText));
    }
}
