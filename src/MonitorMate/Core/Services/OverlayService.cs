using System.Windows;
using MonitorMate.Core.Models;
using MonitorMate.Core.Mvvm;
using MonitorMate.Core.ViewModels.Overlays;
using MonitorMate.Views.Overlays;

namespace MonitorMate.Core.Services;

public sealed record ToastRequest(
    string Title,
    string Message,
    string? PrimaryLabel = null,
    Action? PrimaryAction = null,
    string? SecondaryLabel = null,
    Action? SecondaryAction = null);

public sealed class OverlayService : IDisposable
{
    private readonly ComfortService _comfort;
    private readonly LogService _log;
    private readonly List<BreakOverlayWindow> _breakWindows = new();
    private readonly List<FocusOverlayWindow> _focusWindows = new();
    private readonly List<IdentifyOverlayWindow> _identifyWindows = new();
    private ToastWindow? _toast;
    private GentleBreakWindow? _gentleBreak;
    private FocusOverlayViewModel? _focusViewModel;
    private bool _disposed;

    public OverlayService(ComfortService comfort, LogService log)
    {
        _comfort = comfort;
        _log = log;
    }

    public bool IsBreakOpen => _breakWindows.Count > 0 || _gentleBreak is not null;
    public bool IsFocusOpen => _focusWindows.Count > 0;

    public void ShowGentleBreak(BreakOverlayViewModel viewModel, Action skip)
    {
        CloseBreak();
        try
        {
            _gentleBreak = new GentleBreakWindow(viewModel, skip);
            _gentleBreak.Show();
        }
        catch (Exception ex)
        {
            _log.Error("Could not open the gentle break reminder.", ex);
        }
    }

    public void ShowBreak(BreakOverlayViewModel viewModel, Action skip)
    {
        CloseBreak();
        var displays = CurrentDisplays();
        if (displays.Count == 0)
        {
            return;
        }

        var primary = displays.FirstOrDefault(d => d.IsPrimary) ?? displays[0];
        foreach (var display in displays)
        {
            try
            {
                var window = new BreakOverlayWindow(viewModel, display, ReferenceEquals(display, primary), skip);
                _breakWindows.Add(window);
                window.Show();
                if (ReferenceEquals(display, primary))
                {
                    window.Activate();
                }
            }
            catch (Exception ex)
            {
                _log.Error("Could not open the break overlay.", ex);
            }
        }
    }

    public void CloseBreak()
    {
        foreach (var window in _breakWindows.ToList())
        {
            window.AllowClose();
            window.Close();
        }

        _breakWindows.Clear();
        _gentleBreak?.Close();
        _gentleBreak = null;
    }

    public void ShowToast(ToastRequest request)
    {
        CloseToast();
        var viewModel = new ToastViewModel
        {
            Title = request.Title,
            Message = request.Message,
            PrimaryLabel = request.PrimaryLabel ?? string.Empty,
            SecondaryLabel = request.SecondaryLabel ?? string.Empty,
            HasPrimary = request.PrimaryAction is not null,
            HasSecondary = request.SecondaryAction is not null,
            PrimaryCommand = request.PrimaryAction is null ? null : new RelayCommand(request.PrimaryAction),
            SecondaryCommand = request.SecondaryAction is null ? null : new RelayCommand(request.SecondaryAction)
        };

        try
        {
            _toast = new ToastWindow(viewModel);
            _toast.Show();
        }
        catch (Exception ex)
        {
            _log.Error("Could not show a notification.", ex);
        }
    }

    public void CloseToast()
    {
        if (_toast is null) return;
        _toast.AllowClose();
        _toast.Close();
        _toast = null;
    }

    public void ShowIdentify()
    {
        CloseIdentify();
        var displays = CurrentDisplays();
        foreach (var display in displays)
        {
            try
            {
                var window = new IdentifyOverlayWindow(display);
                _identifyWindows.Add(window);
                window.Show();
            }
            catch (Exception ex)
            {
                _log.Error("Could not show the display identifier.", ex);
            }
        }
    }

    public void CloseIdentify()
    {
        foreach (var window in _identifyWindows.ToList())
        {
            window.Close();
        }

        _identifyWindows.Clear();
    }

    public void StartFocus(FocusSettings settings)
    {
        StopFocus();
        var displays = CurrentDisplays();
        if (displays.Count == 0) return;

        _focusViewModel = new FocusOverlayViewModel
        {
            Mode = settings.Mode,
            DimPercent = settings.DimPercent,
            BandHeight = settings.BandHeight,
            FollowPointer = settings.FollowPointer
        };

        foreach (var display in displays)
        {
            try
            {
                var window = new FocusOverlayWindow(_focusViewModel, display);
                _focusWindows.Add(window);
                window.Show();
                window.BringToFront();
            }
            catch (Exception ex)
            {
                _log.Error("Could not open the focus overlay.", ex);
            }
        }
    }

    public void UpdateFocus(FocusSettings settings)
    {
        if (_focusViewModel is null) return;
        _focusViewModel.Mode = settings.Mode;
        _focusViewModel.DimPercent = settings.DimPercent;
        _focusViewModel.BandHeight = settings.BandHeight;
        _focusViewModel.FollowPointer = settings.FollowPointer;
    }

    public void StopFocus()
    {
        foreach (var window in _focusWindows.ToList())
        {
            window.Close();
        }

        _focusWindows.Clear();
        _focusViewModel = null;
    }

    private IReadOnlyList<DisplayInfo> CurrentDisplays()
    {
        var displays = _comfort.States.Select(s => s.Info).ToList();
        if (displays.Count > 0)
        {
            return displays;
        }

        var width = (int)Math.Max(800, SystemParameters.PrimaryScreenWidth);
        var height = (int)Math.Max(600, SystemParameters.PrimaryScreenHeight);
        return
        [
            new DisplayInfo
            {
                Id = "fallback",
                DeviceName = string.Empty,
                Name = "Display",
                AdapterName = "Windows",
                Bounds = new PixelRect { X = 0, Y = 0, Width = width, Height = height },
                WorkArea = new PixelRect { X = 0, Y = 0, Width = width, Height = height },
                IsPrimary = true,
                Dpi = 96,
                MonitorNumber = 1
            }
        ];
    }

    public void CloseAll()
    {
        CloseBreak();
        CloseToast();
        CloseIdentify();
        StopFocus();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        CloseAll();
    }
}
