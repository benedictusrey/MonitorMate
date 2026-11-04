namespace MonitorMate.Core.Models;

public enum ReminderPhase
{
    Stopped,
    Working,
    PreBreak,
    Breaking,
    Paused
}

public sealed class BreakSession
{
    public int TotalSeconds { get; init; }
    public int RemainingSeconds { get; set; }
    public bool IsLongBreak { get; init; }
    public BreakStyle Style { get; init; }
    public string Prompt { get; init; } = string.Empty;
    public string SubPrompt { get; init; } = string.Empty;
    public string SessionSummary { get; init; } = string.Empty;
    public bool IsSkippable => Style != BreakStyle.Strict;
}

public sealed class ReminderSnapshot
{
    public ReminderPhase Phase { get; init; }
    public bool IsEnabled { get; init; }
    public bool IsRunning { get; init; }
    public int RemainingSeconds { get; init; }
    public int TotalSeconds { get; init; }
    public bool IsLongBreak { get; init; }
    public DateTime? NextBreakAt { get; init; }
    public int CompletedToday { get; init; }
    public int SkippedToday { get; init; }
    public int CyclesToday { get; init; }
    public string PhaseLabel { get; init; } = string.Empty;
}
