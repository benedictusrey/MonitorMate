using System.Windows;
using MonitorMate.Core.ViewModels.Overlays;

namespace MonitorMate.Views.Overlays;

public partial class GentleBreakWindow : Window
{
    private readonly Action _skip;

    public GentleBreakWindow(BreakOverlayViewModel viewModel, Action skip)
    {
        InitializeComponent();
        _skip = skip;
        DataContext = viewModel;
        Loaded += (_, _) =>
        {
            var area = SystemParameters.WorkArea;
            Left = Math.Max(area.Left + 12, area.Right - ActualWidth - 20);
            Top = Math.Max(area.Top + 12, area.Bottom - ActualHeight - 20);
        };
    }

    private void SkipButton_Click(object sender, RoutedEventArgs e) => _skip();
}
