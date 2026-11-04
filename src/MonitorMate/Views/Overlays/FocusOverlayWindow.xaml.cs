using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using MonitorMate.Core.Interop;
using MonitorMate.Core.Models;
using MonitorMate.Core.Services;
using MonitorMate.Core.ViewModels.Overlays;

namespace MonitorMate.Views.Overlays;

public partial class FocusOverlayWindow : Window
{
    private readonly FocusOverlayViewModel _viewModel;
    private readonly DisplayInfo _display;
    private readonly DispatcherTimer _timer;
    private double _dpiScale = 1.0;

    public FocusOverlayWindow(FocusOverlayViewModel viewModel, DisplayInfo display)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _display = display;
        DataContext = viewModel;
        OverlayWindowHelper.ConfigureFullscreen(this, display.Bounds, clickThrough: true);
        Loaded += (_, _) =>
        {
            _dpiScale = VisualTreeHelper.GetDpi(this).DpiScaleX;
            UpdateGeometry();
            BringToFront();
        };
        DpiChanged += (_, _) => _dpiScale = VisualTreeHelper.GetDpi(this).DpiScaleX;
        _timer = new DispatcherTimer(DispatcherPriority.Render) { Interval = TimeSpan.FromMilliseconds(33) };
        _timer.Tick += (_, _) => UpdateGeometry();
        _timer.Start();
        Closed += (_, _) => _timer.Stop();
    }

    public void BringToFront()
    {
        var handle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
        if (handle == IntPtr.Zero) return;
        NativeMethods.SetWindowPos(handle, NativeMethods.HWND_TOPMOST,
            _display.Bounds.X, _display.Bounds.Y,
            Math.Max(1, _display.Bounds.Width), Math.Max(1, _display.Bounds.Height),
            NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_SHOWWINDOW);
    }

    private void UpdateGeometry()
    {
        var width = ActualWidth;
        var height = ActualHeight;
        if (width <= 1 || height <= 1)
        {
            return;
        }

        NativeMethods.GetCursorPos(out var cursor);
        var localX = (cursor.X - _display.Bounds.X) / Math.Max(0.1, _dpiScale);
        var localY = (cursor.Y - _display.Bounds.Y) / Math.Max(0.1, _dpiScale);

        Rect hole;
        if (_viewModel.Mode == FocusMode.Reading)
        {
            var bandHeight = Math.Clamp(_viewModel.BandHeight, 80, height - 20);
            var center = _viewModel.FollowPointer ? localY : height * 0.46;
            var top = Math.Clamp(center - bandHeight / 2, 0, Math.Max(0, height - bandHeight));
            hole = new Rect(0, top, width, bandHeight);
        }
        else
        {
            const double spotlightWidth = 340;
            const double spotlightHeight = 210;
            var left = Math.Clamp(localX - spotlightWidth / 2, 0, Math.Max(0, width - spotlightWidth));
            var top = Math.Clamp(localY - spotlightHeight / 2, 0, Math.Max(0, height - spotlightHeight));
            hole = new Rect(left, top, spotlightWidth, spotlightHeight);
        }

        var group = new GeometryGroup { FillRule = FillRule.EvenOdd };
        group.Children.Add(new RectangleGeometry(new Rect(0, 0, width, height)));
        group.Children.Add(new RectangleGeometry(hole, 0, 0));
        Curtain.Data = group;
    }
}
