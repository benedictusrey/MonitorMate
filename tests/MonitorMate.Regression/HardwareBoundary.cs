using MonitorMate.Core.Models;
using MonitorMate.Core.ViewModels.Overlays;

namespace MonitorMate.Core.Interop
{
    // Hardware boundary double: production policy, composition and persistence remain real.
    public static class NativeMethods
    {
        public const int WM_HOTKEY = 0x0312;
        public static ushort[] Ramp = Enumerable.Range(0, 768).Select(i => (ushort)((i % 256) * 257)).ToArray();
        public static int Writes;
        public static HashSet<int> Keys = new();
        public static IntPtr CreateDC(string driver, string device, string? output, IntPtr init) => new(1);
        public static bool DeleteDC(IntPtr dc) => true;
        public static bool GetDeviceGammaRamp(IntPtr dc, ushort[] ramp) { Ramp.CopyTo(ramp, 0); return true; }
        public static bool SetDeviceGammaRamp(IntPtr dc, ushort[] ramp) { Writes++; Ramp = ramp.ToArray(); return true; }
        public static bool RegisterHotKey(IntPtr hwnd, int id, uint modifiers, uint key) => Keys.Add(id);
        public static bool UnregisterHotKey(IntPtr hwnd, int id) => Keys.Remove(id);
        public static bool LockWorkStation() => true;
    }
}

namespace MonitorMate.Core.Services
{
    public sealed record HardwareApplyResult(bool Supported, bool Confirmed, int ActualPercent, string? Error = null);
    public sealed class DisplayService
    {
        public List<DisplayInfo> Infos = new();
        public bool RejectWrites;
        public int BrightnessCalls;
        public int RestoreCalls;
        public IReadOnlyList<DisplayInfo> Scan() => Infos;
        public Task<HardwareApplyResult> SetBrightnessAsync(DisplayInfo info, int percent)
        {
            BrightnessCalls++;
            return Task.FromResult(new HardwareApplyResult(true, !RejectWrites, percent, RejectWrites ? "Monitor rejected write" : null));
        }
        public Task<HardwareApplyResult> SetContrastAsync(DisplayInfo info, int percent) => Task.FromResult(new HardwareApplyResult(true, true, percent));
        public Task<HardwareApplyResult> RestoreOriginalBrightnessAsync(DisplayInfo info)
        {
            RestoreCalls++;
            return Task.FromResult(new HardwareApplyResult(true, true, 70));
        }
    }
    public sealed record ToastRequest(string Title, string Message, string? PrimaryLabel = null, Action? PrimaryAction = null, string? SecondaryLabel = null, Action? SecondaryAction = null);
    public sealed class OverlayService
    {
        public Action? Skip;
        public void CloseToast() { }
        public void ShowToast(ToastRequest request) { }
        public void ShowGentleBreak(BreakOverlayViewModel vm, Action skip) => Skip = skip;
        public void ShowBreak(BreakOverlayViewModel vm, Action skip) => Skip = skip;
        public void CloseBreak() { }
        public void ShowIdentify() { }
    }
    public sealed class AppServices
    {
        public required ComfortService Comfort { get; init; }
        public required DisplayService Displays { get; init; }
        public required SettingsService Settings { get; init; }
        public required LogService Log { get; init; }
        public OverlayService Overlays { get; } = new();
    }
    public static class ActivityProbe
    {
        public static TimeSpan GetIdleTime() => TimeSpan.Zero;
        public static bool IsFullscreenOrPresenting() => false;
    }
}
