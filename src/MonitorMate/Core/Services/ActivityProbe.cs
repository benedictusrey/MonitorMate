using MonitorMate.Core.Interop;

namespace MonitorMate.Core.Services;

public static class ActivityProbe
{
    public static TimeSpan GetIdleTime()
    {
        var info = new NativeMethods.LASTINPUTINFO
        {
            cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.LASTINPUTINFO>()
        };
        if (!NativeMethods.GetLastInputInfo(ref info))
        {
            return TimeSpan.Zero;
        }

        var elapsed = unchecked((uint)Environment.TickCount - info.dwTime);
        return TimeSpan.FromMilliseconds(elapsed);
    }

    public static bool IsFullscreenOrPresenting()
    {
        try
        {
            var result = NativeMethods.SHQueryUserNotificationState(out var state);
            if (result != 0)
            {
                return false;
            }

            return state is NativeMethods.QUNS_RUNNING_D3D_FULL_SCREEN
                or NativeMethods.QUNS_PRESENTATION_MODE;
        }
        catch
        {
            return false;
        }
    }

    public static bool IsUserActive(TimeSpan idleThreshold) => GetIdleTime() < idleThreshold;
}
