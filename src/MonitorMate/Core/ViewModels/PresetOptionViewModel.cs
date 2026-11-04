using System.Windows.Input;
using MonitorMate.Core.Models;
using MonitorMate.Core.Mvvm;

namespace MonitorMate.Core.ViewModels;

public sealed class PresetOptionViewModel : ObservableObject
{
    private bool _isCurrent;

    public PresetOptionViewModel(ComfortPreset preset, ICommand applyCommand)
    {
        Preset = preset;
        ApplyCommand = applyCommand;
    }

    public ComfortPreset Preset { get; }
    public string Id => Preset.Id;
    public string Name => Preset.Name;
    public string Description => Preset.Description;
    public string TemperatureText => $"{Preset.TemperatureK} K";
    public string BrightnessText => $"{Preset.Brightness}%";
    public ICommand ApplyCommand { get; }

    public bool IsCurrent
    {
        get => _isCurrent;
        set => SetProperty(ref _isCurrent, value);
    }
}
