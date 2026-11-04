namespace MonitorMate.Reminders;

public enum ReminderPhase { Stopped, Working, Break }

public sealed class ReminderEngine
{
    private int _workMinutes = 20;
    private int _breakSeconds = 20;
    public ReminderPhase Phase { get; private set; }
    public bool IsPaused { get; private set; }
    public TimeSpan Remaining { get; private set; }
    public TimeSpan Duration { get; private set; }
    public int CompletedBreaks { get; private set; }

    public void Start(int workMinutes, int breakSeconds)
    {
        Validate(workMinutes, breakSeconds);
        _workMinutes = workMinutes;
        _breakSeconds = breakSeconds;
        Begin(ReminderPhase.Working, TimeSpan.FromMinutes(workMinutes));
    }

    public void Pause() { if (Phase != ReminderPhase.Stopped) IsPaused = true; }
    public void Resume() => IsPaused = false;
    public void Stop() => Begin(ReminderPhase.Stopped, TimeSpan.Zero);
    public void TakeBreak(int breakSeconds)
    {
        Validate(_workMinutes, breakSeconds);
        _breakSeconds = breakSeconds;
        Begin(ReminderPhase.Break, TimeSpan.FromSeconds(breakSeconds));
    }
    public void SkipBreak()
    {
        if (Phase == ReminderPhase.Break)
            Begin(ReminderPhase.Working, TimeSpan.FromMinutes(_workMinutes));
    }
    public void Advance(TimeSpan elapsed)
    {
        if (elapsed < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(elapsed));
        if (IsPaused || Phase == ReminderPhase.Stopped) return;
        Remaining -= elapsed;
        if (Remaining > TimeSpan.Zero) return;
        if (Phase == ReminderPhase.Working)
            Begin(ReminderPhase.Break, TimeSpan.FromSeconds(_breakSeconds));
        else
        {
            CompletedBreaks++;
            Begin(ReminderPhase.Working, TimeSpan.FromMinutes(_workMinutes));
        }
    }
    private void Begin(ReminderPhase phase, TimeSpan duration)
    {
        Phase = phase; Remaining = duration; Duration = duration; IsPaused = false;
    }
    public static void Validate(int workMinutes, int breakSeconds)
    {
        if (workMinutes is < 1 or > 240) throw new ArgumentOutOfRangeException(nameof(workMinutes));
        if (breakSeconds is < 5 or > 600) throw new ArgumentOutOfRangeException(nameof(breakSeconds));
    }
}
