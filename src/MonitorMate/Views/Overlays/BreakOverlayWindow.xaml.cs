using System.Windows;
using System.Windows.Threading;
using MonitorMate.Core.Models;
using MonitorMate.Core.Services;
using MonitorMate.Core.ViewModels.Overlays;

namespace MonitorMate.Views.Overlays;

public partial class BreakOverlayWindow : Window
{
    private readonly BreakOverlayViewModel _viewModel;
    private readonly Action _skip;
    private readonly bool _isPrimary;
    private DispatcherTimer? _holdTimer;
    private DateTime _holdStarted;
    private bool _allowClose;

    public BreakOverlayWindow(BreakOverlayViewModel viewModel, DisplayInfo display, bool isPrimary, Action skip)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _skip = skip;
        _isPrimary = isPrimary;
        DataContext = viewModel;
        OverlayWindowHelper.ConfigureFullscreen(this, display.Bounds, clickThrough: false);
        ShowActivated = isPrimary;

        ModeLabel.Text = viewModel.LengthLabel;
        ActionPanel.Visibility = isPrimary && viewModel.CanSkip ? Visibility.Visible : Visibility.Collapsed;
        StrictHint.Visibility = isPrimary && !viewModel.CanSkip ? Visibility.Visible : Visibility.Collapsed;
        if (viewModel.CanSkip && viewModel.HoldToSkip)
        {
            SkipButton.Content = "Hold to skip";
        }

        if (!isPrimary)
        {
            ActionPanel.Visibility = Visibility.Collapsed;
            StrictHint.Visibility = Visibility.Collapsed;
        }
    }

    public void AllowClose()
    {
        _allowClose = true;
    }

    private void SkipButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.HoldToSkip)
        {
            return;
        }

        RequestSkip();
    }

    private void SkipButton_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (!_viewModel.HoldToSkip || !_viewModel.CanSkip)
        {
            return;
        }

        _holdStarted = DateTime.UtcNow;
        _holdTimer?.Stop();
        _holdTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        _holdTimer.Tick += (_, _) =>
        {
            var elapsed = DateTime.UtcNow - _holdStarted;
            var remaining = Math.Max(0, 3 - elapsed.TotalSeconds);
            SkipButton.Content = remaining <= 0.1 ? "Letting go…" : $"Keep holding {Math.Ceiling(remaining):0}s";
            if (elapsed.TotalSeconds >= 3)
            {
                StopHold();
                RequestSkip();
            }
        };
        _holdTimer.Start();
    }

    private void SkipButton_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        StopHold();
    }

    private void SkipButton_PreviewMouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        StopHold();
    }

    private void StopHold()
    {
        _holdTimer?.Stop();
        _holdTimer = null;
        SkipButton.Content = _viewModel.HoldToSkip ? "Hold to skip" : "Skip break";
    }

    private void RequestSkip()
    {
        _skip();
    }

    private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Escape && _viewModel.CanSkip)
        {
            RequestSkip();
        }
    }

    private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (!_allowClose)
        {
            e.Cancel = true;
        }
    }
}
