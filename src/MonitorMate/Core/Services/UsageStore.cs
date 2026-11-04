using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Threading;
using MonitorMate.Core.Models;

namespace MonitorMate.Core.Services;

public sealed class UsageStore : IDisposable
{
    private readonly LogService _log;
    private readonly JsonSerializerOptions _json;
    private readonly DispatcherTimer _saveTimer;
    private readonly object _gate = new();
    private UsageData _data = new();
    private bool _dirty;
    private bool _recoveryBlocked;

    public UsageStore(LogService log)
    {
        _log = log;
        AppPaths.EnsureCreated();
        _json = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };
        Load();
        _saveTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(45)
        };
        _saveTimer.Tick += (_, _) => Flush();
        _saveTimer.Start();
    }

    public DayUsage Today => GetOrCreate(DateOnly.FromDateTime(DateTime.Now));

    public DayUsage GetOrCreate(DateOnly date)
    {
        lock (_gate)
        {
            var key = date.ToString("yyyy-MM-dd");
            var record = _data.Days.FirstOrDefault(d => d.Date == key);
            if (record is not null)
            {
                return record;
            }

            record = new DayUsage { Date = key };
            _data.Days.Add(record);
            _data.Days = _data.Days.OrderBy(d => d.Date, StringComparer.Ordinal).ToList();
            _dirty = true;
            return record;
        }
    }

    public IReadOnlyList<WeekPoint> GetLastWeek()
    {
        var result = new List<WeekPoint>(7);
        var today = DateOnly.FromDateTime(DateTime.Now);
        for (var i = 6; i >= 0; i--)
        {
            var date = today.AddDays(-i);
            var record = GetOrCreate(date);
            result.Add(new WeekPoint
            {
                Label = date.ToString("ddd"),
                DateLabel = date.ToString("MMM d"),
                ScreenMinutes = record.ScreenMinutes,
                BreakMinutes = record.BreakMinutes,
                BreaksCompleted = record.BreaksCompleted,
                BreaksSkipped = record.BreaksSkipped,
                IsToday = i == 0
            });
        }

        return result;
    }

    public void AddScreenMinute()
    {
        lock (_gate)
        {
            Today.ScreenMinutes++;
            _dirty = true;
        }
    }

    public void AddBreakMinute()
    {
        lock (_gate)
        {
            Today.BreakMinutes++;
            _dirty = true;
        }
    }

    public void AddBreakSeconds(int seconds)
    {
        if (seconds <= 0) return;
        lock (_gate)
        {
            var today = Today;
            var total = (long)Math.Clamp(today.BreakRemainderSeconds, 0, 59) + seconds;
            today.BreakMinutes += (int)(total / 60);
            today.BreakRemainderSeconds = (int)(total % 60);
            _dirty = true;
        }
    }

    public void AddBreakMinutes(int minutes)
    {
        if (minutes <= 0) return;
        lock (_gate)
        {
            Today.BreakMinutes += minutes;
            _dirty = true;
        }
    }

    public void AddFocusMinutes(int minutes)
    {
        if (minutes <= 0) return;
        lock (_gate)
        {
            Today.FocusMinutes += minutes;
            _dirty = true;
        }
    }

    public void RecordReminder()
    {
        lock (_gate)
        {
            Today.RemindersShown++;
            _dirty = true;
        }
    }

    public void RecordBreakCompleted()
    {
        lock (_gate)
        {
            Today.BreaksCompleted++;
            _dirty = true;
        }
    }

    public void RecordBreakSkipped()
    {
        lock (_gate)
        {
            Today.BreaksSkipped++;
            _dirty = true;
        }
    }

    public void ResetAll()
    {
        lock (_gate)
        {
            _data = new UsageData();
            _recoveryBlocked = false;
            _dirty = true;
        }

        Flush();
    }

    private void Load()
    {
        try
        {
            if (!File.Exists(AppPaths.UsageFile))
            {
                return;
            }

            var json = File.ReadAllText(AppPaths.UsageFile);
            var loaded = JsonSerializer.Deserialize<UsageData>(json, _json);
            if (loaded is not null)
            {
                loaded.Days ??= new List<DayUsage>();
                _data = loaded;
            }
        }
        catch (Exception ex)
        {
            _log.Error("Could not load usage history.", ex);
            BackupCorruptUsage();
            _data = new UsageData();
        }

        var validDays = _data.Days.Where(day => day is not null &&
            DateOnly.TryParseExact(day.Date, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out _)).ToList();
        if (validDays.Count != _data.Days.Count) BackupCorruptUsage();
        var cutoff = DateTime.Now.AddDays(-120).ToString("yyyy-MM-dd");
        _data.Days = validDays.Where(d => string.CompareOrdinal(d.Date, cutoff) >= 0)
            .OrderBy(d => d.Date, StringComparer.Ordinal)
            .ToList();
    }

    private void BackupCorruptUsage()
    {
        try
        {
            File.Copy(AppPaths.UsageFile, AppPaths.UsageFile + $".corrupt-{DateTime.UtcNow:yyyyMMdd-HHmmss-fffffff}.bak", overwrite: false);
        }
        catch (Exception backupError)
        {
            _recoveryBlocked = true;
            _log.Error("Usage history could not be backed up; automatic saves are blocked.", backupError);
        }
    }

    public void Flush()
    {
        if (!_dirty || _recoveryBlocked)
        {
            return;
        }

        lock (_gate)
        {
            try
            {
                _data.LastSavedUtc = DateTime.UtcNow;
                var json = JsonSerializer.Serialize(_data, _json);
                var temp = AppPaths.UsageFile + ".tmp";
                File.WriteAllText(temp, json);
                File.Move(temp, AppPaths.UsageFile, overwrite: true);
                _dirty = false;
            }
            catch (Exception ex)
            {
                _log.Error("Could not save usage history.", ex);
            }
        }
    }

    public void Dispose()
    {
        _saveTimer.Stop();
        Flush();
    }
}
