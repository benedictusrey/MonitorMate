using System.Windows.Threading;
using MonitorMate.Core.Models;
using MonitorMate.Core.Mvvm;

namespace MonitorMate.Core.Services;

public sealed class ScheduleSnapshot
{
    public bool Enabled { get; init; }
    public string PeriodName { get; init; } = "Daylight";
    public string PeriodDescription { get; init; } = string.Empty;
    public string PresetId { get; init; } = "day";
    public string PresetName { get; init; } = "Daylight";
    public DateTime? NextChange { get; init; }
    public string NextChangeText { get; init; } = "Schedule off";
    public string DayLabel { get; init; } = "07:00";
    public string EveningLabel { get; init; } = "19:00";
    public string NightLabel { get; init; } = "22:30";
    public string SunriseText { get; init; } = "—";
    public string SunsetText { get; init; } = "—";
    public bool UsingSolar { get; init; }
    public string? Warning { get; init; }
}

public sealed class ScheduleService : ObservableObject, IDisposable
{
    private readonly SettingsService _settings;
    private readonly ComfortService _comfort;
    private readonly LogService _log;
    private readonly DispatcherTimer _timer;
    private string _lastAppliedKey = string.Empty;
    private bool _started;
    private bool _disposed;

    public ScheduleService(SettingsService settings, ComfortService comfort, LogService log)
    {
        _settings = settings;
        _comfort = comfort;
        _log = log;
        _timer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(30)
        };
        _timer.Tick += async (_, _) => await TickAsync();
    }

    public event EventHandler<ScheduleSnapshot>? SnapshotChanged;

    public void Start()
    {
        if (_started) return;
        _started = true;
        _timer.Start();
        _ = TickAsync(force: true);
    }

    public void Stop()
    {
        _started = false;
        _timer.Stop();
        SnapshotChanged?.Invoke(this, GetSnapshot());
    }

    public ScheduleSnapshot GetSnapshot(DateTime? now = null)
    {
        var at = now ?? DateTime.Now;
        var settings = _settings.Settings;
        if (!settings.AutomaticDisplayControlAllowed || !settings.ScheduleEnabled)
        {
            return new ScheduleSnapshot
            {
                Enabled = false,
                NextChangeText = "Schedule off",
                PresetId = settings.LastPresetId,
                PresetName = PresetCatalog.Get(settings.LastPresetId).Name,
                Warning = null
            };
        }

        if (settings.ScheduleMode == ScheduleMode.Fixed &&
            (!TimeOnly.TryParse(settings.DayStart, out var day) ||
             !TimeOnly.TryParse(settings.EveningStart, out var evening) ||
             !TimeOnly.TryParse(settings.NightStart, out var night) || day >= evening || evening >= night))
        {
            return new ScheduleSnapshot
            {
                Enabled = false,
                NextChangeText = "Correct the schedule times",
                Warning = "Daylight, evening and night must use distinct times in that order."
            };
        }
        var times = ResolveTimes(at, settings, out var warning);
        var period = ResolvePeriod(at, times, settings);
        var next = NextChange(at, times, settings);
        var preset = PresetCatalog.Get(period.PresetId);
        return new ScheduleSnapshot
        {
            Enabled = true,
            PeriodName = period.Name,
            PeriodDescription = period.Description,
            PresetId = period.PresetId,
            PresetName = preset.Name,
            NextChange = next,
            NextChangeText = next is null ? "No further change today" : FormatNext(next.Value, at),
            DayLabel = FormatTime(times.DayStart),
            EveningLabel = FormatTime(times.EveningStart),
            NightLabel = FormatTime(times.NightStart),
            SunriseText = FormatTime(times.DayStart),
            SunsetText = FormatTime(times.EveningStart),
            UsingSolar = settings.ScheduleMode == ScheduleMode.Solar && times.IsSolar,
            Warning = warning
        };
    }

    public async Task ApplyCurrentAsync(bool force = false)
    {
        var settings = _settings.Settings;
        if (!GetSnapshot().Enabled || _comfort.IsPaused)
        {
            SnapshotChanged?.Invoke(this, GetSnapshot());
            return;
        }

        if (_comfort.ShouldScheduleYield())
        {
            SnapshotChanged?.Invoke(this, GetSnapshot());
            return;
        }

        var now = DateTime.Now;
        var times = ResolveTimes(now, settings, out _);
        var period = ResolvePeriod(now, times, settings);
        var key = $"{now:yyyy-MM-dd}|{period.Key}";
        if (!force && key == _lastAppliedKey)
        {
            SnapshotChanged?.Invoke(this, GetSnapshot());
            return;
        }

        _lastAppliedKey = key;
        await _comfort.ApplyPresetAsync(period.PresetId, fromSchedule: true);
        SnapshotChanged?.Invoke(this, GetSnapshot());
    }

    private async Task TickAsync(bool force = false)
    {
        if (_disposed) return;
        try
        {
            await ApplyCurrentAsync(force);
        }
        catch (Exception ex)
        {
            _log.Error("Schedule tick failed.", ex);
        }
    }

    public void ResetAppliedKey() => _lastAppliedKey = string.Empty;

    private static ScheduleTimes ResolveTimes(DateTime now, AppSettings settings, out string? warning)
    {
        warning = null;
        var fixedDay = ParseTime(settings.DayStart, new TimeOnly(7, 0));
        var fixedEvening = ParseTime(settings.EveningStart, new TimeOnly(19, 0));
        var night = ParseTime(settings.NightStart, new TimeOnly(22, 30));
        var day = now.Date.Add(fixedDay.ToTimeSpan());
        var evening = now.Date.Add(fixedEvening.ToTimeSpan());

        if (settings.ScheduleMode == ScheduleMode.Solar && settings.HasLocation)
        {
            var solar = SolarCalculator.Calculate(DateOnly.FromDateTime(now), settings.Latitude, settings.Longitude);
            if (solar is not null)
            {
                day = solar.Sunrise;
                evening = solar.Sunset;
                return new ScheduleTimes(day, evening, now.Date.Add(night.ToTimeSpan()), true);
            }

            warning = "Sunrise and sunset could not be calculated for this location. Fixed hours are being used.";
        }
        else if (settings.ScheduleMode == ScheduleMode.Solar)
        {
            warning = "Add a latitude and longitude to use sunrise and sunset. Fixed hours are being used.";
        }

        return new ScheduleTimes(day, evening, now.Date.Add(night.ToTimeSpan()), false);
    }

    private static SchedulePeriod ResolvePeriod(DateTime now, ScheduleTimes times, AppSettings settings)
    {
        var time = TimeOnly.FromDateTime(now);
        var dayStart = TimeOnly.FromDateTime(times.DayStart);
        var eveningStart = TimeOnly.FromDateTime(times.EveningStart);
        var nightStart = TimeOnly.FromDateTime(times.NightStart);

        if (time < dayStart || time >= nightStart)
        {
            return new SchedulePeriod("night", "Night", "Low light with a deep warm cast.", settings.NightPresetId);
        }

        if (time >= eveningStart)
        {
            return new SchedulePeriod("evening", "Evening", "A softer, warmer screen as daylight fades.", settings.EveningPresetId);
        }

        return new SchedulePeriod("day", "Daylight", "Neutral colour for bright rooms.", settings.DayPresetId);
    }

    private static DateTime? NextChange(DateTime now, ScheduleTimes times, AppSettings settings)
    {
        var candidates = new List<DateTime>();
        for (var offset = 0; offset <= 2; offset++)
        {
            var dailyTimes = offset == 0 ? times : ResolveTimes(now.Date.AddDays(offset), settings, out _);
            candidates.Add(dailyTimes.DayStart);
            candidates.Add(dailyTimes.EveningStart);
            candidates.Add(dailyTimes.NightStart);
        }

        return candidates.Where(c => c > now).OrderBy(c => c).Cast<DateTime?>().FirstOrDefault();
    }

    private static string FormatNext(DateTime next, DateTime now)
    {
        var delta = next - now;
        if (delta.TotalMinutes < 1)
        {
            return "in under a minute";
        }

        if (delta.TotalHours < 1)
        {
            return $"in {(int)Math.Round(delta.TotalMinutes)} min";
        }

        if (next.Date == now.Date)
        {
            return $"today at {next:HH:mm}";
        }

        if (next.Date == now.Date.AddDays(1))
        {
            return $"tomorrow at {next:HH:mm}";
        }

        return $"{next:ddd} at {next:HH:mm}";
    }

    private static TimeOnly ParseTime(string text, TimeOnly fallback)
        => TimeOnly.TryParse(text, out var value) ? value : fallback;

    private static string FormatTime(DateTime value) => value.ToString("HH:mm");

    public void Dispose()
    {
        _disposed = true;
        _timer.Stop();
    }

    private sealed record ScheduleTimes(DateTime DayStart, DateTime EveningStart, DateTime NightStart, bool IsSolar);
    private sealed record SchedulePeriod(string Key, string Name, string Description, string PresetId);
}
