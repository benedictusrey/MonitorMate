using System.Windows;
using System.Windows.Interop;
using MonitorMate.Core.Interop;
using MonitorMate.Core.Models;

namespace MonitorMate.Core.Services;

public sealed class HotkeyService : IDisposable
{
    private const int IdToggleCare = 2001;
    private const int IdBrightnessUp = 2002;
    private const int IdBrightnessDown = 2003;
    private const int IdWarmer = 2004;
    private const int IdCooler = 2005;
    private const int IdTakeBreak = 2006;
    private const int IdShowWindow = 2007;
    private const int IdToggleFocus = 2008;
    private const int IdEscape = 2901;
    private const uint VkEscape = 0x1B;

    private readonly SettingsService _settings;
    private readonly LogService _log;
    private readonly Dictionary<int, Action> _actions = new();
    private readonly Dictionary<int, string> _registered = new();
    private HwndSource? _source;
    private IntPtr _handle;
    private bool _initialized;
    private bool _disposed;

    public HotkeyService(SettingsService settings, LogService log)
    {
        _settings = settings;
        _log = log;
    }

    public IReadOnlyList<string> Conflicts { get; private set; } = Array.Empty<string>();

    public event EventHandler? ConflictsChanged;

    public Action? ToggleCare { get; set; }
    public Action? BrightnessUp { get; set; }
    public Action? BrightnessDown { get; set; }
    public Action? Warmer { get; set; }
    public Action? Cooler { get; set; }
    public Action? TakeBreak { get; set; }
    public Action? ShowWindow { get; set; }
    public Action? ToggleFocus { get; set; }

    public void Initialize(Window window)
    {
        if (_initialized) return;
        _handle = new WindowInteropHelper(window).Handle;
        _source = HwndSource.FromHwnd(_handle);
        _source?.AddHook(WndProc);
        _initialized = true;
        Rebuild();
    }

    public void Rebuild()
    {
        if (!_initialized || _disposed) return;
        UnregisterAll(preserveEscape: true);
        Conflicts = Array.Empty<string>();
        var config = _settings.Settings.Hotkeys;
        if (!config.Enabled)
        {
            ConflictsChanged?.Invoke(this, EventArgs.Empty);
            return;
        }

        var conflicts = new List<string>();
        Register(IdToggleCare, config.ToggleCare, () => ToggleCare, "Toggle care", conflicts);
        Register(IdBrightnessUp, config.BrightnessUp, () => BrightnessUp, "Brightness up", conflicts);
        Register(IdBrightnessDown, config.BrightnessDown, () => BrightnessDown, "Brightness down", conflicts);
        Register(IdWarmer, config.Warmer, () => Warmer, "Warmer", conflicts);
        Register(IdCooler, config.Cooler, () => Cooler, "Cooler", conflicts);
        Register(IdTakeBreak, config.TakeBreak, () => TakeBreak, "Take a break", conflicts);
        Register(IdShowWindow, config.ShowWindow, () => ShowWindow, "Show MonitorMate", conflicts);
        Register(IdToggleFocus, config.ToggleFocus, () => ToggleFocus, "Toggle focus", conflicts);

        Conflicts = conflicts;
        ConflictsChanged?.Invoke(this, EventArgs.Empty);
    }

    private void Register(int id, string? gestureText, Func<Action?> actionProvider, string label, List<string> conflicts)
    {
        var gesture = HotkeyGesture.Parse(gestureText);
        var action = actionProvider();
        if (gesture is null || action is null)
        {
            return;
        }

        if (NativeMethods.RegisterHotKey(_handle, id, gesture.Modifiers, gesture.VirtualKey))
        {
            _actions[id] = action;
            _registered[id] = gesture.Display;
        }
        else
        {
            conflicts.Add($"{label} ({gesture.Display}) is already used by another app.");
        }
    }

    public bool SetEscapeCapture(bool active, Action? action = null)
    {
        if (!_initialized || _disposed) return false;
        if (active)
        {
            if (_registered.ContainsKey(IdEscape))
            {
                return true;
            }

            if (NativeMethods.RegisterHotKey(_handle, IdEscape, 0, VkEscape))
            {
                _registered[IdEscape] = "Esc";
                if (action is not null)
                {
                    _actions[IdEscape] = action;
                }

                return true;
            }

            return false;
        }

        if (_registered.Remove(IdEscape))
        {
            NativeMethods.UnregisterHotKey(_handle, IdEscape);
        }

        _actions.Remove(IdEscape);
        return true;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WM_HOTKEY)
        {
            var id = wParam.ToInt32();
            if (_actions.TryGetValue(id, out var action))
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    _log.Error("A global shortcut failed.", ex);
                }

                handled = true;
            }
        }

        return IntPtr.Zero;
    }

    private void UnregisterAll(bool preserveEscape = false)
    {
        foreach (var id in _registered.Keys.Where(id => !preserveEscape || id != IdEscape).ToList())
        {
            NativeMethods.UnregisterHotKey(_handle, id);
            _registered.Remove(id);
            _actions.Remove(id);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        UnregisterAll();
        _source?.RemoveHook(WndProc);
        _source = null;
    }
}
