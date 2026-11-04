using System.Drawing;
using System.Windows.Forms;
using Window = System.Windows.Window;

namespace MonitorMate.Core.Services;

/// <summary>
/// Uses the platform NotifyIcon rather than a hand-built shell struct. It owns
/// a proper hidden window and is the reliable path for the Windows tray.
/// </summary>
public sealed class TrayService : IDisposable
{
    private readonly LogService _log;
    private readonly Action _showWindow;
    private readonly Action _exit;
    private readonly Action _togglePause;
    private readonly Action _takeBreak;
    private readonly Action _startFocus;
    private readonly Action _openSettings;
    private readonly NotifyIcon _notifyIcon;
    private readonly ContextMenuStrip _menu;
    private readonly ToolStripMenuItem _pauseItem;
    private readonly Icon? _ownedIcon;
    private bool _paused;
    private bool _disposed;

    public TrayService(LogService log, Action showWindow, Action exit, Action togglePause, Action takeBreak,
        Action startFocus, Action openSettings)
    {
        _log = log;
        _showWindow = showWindow;
        _exit = exit;
        _togglePause = togglePause;
        _takeBreak = takeBreak;
        _startFocus = startFocus;
        _openSettings = openSettings;

        _pauseItem = new ToolStripMenuItem("Pause care");
        _pauseItem.Click += (_, _) => _togglePause();
        var open = new ToolStripMenuItem("Open MonitorMate") { Font = new Font(SystemFonts.MenuFont ?? SystemFonts.DefaultFont, FontStyle.Bold) };
        open.Click += (_, _) => _showWindow();
        var breakItem = new ToolStripMenuItem("Take a break");
        breakItem.Click += (_, _) => _takeBreak();
        var focus = new ToolStripMenuItem("Start a focus session");
        focus.Click += (_, _) => _startFocus();
        var settings = new ToolStripMenuItem("Settings");
        settings.Click += (_, _) => _openSettings();
        var exitItem = new ToolStripMenuItem("Exit MonitorMate");
        exitItem.Click += (_, _) => _exit();

        _menu = new ContextMenuStrip
        {
            ShowImageMargin = false,
            Renderer = new ToolStripProfessionalRenderer(new DarkMenuColors()),
            BackColor = Color.FromArgb(20, 30, 39),
            ForeColor = Color.FromArgb(242, 246, 248),
            Font = SystemFonts.MenuFont ?? SystemFonts.DefaultFont
        };
        _menu.Items.Add(open);
        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add(_pauseItem);
        _menu.Items.Add(breakItem);
        _menu.Items.Add(focus);
        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add(settings);
        _menu.Items.Add(exitItem);
        _menu.Opening += (_, _) => _pauseItem.Text = _paused ? "Resume care" : "Pause care";

        Icon? icon = null;
        try
        {
            if (AppLaunch.IconPath is { } iconPath)
            {
                icon = new Icon(iconPath);
            }
            else if (Environment.ProcessPath is { } processPath)
            {
                icon = Icon.ExtractAssociatedIcon(processPath);
            }
        }
        catch (Exception ex)
        {
            _log.Warn($"Could not read the application icon: {ex.Message}");
        }

        _ownedIcon = icon;
        _notifyIcon = new NotifyIcon
        {
            Icon = icon ?? SystemIcons.Application,
            Text = "MonitorMate",
            ContextMenuStrip = _menu,
            Visible = true
        };
        _notifyIcon.MouseUp += (_, e) =>
        {
            if (e.Button == MouseButtons.Left)
            {
                _showWindow();
            }
        };
    }

    public void Initialize(Window window)
    {
        // The NotifyIcon owns its own message window. Kept for composition
        // symmetry with the rest of the app.
        _ = window;
    }

    public void SetPaused(bool paused) => _paused = paused;

    public void UpdateTooltip(string text)
    {
        if (_disposed) return;
        _notifyIcon.Text = text.Length > 63 ? text[..63] : text;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _menu.Dispose();
        _ownedIcon?.Dispose();
    }

    private sealed class DarkMenuColors : ProfessionalColorTable
    {
        private static readonly Color Background = Color.FromArgb(20, 30, 39);
        private static readonly Color Hover = Color.FromArgb(30, 43, 53);
        private static readonly Color Border = Color.FromArgb(52, 72, 84);

        public override Color ToolStripDropDownBackground => Background;
        public override Color ImageMarginGradientBegin => Background;
        public override Color ImageMarginGradientMiddle => Background;
        public override Color ImageMarginGradientEnd => Background;
        public override Color MenuItemSelected => Hover;
        public override Color MenuItemBorder => Border;
        public override Color MenuBorder => Border;
        public override Color SeparatorDark => Border;
        public override Color SeparatorLight => Border;
    }
}
