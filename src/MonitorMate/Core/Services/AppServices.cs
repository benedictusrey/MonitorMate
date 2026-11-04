namespace MonitorMate.Core.Services;

public sealed class AppServices : IDisposable
{
    private bool _disposed;

    public AppServices()
    {
        AppPaths.EnsureCreated();
        Log = new LogService(AppPaths.LogFile);
        Settings = new SettingsService(Log);
        Displays = new DisplayService(Log);
        Gamma = new GammaService(Log);
        Usage = new UsageStore(Log);
        Comfort = new ComfortService(Displays, Gamma, Settings, Log);
        Overlays = new OverlayService(Comfort, Log);
        Hotkeys = new HotkeyService(Settings, Log);
        Reminders = new ReminderService(Settings, Overlays, Usage, Log);
        Schedule = new ScheduleService(Settings, Comfort, Log);
        Focus = new FocusSessionService(Settings, Overlays, Usage, Hotkeys, Log);
        UsageTracker = new UsageTracker(Usage);
        Startup = new StartupService(Log);
    }

    public LogService Log { get; }
    public SettingsService Settings { get; }
    public DisplayService Displays { get; }
    public GammaService Gamma { get; }
    public UsageStore Usage { get; }
    public ComfortService Comfort { get; }
    public OverlayService Overlays { get; }
    public HotkeyService Hotkeys { get; }
    public ReminderService Reminders { get; }
    public ScheduleService Schedule { get; }
    public FocusSessionService Focus { get; }
    public UsageTracker UsageTracker { get; }
    public StartupService Startup { get; }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try { Focus.Dispose(); } catch { }
        try { Reminders.Dispose(); } catch { }
        try { Schedule.Dispose(); } catch { }
        try { UsageTracker.Dispose(); } catch { }
        try { Overlays.Dispose(); } catch { }
        try { Comfort.Dispose(); } catch { }
        try { Displays.Dispose(); } catch { }
        try { Gamma.Dispose(); } catch { }
        try { Usage.Dispose(); } catch { }
        try { Hotkeys.Dispose(); } catch { }
        try { Settings.Dispose(); } catch { }
    }
}
