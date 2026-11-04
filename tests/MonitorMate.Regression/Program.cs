using System.Reflection;
using System.Windows.Threading;
using MonitorMate.Core.Interop;
using MonitorMate.Core.Models;
using MonitorMate.Core.Services;
using MonitorMate.Core.ViewModels;

internal static class Program
{
    private static int _failed;
    private static int _checks;
    private static void Require(bool value, string message) { if (!value) throw new Exception(message); }
    private static void Run(string name, Action test)
    {
        _checks++;
        try { test(); Console.WriteLine("PASS " + name); }
        catch (Exception exception) { _failed++; Console.WriteLine("FAIL " + name + ": " + exception.GetBaseException().Message); }
    }
    [STAThread]
    public static int Main()
    {
        Environment.SetEnvironmentVariable("MONITOR_MATE_DATA_DIR", Path.Combine(Path.GetTempPath(), "MonitorMate-regression-" + Guid.NewGuid().ToString("N")));
        Run("Reset updates the preferences used by display apply", () =>
        {
            var state = new DisplayState { Info = Info(), Preference = new DisplayPreference { Brightness = 30, TemperatureK = 3600, SoftwareDim = 20 } };
            new DisplayCardViewModel(state, _ => { }).ResetToOriginal();
            Require(state.Preference.TemperatureK == 6500 && state.Preference.SoftwareDim == 0 && state.Preference.Brightness == 70, "Reset left tinted/dimmed preferences active.");
        });
        Run("Setup blocks all display writes", () =>
        {
            var fixture = Create(false, true);
            fixture.Comfort.ApplyPresetAsync("night").GetAwaiter().GetResult();
            Require(NativeMethods.Writes == 0, "Preset wrote gamma before setup consent.");
        });
        Run("Disabled display receives no apply writes", () =>
        {
            var fixture = Create(true, false);
            fixture.Comfort.ApplyAllAsync().GetAwaiter().GetResult();
            Require(NativeMethods.Writes == 0, "Disabled display received a gamma write.");
        });
        Run("Manual controls work while automatic refresh stays read-only", () =>
        {
            var fixture = Create(true, true);
            fixture.Settings.Settings.AutomaticDisplayControlAllowed = false;
            fixture.Settings.Settings.ScheduleEnabled = false;
            fixture.Comfort.ApplyPresetAsync("night").GetAwaiter().GetResult();
            Require(NativeMethods.Writes > 0, "Manual preset stopped working without automatic consent.");
            NativeMethods.Writes = 0;
            fixture.Comfort.RefreshAsync().GetAwaiter().GetResult();
            Require(NativeMethods.Writes == 0, "Manual-mode refresh changed the display.");
            fixture.Comfort.ApplyPresetAsync("day", fromSchedule: true).GetAwaiter().GetResult();
            Require(NativeMethods.Writes == 0, "Scheduled preset bypassed automatic consent.");
        });
        Run("Manual preset yields schedule during hold", () =>
        {
            var fixture = Create(true, true);
            fixture.Comfort.ApplyPresetAsync("night").GetAwaiter().GetResult();
            Require(fixture.Comfort.ShouldScheduleYield(), "Manual preset hold was ignored.");
        });
        Run("Recapture does not bake in own tint", () =>
        {
            var fixture = Create(true, true);
            var original = NativeMethods.Ramp.ToArray();
            fixture.Gamma.Apply(fixture.Info, 70, 3600, 20);
            fixture.Gamma.RecaptureBaseline(fixture.Info);
            fixture.Gamma.Restore(fixture.Info);
            Require(original.SequenceEqual(NativeMethods.Ramp), "Recapture replaced original baseline with active tint.");
        });
        Run("Hotkey rebuild retains active focus Escape", () =>
        {
            var fixture = Create(true, true);
            var hotkeys = new HotkeyService(fixture.Settings, fixture.Log);
            typeof(HotkeyService).GetField("_initialized", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(hotkeys, true);
            hotkeys.SetEscapeCapture(true, () => { });
            fixture.Settings.Settings.Hotkeys.Enabled = false;
            hotkeys.Rebuild();
            Require(NativeMethods.Keys.Contains(2901), "Disabling shortcuts removed the active focus exit.");
            hotkeys.Dispose();
        });
        Run("Manual break starts a stopped timer", () =>
        {
            var fixture = Create(true, true);
            using var usage = new UsageStore(fixture.Log);
            using var reminder = new ReminderService(fixture.Settings, new OverlayService(), usage, fixture.Log);
            reminder.Start(); reminder.Pause(); reminder.TakeBreakNow();
            var timer = (DispatcherTimer)typeof(ReminderService).GetField("_timer", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(reminder)!;
            Require(timer.IsEnabled, "Manual break opened while its timer remained stopped.");
        });
        Run("Immediate skip does not record full configured break", () =>
        {
            var fixture = Create(true, true);
            fixture.Settings.Settings.Breaks.ShortBreakSeconds = 300;
            using var usage = new UsageStore(fixture.Log);
            var overlays = new OverlayService();
            using var reminder = new ReminderService(fixture.Settings, overlays, usage, fixture.Log);
            var before = usage.Today.BreakMinutes;
            reminder.TakeBreakNow(); overlays.Skip!();
            Require(usage.Today.BreakMinutes == before, "An immediate skip counted the entire five-minute break.");
        });
        Run("Solar events remain on requested local date", () =>
        {
            var date = new DateOnly(2026, 11, 4);
            foreach (var longitude in new[] { -180d, -60d, 0d, 120d, 180d })
            {
                var solar = SolarCalculator.Calculate(date, 0, longitude)!;
                Require(DateOnly.FromDateTime(solar.Sunrise) == date && DateOnly.FromDateTime(solar.Sunset) == date, "Solar date jumped outside requested day.");
            }
        });
        Run("Rejected brightness remains retryable", () =>
        {
            var fixture = Create(true, true);
            fixture.Displays.RejectWrites = true;
            var state = fixture.Comfort.States[0];
            fixture.Comfort.ApplyStateAsync(state).GetAwaiter().GetResult();
            fixture.Comfort.ApplyStateAsync(state).GetAwaiter().GetResult();
            Require(state.AppliedBrightness == 70 && fixture.Displays.BrightnessCalls == 2,
                "Unconfirmed request was cached as applied and suppressed retry.");
        });
        Run("Corrupt usage is backed up before recovery", () =>
        {
            var fixture = Create(true, true);
            var corrupted = "{bad-usage-" + Guid.NewGuid().ToString("N");
            File.WriteAllText(AppPaths.UsageFile, corrupted);
            using var usage = new UsageStore(fixture.Log);
            _ = usage.Today; usage.Flush();
            Require(Directory.GetFiles(AppPaths.DataDirectory, "usage.json.corrupt-*.bak")
                .Any(path => File.ReadAllText(path) == corrupted), "Recovery overwrote corrupt history without a backup.");
        });
        Run("Null loaded display preference is recovered", () =>
        {
            var fixture = Create(true, true);
            File.WriteAllText(AppPaths.SettingsFile, "{\"displayPreferences\":{\"bad\":null}}");
            using var settings = new SettingsService(fixture.Log);
            Require(settings.Settings.GetDisplayPreference("bad") is not null, "Null preference escaped settings normalization.");
        });
        Run("Display reset command cannot bypass setup consent", () =>
        {
            var fixture = Create(false, true);
            var services = new AppServices { Comfort = fixture.Comfort, Displays = fixture.Displays, Settings = fixture.Settings, Log = fixture.Log };
            var viewModel = new DisplaysViewModel(services);
            viewModel.ResetSelectedCommand.Execute(null);
            Require(fixture.Displays.RestoreCalls == 0, "Reset command called hardware restore before setup.");
        });
        Run("Invalid fixed boundaries do not produce an active schedule", () =>
        {
            var fixture = Create(true, true);
            fixture.Settings.Settings.DayStart = "20:00";
            fixture.Settings.Settings.EveningStart = "19:00";
            fixture.Settings.Settings.NightStart = "07:00";
            using var schedule = new ScheduleService(fixture.Settings, fixture.Comfort, fixture.Log);
            var snapshot = schedule.GetSnapshot(new DateTime(2026, 11, 4, 12, 0, 0));
            Require(!snapshot.Enabled && !string.IsNullOrEmpty(snapshot.Warning), "Contradictory boundaries remained active.");
        });
        Run("Next solar change uses tomorrow's astronomical events", () =>
        {
            var fixture = Create(true, true);
            var settings = fixture.Settings.Settings;
            settings.ScheduleMode = ScheduleMode.Solar; settings.HasLocation = true;
            settings.Latitude = 35; settings.Longitude = 139;
            using var schedule = new ScheduleService(fixture.Settings, fixture.Comfort, fixture.Log);
            var snapshot = schedule.GetSnapshot(new DateTime(2026, 11, 4, 23, 50, 0));
            var tomorrow = SolarCalculator.Calculate(new DateOnly(2026, 11, 5), 35, 139)!;
            var expected = new[] { tomorrow.Sunrise, tomorrow.Sunset, new DateTime(2026, 11, 5, 22, 30, 0) }.Min();
            Require(snapshot.NextChange == expected, "Tomorrow's boundary was copied from today's time.");
        });
        Run("Short completed breaks accumulate time", () =>
        {
            var fixture = Create(true, true);
            File.Delete(AppPaths.UsageFile);
            fixture.Settings.Settings.Breaks.ShortBreakSeconds = 20;
            using var usage = new UsageStore(fixture.Log);
            using var reminder = new ReminderService(fixture.Settings, new OverlayService(), usage, fixture.Log);
            var tick = typeof(ReminderService).GetMethod("Tick", BindingFlags.Instance | BindingFlags.NonPublic)!;
            for (var cycle = 0; cycle < 3; cycle++)
            {
                reminder.TakeBreakNow();
                for (var second = 0; second < 20; second++) tick.Invoke(reminder, null);
            }
            Require(usage.Today.BreakMinutes == 1, "Three 20-second breaks lost their accumulated minute.");
        });
        Run("Malformed usage records cannot crash startup", () =>
        {
            var fixture = Create(true, true);
            File.WriteAllText(AppPaths.UsageFile, "{\"days\":[null,{\"date\":\"invalid\"}]}");
            using var usage = new UsageStore(fixture.Log);
            Require(usage.Today.ScreenMinutes == 0, "Malformed history was not discarded safely.");
        });
        Console.WriteLine($"{_checks} checks; {_failed} failures. Native display/hotkey boundaries were simulated.");
        return _failed == 0 ? 0 : 1;
    }
    private static DisplayInfo Info() => new()
    {
        Id = Guid.NewGuid().ToString("N"), DeviceName = "regression-only", Name = "Fixture", AdapterName = "Simulated",
        Bounds = new PixelRect(), WorkArea = new PixelRect(), HardwareBrightnessSupported = true,
        HardwareBrightnessMax = 100, OriginalBrightness = 70, GammaSupported = true
    };
    private static (ComfortService Comfort, GammaService Gamma, SettingsService Settings, LogService Log, DisplayInfo Info, DisplayService Displays) Create(bool setup, bool enabled)
    {
        NativeMethods.Ramp = Enumerable.Range(0, 768).Select(i => (ushort)((i % 256) * 257)).ToArray();
        var log = new LogService(AppPaths.LogFile);
        var settings = new SettingsService(log);
        settings.Replace(new AppSettings { SetupCompleted = setup, AutomaticDisplayControlAllowed = setup, ScheduleEnabled = setup, SoundEnabled = false, Breaks = new BreakSettings { SoundEnabled = false } });
        var info = Info();
        settings.Settings.DisplayPreferences[info.Id] = new DisplayPreference { Initialized = true, Enabled = enabled, TemperatureK = 3600, Brightness = 40 };
        var displays = new DisplayService(); displays.Infos.Add(info);
        var gamma = new GammaService(log);
        var comfort = new ComfortService(displays, gamma, settings, log);
        comfort.InitializeAsync().GetAwaiter().GetResult();
        NativeMethods.Writes = 0;
        return (comfort, gamma, settings, log, info, displays);
    }
}
