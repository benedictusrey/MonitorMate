using System.Windows;
using System.Windows.Threading;
using MonitorMate.Core.ViewModels.Overlays;

namespace MonitorMate.Views.Overlays;

public partial class ToastWindow : Window
{
    private readonly ToastViewModel _viewModel;
    private readonly DispatcherTimer _autoClose;
    private bool _allowClose;

    public ToastWindow(ToastViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        _autoClose = new DispatcherTimer { Interval = TimeSpan.FromSeconds(14) };
        _autoClose.Tick += (_, _) => Close();
        Loaded += (_, _) =>
        {
            var area = SystemParameters.WorkArea;
            Left = Math.Max(area.Left + 12, area.Right - ActualWidth - 20);
            Top = Math.Max(area.Top + 12, area.Bottom - ActualHeight - 20);
            _autoClose.Start();
        };
        MouseEnter += (_, _) => _autoClose.Stop();
        MouseLeave += (_, _) =>
        {
            _autoClose.Stop();
            _autoClose.Interval = TimeSpan.FromSeconds(4);
            _autoClose.Start();
        };
        Closing += (_, e) =>
        {
            if (!_allowClose && !_autoClose.IsEnabled)
            {
                // The owner closes the toast explicitly when replacing it.
            }
        };
    }

    public void AllowClose()
    {
        _allowClose = true;
        _autoClose.Stop();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    private void PrimaryButton_Click(object sender, RoutedEventArgs e)
    {
        var command = _viewModel.PrimaryCommand;
        Close();
        command?.Execute(null);
    }

    private void SecondaryButton_Click(object sender, RoutedEventArgs e)
    {
        var command = _viewModel.SecondaryCommand;
        Close();
        command?.Execute(null);
    }
}
