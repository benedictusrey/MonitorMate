using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace MonitorMate.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
    }

    private void Credit_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        }
        catch
        {
            // A missing browser is not worth an error dialog.
        }

        e.Handled = true;
    }
}
