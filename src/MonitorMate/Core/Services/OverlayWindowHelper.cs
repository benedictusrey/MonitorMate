using System.Windows;
using System.Windows.Interop;
using MonitorMate.Core.Interop;
using MonitorMate.Core.Models;

namespace MonitorMate.Core.Services;

public static class OverlayWindowHelper
{
    public static void ConfigureFullscreen(Window window, PixelRect bounds, bool clickThrough, bool topmost = true)
    {
        window.WindowStyle = WindowStyle.None;
        window.ResizeMode = ResizeMode.NoResize;
        window.AllowsTransparency = true;
        window.Background = System.Windows.Media.Brushes.Transparent;
        window.ShowInTaskbar = false;
        window.WindowStartupLocation = WindowStartupLocation.Manual;
        window.Topmost = topmost;
        window.Left = -10000;
        window.Top = -10000;
        window.Width = Math.Max(1, bounds.Width);
        window.Height = Math.Max(1, bounds.Height);

        void Apply()
        {
            var handle = new WindowInteropHelper(window).Handle;
            if (handle == IntPtr.Zero)
            {
                return;
            }

            if (clickThrough)
            {
                var style = NativeMethods.GetWindowLongPtr(handle, NativeMethods.GWL_EXSTYLE).ToInt64();
                style |= NativeMethods.WS_EX_TRANSPARENT | NativeMethods.WS_EX_NOACTIVATE | NativeMethods.WS_EX_TOOLWINDOW;
                NativeMethods.SetWindowLongPtr(handle, NativeMethods.GWL_EXSTYLE, new IntPtr(style));
            }

            NativeMethods.SetWindowPos(handle, topmost ? NativeMethods.HWND_TOPMOST : IntPtr.Zero,
                bounds.X, bounds.Y, Math.Max(1, bounds.Width), Math.Max(1, bounds.Height),
                NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_SHOWWINDOW | NativeMethods.SWP_FRAMECHANGED);
        }

        window.SourceInitialized += (_, _) => Apply();
        window.Loaded += (_, _) => Apply();
        window.DpiChanged += (_, _) => Apply();
    }

    public static void ConfigureToast(Window window)
    {
        window.WindowStyle = WindowStyle.None;
        window.ResizeMode = ResizeMode.NoResize;
        window.AllowsTransparency = true;
        window.Background = System.Windows.Media.Brushes.Transparent;
        window.ShowInTaskbar = false;
        window.Topmost = true;
        window.ShowActivated = false;
        window.WindowStartupLocation = WindowStartupLocation.Manual;
    }

    public static void ApplyDarkTitleBar(Window window)
    {
        var handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero) return;
        var enabled = 1;
        NativeMethods.DwmSetWindowAttribute(handle, NativeMethods.DWMWA_USE_IMMERSIVE_DARK_MODE, ref enabled, sizeof(int));
        var corner = NativeMethods.DWMWCP_ROUND;
        NativeMethods.DwmSetWindowAttribute(handle, NativeMethods.DWMWA_WINDOW_CORNER_PREFERENCE, ref corner, sizeof(int));
    }
}
