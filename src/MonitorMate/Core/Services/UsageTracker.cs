using System.Windows.Threading;

namespace MonitorMate.Core.Services;

public sealed class UsageTracker : IDisposable
{
    private readonly UsageStore _usage;
    private readonly DispatcherTimer _timer;
    private bool _sessionLocked;
    private bool _suspended;
    private bool _disposed;

    public UsageTracker(UsageStore usage)
    {
        _usage = usage;
        _timer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromSeconds(60) };
        _timer.Tick += (_, _) => Tick();
    }

    public void Start() => _timer.Start();
    public void Stop() => _timer.Stop();

    public void SetSessionLocked(bool locked) => _sessionLocked = locked;
    public void SetSuspended(bool suspended) => _suspended = suspended;

    private void Tick()
    {
        if (_disposed || _sessionLocked || _suspended)
        {
            return;
        }

        if (ActivityProbe.GetIdleTime() < TimeSpan.FromMinutes(2))
        {
            _usage.AddScreenMinute();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _timer.Stop();
    }
}
