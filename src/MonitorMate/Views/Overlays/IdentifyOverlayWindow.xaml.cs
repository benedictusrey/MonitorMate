using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using MonitorMate.Core.Models;
using MonitorMate.Core.Services;

namespace MonitorMate.Views.Overlays;

public partial class IdentifyOverlayWindow : Window
{
    public IdentifyOverlayWindow(DisplayInfo display)
    {
        InitializeComponent();
        DataContext = new
        {
            NumberText = display.MonitorNumber.ToString("00"),
            DisplayName = display.Name,
            Resolution = $"{display.Resolution} / {display.AdapterName}",
            IsPrimary = display.IsPrimary
        };
        OverlayWindowHelper.ConfigureFullscreen(this, display.Bounds, clickThrough: true);
        Opacity = 0;
        Loaded += (_, _) =>
        {
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(220));
            BeginAnimation(OpacityProperty, fadeIn);
            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3.0) };
            timer.Tick += (_, _) =>
            {
                timer.Stop();
                var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(260));
                fadeOut.Completed += (_, _) => Close();
                BeginAnimation(OpacityProperty, fadeOut);
            };
            timer.Start();
        };
    }
}
