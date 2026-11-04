using Avalonia;
using MonitorMate.Reminders;
namespace MonitorMate.Desktop;
public static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        if (args.Contains("--smoke-test"))
        {
            var engine = new ReminderEngine(); engine.Start(1,5); engine.Advance(TimeSpan.FromMinutes(1));
            if (engine.Phase != ReminderPhase.Break) return 1;
            Console.WriteLine($"MonitorMate 1.0.0 reminder preview: {System.Runtime.InteropServices.RuntimeInformation.RuntimeIdentifier}");
            return 0;
        }
        using var instance = new Mutex(true,"MonitorMate.ReminderPreview",out var created);
        if (!created) return 1;
        return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>().UsePlatformDetect().WithInterFont().LogToTrace();
}
