using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
namespace MonitorMate.Desktop;
public sealed class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = new MainWindow(); desktop.MainWindow = window;
            if (desktop.Args?.Contains("--ui-smoke") == true)
                window.Opened += (_, _) => Avalonia.Threading.DispatcherTimer.RunOnce(() => desktop.Shutdown(), TimeSpan.FromSeconds(1));
        }
        base.OnFrameworkInitializationCompleted();
    }
}
