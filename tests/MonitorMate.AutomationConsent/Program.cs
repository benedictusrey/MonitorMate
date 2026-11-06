using System.IO;
using System.Windows.Input;
using MonitorMate.Core.Models;
using MonitorMate.Core.Services;
using MonitorMate.Core.ViewModels;

internal static class Program
{
    private static int _checks;
    private static int _failures;
    private static void Require(bool value, string message) { if (!value) throw new Exception(message); }
    private static void Check(string name, Action test)
    {
        _checks++;
        try { test(); Console.WriteLine("PASS " + name); }
        catch (Exception error) { _failures++; Console.WriteLine("FAIL " + name + ": " + error.GetBaseException().Message); }
    }
    private static MainViewModel Create(AppServices services, Func<bool> answer) =>
        new(services, () => { }, () => { }, () => { }, (_, _, _) => answer(), () => { });
    private static ICommand AutomaticSetup(MainViewModel main) =>
        main.TodayPage.GetType().GetProperty("EnableAutomaticCommand")?.GetValue(main.TodayPage) as ICommand
        ?? throw new Exception("The first-run automatic choice is missing.");

    [STAThread]
    public static int Main()
    {
        Environment.SetEnvironmentVariable("MONITOR_MATE_DATA_DIR", Path.Combine(Path.GetTempPath(), "MonitorMate-consent-" + Guid.NewGuid().ToString("N")));
        _ = new MonitorMate.App();
        Check("New settings never schedule or restore automatically", () =>
        {
            var settings = new AppSettings();
            Require(!settings.ScheduleEnabled && !settings.RestoreOnLaunch, "New users start with automatic changes enabled.");
        });
        Check("First-run manual choice completes setup without automation", () =>
        {
            using var services = new AppServices(); services.Settings.Replace(new AppSettings());
            using var main = Create(services, () => false);
            main.TodayPage.FinishSetupCommand.Execute(null);
            var settings = services.Settings.Settings;
            Require(settings.SetupCompleted && !settings.ScheduleEnabled && !settings.RestoreOnLaunch,
                "Manual choice left automatic changes enabled or manual controls locked.");
        });
        Check("Declining later automation keeps manual mode and asks again", () =>
        {
            using var services = new AppServices();
            services.Settings.Replace(new AppSettings { SetupCompleted = true, ScheduleEnabled = false, RestoreOnLaunch = false });
            var prompts = 0;
            using var main = Create(services, () => { prompts++; return false; });
            main.ComfortPage.ScheduleEnabled = true;
            main.ComfortPage.ScheduleEnabled = true;
            Require(prompts == 2 && !services.Settings.Settings.ScheduleEnabled && !main.ComfortPage.ScheduleEnabled,
                "Rejected consent enabled automation or prevented another prompt.");
        });
        Check("Disabling and reenabling automation requests fresh consent", () =>
        {
            using var services = new AppServices();
            services.Settings.Replace(new AppSettings { SetupCompleted = true, ScheduleEnabled = false, RestoreOnLaunch = false });
            var prompts = 0;
            using var main = Create(services, () => { prompts++; return true; });
            main.ComfortPage.ScheduleEnabled = true;
            main.ComfortPage.ScheduleEnabled = false;
            Require(main.TodayPage.ScheduleNextText == "Schedule is off", "Stopping automation left the UI showing an active schedule.");
            main.ComfortPage.ScheduleEnabled = true;
            Require(prompts == 2 && services.Settings.Settings.ScheduleEnabled,
                "Automatic mode bypassed a fresh consent request.");
        });
        Check("Declining the first automatic prompt still enables manual use", () =>
        {
            using var services = new AppServices(); services.Settings.Replace(new AppSettings());
            var prompts = 0;
            using var main = Create(services, () => { prompts++; return false; });
            AutomaticSetup(main).Execute(null);
            Require(prompts == 1 && services.Settings.Settings.SetupCompleted && !services.Settings.Settings.ScheduleEnabled,
                "Declining automatic setup prevented manual use.");
        });
        Check("First-run automatic choice requires explicit acceptance", () =>
        {
            using var services = new AppServices(); services.Settings.Replace(new AppSettings());
            var prompts = 0;
            using var main = Create(services, () => { prompts++; return true; });
            AutomaticSetup(main).Execute(null);
            Require(prompts == 1 && services.Settings.Settings.SetupCompleted && services.Settings.Settings.ScheduleEnabled,
                "Accepted automatic setup was not applied.");
        });
        Check("Launch restore also requires explicit acceptance", () =>
        {
            using var services = new AppServices();
            services.Settings.Replace(new AppSettings { SetupCompleted = true, ScheduleEnabled = false, RestoreOnLaunch = false });
            var prompts = 0;
            using var main = Create(services, () => { prompts++; return false; });
            main.SettingsPage.RestoreOnLaunch = true;
            Require(prompts == 1 && !services.Settings.Settings.RestoreOnLaunch, "Launch restore bypassed manual mode consent.");
        });
        Check("Accepted launch restore persists and can be declined on retry", () =>
        {
            using var services = new AppServices(); services.Settings.Replace(new AppSettings());
            var prompts = 0;
            using var main = Create(services, () => ++prompts == 1);
            main.SettingsPage.RestoreOnLaunch = true;
            services.Settings.Flush();
            using (var loaded = new SettingsService(services.Log))
                Require(loaded.Settings.AutomaticDisplayControlAllowed && loaded.Settings.RestoreOnLaunch,
                    "Accepted launch restore did not survive reload.");
            main.SettingsPage.RestoreOnLaunch = false;
            main.SettingsPage.RestoreOnLaunch = true;
            Require(prompts == 2 && !services.Settings.Settings.RestoreOnLaunch &&
                !services.Settings.Settings.AutomaticDisplayControlAllowed, "Declined retry restored automatic access.");
        });
        Check("Legacy schedule flags do not imply automatic-control consent", () =>
        {
            using var services = new AppServices();
            services.Settings.Replace(new AppSettings { SetupCompleted = true, ScheduleEnabled = true, RestoreOnLaunch = true });
            Require(!services.Schedule.GetSnapshot().Enabled, "Legacy flags enabled automatic control without the new approval.");
        });
        Check("Startup summary distinguishes launch from profile restoration", () =>
        {
            using var services = new AppServices(); services.Settings.Replace(new AppSettings { SetupCompleted=true });
            using var main = Create(services, () => true);
            Require(main.SettingsPage.StartupSummary.Contains("stay manual"), "Manual launch behavior is not explained.");
            main.SettingsPage.RestoreOnLaunch = true;
            Require(main.SettingsPage.StartupSummary.Contains("per-display adjustments"), "Saved-profile restoration is not explained.");
            main.TodayPage.RefreshAll();
            Require(main.TodayPage.CurrentPeriod == "Saved", "Saved-profile mode is mislabeled as manual.");
            main.ComfortPage.ScheduleEnabled = true;
            Require(main.SettingsPage.StartupSummary.Contains("schedule takes priority"), "Schedule/restore precedence is hidden.");
        });
        Check("Invalid fixed hours are rejected before saving", () =>
        {
            using var services = new AppServices(); services.Settings.Replace(new AppSettings());
            using var main = Create(services, () => false);
            main.ComfortPage.DayStartText="19:00"; main.ComfortPage.EveningStartText="07:00";
            var method=typeof(ComfortViewModel).GetMethod("ApplyScheduleAsync",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!;
            ((Task)method.Invoke(main.ComfortPage,null)!).GetAwaiter().GetResult();
            Require(!string.IsNullOrWhiteSpace(main.ComfortPage.ScheduleError) && services.Settings.Settings.DayStart=="07:00",
                "Contradictory hours were saved without a field error.");
        });
        Console.WriteLine($"{_checks} consent checks; {_failures} failures. No display enumeration or full UI launch.");
        return _failures == 0 ? 0 : 1;
    }
}
