namespace MonitorMate.Core.Models;

public sealed class DayUsage
{
    public string Date { get; set; } = string.Empty;
    public int ScreenMinutes { get; set; }
    public int BreakMinutes { get; set; }
    public int BreakRemainderSeconds { get; set; }
    public int FocusMinutes { get; set; }
    public int BreaksCompleted { get; set; }
    public int BreaksSkipped { get; set; }
    public int RemindersShown { get; set; }

    public int AdherencePercent =>
        RemindersShown <= 0 ? 0 : (int)Math.Round(BreaksCompleted * 100.0 / RemindersShown);
}

public sealed class UsageData
{
    public List<DayUsage> Days { get; set; } = new();
    public DateTime LastSavedUtc { get; set; } = DateTime.UtcNow;
}

public sealed class WeekPoint
{
    public required string Label { get; init; }
    public required string DateLabel { get; init; }
    public int ScreenMinutes { get; init; }
    public int BreakMinutes { get; init; }
    public int FocusMinutes { get; init; }
    public int BreaksCompleted { get; init; }
    public int BreaksSkipped { get; init; }
    public bool IsToday { get; init; }
}
