using System.Diagnostics;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using MonitorMate.Core.Interop;
using MonitorMate.Core.Services;
using MonitorMate.Core.ViewModels;
using MonitorMate.Views;

namespace MonitorMate;

public partial class App : Application
{
    private const string MutexName = @"Local\MonitorMate.SingleInstance";
    private Mutex? _mutex;
    private AppServices? _services;
    private MainViewModel? _viewModel;
    private MainWindow? _window;
    private TrayService? _tray;
    private bool _disposed;

    public bool IsExiting { get; private set; }
    public AppServices Services => _services ?? throw new InvalidOperationException("Services are not ready.");

    protected override void OnStartup(StartupEventArgs e)
    {
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        base.OnStartup(e);

        _mutex = new Mutex(initiallyOwned: true, MutexName, out var createdNew);
        if (!createdNew)
        {
            var message = NativeMethods.RegisterWindowMessage("MonitorMate.Show");
            NativeMethods.PostMessage(new IntPtr(0xFFFF), message, IntPtr.Zero, IntPtr.Zero);
            Shutdown(0);
            return;
        }

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            try
            {
                _services?.Log.Error("Unhandled application exception.", args.ExceptionObject as Exception);
            }
            catch
            {
                // ignored
            }
        };

        try
        {
            _services = new AppServices();
            if (!AppEnvironment.IsPackaged && _services.Settings.Settings.StartWithWindows && _services.Startup.IsEnabled())
            {
                // Refresh an existing opt-in entry after moving or updating the app.
                _services.Startup.SetEnabled(true);
            }
            var showWindow = new Action(ShowMainWindow);
            var hideWindow = new Action(() => _window?.HideToTray());
            var requestExit = new Action(RequestExit);
            var confirm = new Func<string, string, string, bool>((title, message, label) =>
                _window is null || !_window.IsVisible
                    ? false
                    : ConfirmDialog.Show(_window, title, message, label));
            var requestRestart = new Action(Restart);

            _viewModel = new MainViewModel(_services, showWindow, hideWindow, requestExit, confirm, requestRestart);
            _window = new MainWindow(_viewModel);

            new WindowInteropHelper(_window).EnsureHandle();
            _services.Hotkeys.Initialize(_window);
            _services.Hotkeys.ToggleCare = _viewModel.TogglePause;
            _services.Hotkeys.BrightnessUp = () => _viewModel.AdjustBrightness(5);
            _services.Hotkeys.BrightnessDown = () => _viewModel.AdjustBrightness(-5);
            _services.Hotkeys.Warmer = () => _viewModel.AdjustTemperature(-250);
            _services.Hotkeys.Cooler = () => _viewModel.AdjustTemperature(250);
            _services.Hotkeys.TakeBreak = _viewModel.TakeBreak;
            _services.Hotkeys.ShowWindow = ShowMainWindow;
            _services.Hotkeys.ToggleFocus = _viewModel.ToggleFocus;
            _services.Hotkeys.Rebuild();

            _tray = new TrayService(_services.Log, ShowMainWindow, RequestExit, _viewModel.TogglePause,
                _viewModel.TakeBreak, _viewModel.ToggleFocus, () =>
                {
                    _viewModel.Navigate("settings");
                    ShowMainWindow();
                });
            _tray.Initialize(_window);
            _tray.SetPaused(_viewModel.IsPaused);
            _viewModel.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(MainViewModel.IsPaused))
                {
                    _tray.SetPaused(_viewModel.IsPaused);
                }
                else if (args.PropertyName == nameof(MainViewModel.TrayTooltip))
                {
                    _tray.UpdateTooltip(_viewModel.TrayTooltip);
                }
            };

            var launchToTray = e.Args.Any(a => a.Equals("--tray", StringComparison.OrdinalIgnoreCase));
            var firstRun = !_services.Settings.Settings.SetupCompleted;
            var show = !launchToTray && (!_services.Settings.Settings.StartMinimized || firstRun);
            if (firstRun)
            {
                show = true;
            }

            if (show)
            {
                _window.WindowState = WindowState.Normal;
                _window.Show();
                _window.ShowFromTray();
            }

            SessionEnding += (_, _) => RequestExit();
            _ = InitializeAsync();
        }
        catch (Exception ex)
        {
            try
            {
                _services?.Log.Error("Application startup failed.", ex);
            }
            catch
            {
                // ignored
            }

            MessageBox.Show($"MonitorMate could not start.{Environment.NewLine}{ex.Message}",
                "MonitorMate", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    private async Task InitializeAsync()
    {
        try
        {
            if (_viewModel is null) return;
            await _viewModel.InitializeAsync();
            _tray?.UpdateTooltip(_viewModel.TrayTooltip);
        }
        catch (Exception ex)
        {
            _services?.Log.Error("Background initialization failed.", ex);
        }
    }

    public void ShowMainWindow()
    {
        if (_window is null) return;
        _window.ShowFromTray();
    }

    public void NotifyHiddenToTray()
    {
        _tray?.UpdateTooltip("MonitorMate is still caring for your displays in the background.");
    }

    public void RequestExit()
    {
        if (IsExiting) return;
        IsExiting = true;
        try
        {
            _tray?.Dispose();
            _tray = null;
            _window?.Close();
            Shutdown(0);
        }
        catch
        {
            Shutdown(0);
        }
    }

    private void Restart()
    {
        try
        {
            IsExiting = true;
            _tray?.Dispose();
            _tray = null;
            _services?.Dispose();
            _window?.Close();
            _mutex?.ReleaseMutex();
            _mutex?.Dispose();
            _mutex = null;
            Process.Start(AppLaunch.CreateRestartStartInfo());
            Shutdown(0);
        }
        catch (Exception ex)
        {
            _services?.Log.Error("Restart failed.", ex);
            IsExiting = false;
        }
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        try
        {
            _services?.Log.Error("A UI exception was caught.", e.Exception);
        }
        catch
        {
            // ignored
        }

        e.Handled = true;
        _viewModel?.DismissErrorCommand.Execute(null);
        _services?.Overlays.ShowToast(new ToastRequest(
            "MonitorMate recovered",
            "Something unexpected happened in the interface. Your display settings are still safe."));
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_disposed)
        {
            base.OnExit(e);
            return;
        }

        _disposed = true;
        try
        {
            _tray?.Dispose();
        }
        catch
        {
            // ignored
        }

        try
        {
            _viewModel?.Dispose();
        }
        catch
        {
            // ignored
        }

        try
        {
            _services?.Dispose();
        }
        catch
        {
            // ignored
        }

        try
        {
            _mutex?.ReleaseMutex();
        }
        catch
        {
            // ignored
        }

        _mutex?.Dispose();
        _mutex = null;
        base.OnExit(e);
    }
}
