using System.Windows;
using System.Windows.Input;

namespace MonitorMate.Views;

public partial class ConfirmDialog : Window
{
    public ConfirmDialog(string title, string message, string confirmLabel)
    {
        InitializeComponent();
        TitleText.Text = title;
        MessageText.Text = message;
        ConfirmButton.Content = confirmLabel;
    }

    public bool Result { get; private set; }

    public static bool Show(Window owner, string title, string message, string confirmLabel)
    {
        var dialog = new ConfirmDialog(title, message, confirmLabel) { Owner = owner };
        dialog.ShowDialog();
        return dialog.Result;
    }

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        Result = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => Close();

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }
}
