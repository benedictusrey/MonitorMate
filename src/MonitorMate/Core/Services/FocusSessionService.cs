using System.Windows.Threading;
using MonitorMate.Core.Models;

namespace MonitorMate.Core.Services;

public sealed class FocusSessionService : IDisposable
{
    private readonly SettingsService _settings;
    private readonly OverlayService _overlays;
    private readonly UsageStore _usage;
    private readonly HotkeyService _hotkeys;
    private readonly LogService _log;
    private readonly DispatcherTimer _timer;
    private DateTime _startedAt;
    private DateTime _endsAt;
    private bool _disposed;

    public FocusSessionService(SettingsService settings, OverlayService overlays, UsageStore usage,
        HotkeyService hotkeys, LogService log)
    {
        _settings = settings;
        _overlays = overlays;
        _usage = usage;
        _hotkeys = hotkeys;
        _log = log;
        _timer = new DispatcherTimer(DispatcherPriority.Normal) { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) => Tick();
    }

    public bool IsActive { get; private set; }
    public int RemainingSeconds { get; private set; }
    public int TotalSeconds { get; private set; }
    public DateTime? EndsAt => IsActive ? _endsAt : null;

    public event EventHandler? StateChanged;
    public event EventHandler? Started;
    public event EventHandler<string>? Ended;

    public bool Start()
    {
        if (_disposed) return false;
        if (IsActive)
        {
            Stop("restarted");
        }

        var focus = _settings.Settings.Focus;
        _startedAt = DateTime.Now;
        _endsAt = _startedAt.AddMinutes(Math.Clamp(focus.DefaultMinutes, 1, 180));
        RemainingSeconds = Math.Max(1, (int)Math.Round((_endsAt - DateTime.Now).TotalSeconds));
        TotalSeconds = RemainingSeconds;
        IsActive = true;
        _overlays.StartFocus(focus);
        if (!_hotkeys.SetEscapeCapture(true, () => Stop("escape")))
        {
            _log.Warn("Could not register Escape while focus mode was active.");
        }

        _timer.Start();
        Started?.Invoke(this, EventArgs.Empty);
        StateChanged?.Invoke(this, EventArgs.Empty);
        return true;
    }

    public void Stop(string reason = "finished")
    {
        if (!IsActive) return;
        IsActive = false;
        _timer.Stop();
        _hotkeys.SetEscapeCapture(false);
        _overlays.StopFocus();
        var elapsed = DateTime.Now - _startedAt;
        if (elapsed.TotalSeconds >= 30)
        {
            _usage.AddFocusMinutes(Math.Max(1, (int)Math.Round(elapsed.TotalMinutes)));
        }

        RemainingSeconds = 0;
        Ended?.Invoke(this, reason);
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void UpdateSettings()
    {
        if (!IsActive) return;
        _overlays.UpdateFocus(_settings.Settings.Focus);
    }

    private void Tick()
    {
        if (!IsActive) return;
        var remaining = (int)Math.Round((_endsAt - DateTime.Now).TotalSeconds);
        RemainingSeconds = Math.Max(0, remaining);
        StateChanged?.Invoke(this, EventArgs.Empty);
        if (RemainingSeconds <= 0)
        {
            Stop("finished");
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        if (IsActive) Stop("exit");
        _disposed = true;
        _timer.Stop();
    }
}
