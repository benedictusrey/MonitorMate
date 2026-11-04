using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using MonitorMate.Core.Interop;
using MonitorMate.Core.Models;

namespace MonitorMate.Core.Services;

public sealed record HardwareApplyResult(bool Supported, bool Confirmed, int ActualPercent, string? Error = null);

public sealed class DisplayService : IDisposable
{
    private const byte VcpBrightness = 0x10;
    private const byte VcpContrast = 0x12;
    private readonly LogService _log;
    private readonly object _gate = new();
    private readonly List<PhysicalMonitorLease> _leases = new();
    private IReadOnlyList<DisplayInfo> _displays = Array.Empty<DisplayInfo>();
    private bool _disposed;

    public DisplayService(LogService log) => _log = log;

    public IReadOnlyList<DisplayInfo> Displays
    {
        get
        {
            lock (_gate)
            {
                return _displays;
            }
        }
    }

    public IReadOnlyList<DisplayInfo> Scan()
    {
        lock (_gate)
        {
            if (_disposed)
            {
                return Array.Empty<DisplayInfo>();
            }

            DisposeLeases();
            var monitors = new List<IntPtr>();
            NativeMethods.MonitorEnumProc callback = (hMonitor, _, _, _) =>
            {
                monitors.Add(hMonitor);
                return true;
            };

            if (!NativeMethods.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, callback, IntPtr.Zero))
            {
                _log.Warn("EnumDisplayMonitors returned no monitor set.");
            }

            var raw = new List<(IntPtr Handle, NativeMethods.MONITORINFOEX Info)>();
            foreach (var handle in monitors)
            {
                var info = new NativeMethods.MONITORINFOEX
                {
                    cbSize = Marshal.SizeOf<NativeMethods.MONITORINFOEX>(),
                    szDevice = string.Empty
                };
                if (NativeMethods.GetMonitorInfo(handle, ref info))
                {
                    raw.Add((handle, info));
                }
            }

            raw.Sort((a, b) =>
            {
                var x = a.Info.rcMonitor.Left.CompareTo(b.Info.rcMonitor.Left);
                return x != 0 ? x : a.Info.rcMonitor.Top.CompareTo(b.Info.rcMonitor.Top);
            });

            var adapterNames = GetAdapterNames();
            var result = new List<DisplayInfo>(raw.Count);
            var number = 1;
            foreach (var (handle, info) in raw)
            {
                var monitorDevice = new NativeMethods.DISPLAY_DEVICE { cb = Marshal.SizeOf<NativeMethods.DISPLAY_DEVICE>() };
                var gotMonitor = NativeMethods.EnumDisplayDevices(info.szDevice, 0, ref monitorDevice, 0);
                var monitorId = gotMonitor ? monitorDevice.DeviceID : string.Empty;
                var monitorName = gotMonitor ? monitorDevice.DeviceString : string.Empty;
                var adapter = adapterNames.TryGetValue(info.szDevice, out var a) ? a : "Display adapter";
                if (string.IsNullOrWhiteSpace(monitorName) || monitorName.Equals("Generic PnP Monitor", StringComparison.OrdinalIgnoreCase))
                {
                    monitorName = $"Display {number}";
                }

                var idSource = !string.IsNullOrWhiteSpace(monitorId)
                    ? monitorId + "|" + info.szDevice
                    : info.szDevice + "|" + info.rcMonitor.Left + "," + info.rcMonitor.Top + "|" + info.rcMonitor.Width + "x" + info.rcMonitor.Height;
                var id = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(idSource)))[..12].ToLowerInvariant();

                var dpi = GetDpi(handle);
                var gammaSupported = ProbeGamma(info.szDevice, out var gammaWarning);
                var ddc = ProbeDdc(handle, out var physicalHandle, out var lease, out var brightnessSupported,
                    out var contrastSupported, out var brightnessMax, out var contrastMax, out var originalBrightness,
                    out var originalContrast);
                if (lease is not null)
                {
                    _leases.Add(lease);
                }

                result.Add(new DisplayInfo
                {
                    Id = id,
                    DeviceName = info.szDevice,
                    Name = monitorName,
                    AdapterName = adapter,
                    Bounds = ToRect(info.rcMonitor),
                    WorkArea = ToRect(info.rcWork),
                    IsPrimary = (info.dwFlags & 1) != 0,
                    Dpi = dpi,
                    MonitorNumber = number,
                    HardwareBrightnessSupported = brightnessSupported,
                    HardwareContrastSupported = contrastSupported,
                    HardwareBrightnessMin = 0,
                    HardwareBrightnessMax = brightnessMax,
                    HardwareContrastMax = contrastMax,
                    OriginalBrightness = originalBrightness,
                    OriginalContrast = originalContrast,
                    GammaSupported = gammaSupported,
                    GammaWarning = gammaWarning,
                    PhysicalMonitorHandle = physicalHandle,
                    MonitorHandle = handle
                });
                number++;
            }

            _displays = result;
            return result;
        }
    }

    public async Task<HardwareApplyResult> SetBrightnessAsync(DisplayInfo info, int percent, CancellationToken cancellationToken = default)
    {
        if (!info.HardwareBrightnessSupported || info.PhysicalMonitorHandle == IntPtr.Zero)
        {
            return new HardwareApplyResult(false, false, percent, "Hardware brightness is not available on this display.");
        }

        return await Task.Run(() =>
        {
            lock (_gate)
            {
                if (_disposed || !IsCurrent(info))
                {
                    return new HardwareApplyResult(false, false, percent, "The display list changed. Refresh and try again.");
                }

                try
                {
                    var target = MapPercent(percent, info.HardwareBrightnessMax);
                    var wrote = NativeMethods.SetVCPFeature(info.PhysicalMonitorHandle, VcpBrightness, (uint)target);
                    if (!wrote)
                    {
                        return new HardwareApplyResult(true, false, percent, "The monitor did not accept the brightness change.");
                    }

                    Thread.Sleep(60);
                    if (NativeMethods.GetVCPFeatureAndVCPFeatureReply(info.PhysicalMonitorHandle, VcpBrightness,
                            out _, out var current, out var max) && max > 0)
                    {
                        var actual = (int)Math.Round(current * 100.0 / max);
                        return new HardwareApplyResult(true, true, actual);
                    }

                    return new HardwareApplyResult(true, false, percent, "Brightness was sent, but the monitor did not report a value.");
                }
                catch (Exception ex)
                {
                    _log.Warn($"Hardware brightness failed for {info.Name}: {ex.Message}");
                    return new HardwareApplyResult(true, false, percent, "The monitor connection did not complete the brightness request.");
                }
            }
        }, cancellationToken);
    }

    public async Task<HardwareApplyResult> SetContrastAsync(DisplayInfo info, int percent, CancellationToken cancellationToken = default)
    {
        if (!info.HardwareContrastSupported || info.PhysicalMonitorHandle == IntPtr.Zero)
        {
            return new HardwareApplyResult(false, false, percent, "Hardware contrast is not available on this display.");
        }

        return await Task.Run(() =>
        {
            lock (_gate)
            {
                if (_disposed || !IsCurrent(info))
                {
                    return new HardwareApplyResult(false, false, percent, "The display list changed. Refresh and try again.");
                }

                try
                {
                    var target = MapPercent(percent, info.HardwareContrastMax);
                    var wrote = NativeMethods.SetVCPFeature(info.PhysicalMonitorHandle, VcpContrast, (uint)target);
                    if (!wrote)
                    {
                        return new HardwareApplyResult(true, false, percent, "The monitor did not accept the contrast change.");
                    }

                    Thread.Sleep(60);
                    if (NativeMethods.GetVCPFeatureAndVCPFeatureReply(info.PhysicalMonitorHandle, VcpContrast,
                            out _, out var current, out var max) && max > 0)
                    {
                        return new HardwareApplyResult(true, true, (int)Math.Round(current * 100.0 / max));
                    }

                    return new HardwareApplyResult(true, false, percent, "Contrast was sent, but the monitor did not report a value.");
                }
                catch (Exception ex)
                {
                    _log.Warn($"Hardware contrast failed for {info.Name}: {ex.Message}");
                    return new HardwareApplyResult(true, false, percent, "The monitor connection did not complete the contrast request.");
                }
            }
        }, cancellationToken);
    }

    public async Task<HardwareApplyResult> RestoreOriginalBrightnessAsync(DisplayInfo info, CancellationToken cancellationToken = default)
    {
        if (!info.HardwareBrightnessSupported || info.HardwareBrightnessMax == 0)
        {
            return new HardwareApplyResult(false, false, 100, "This display has no hardware brightness control.");
        }

        var percent = (int)Math.Round(info.OriginalBrightness * 100.0 / info.HardwareBrightnessMax);
        return await SetBrightnessAsync(info, percent, cancellationToken);
    }

    private bool IsCurrent(DisplayInfo info)
        => _displays.Any(d => d.Id == info.Id && d.PhysicalMonitorHandle == info.PhysicalMonitorHandle);

    private static int MapPercent(int percent, uint max)
    {
        percent = Math.Clamp(percent, 0, 100);
        return (int)Math.Round(percent / 100.0 * max);
    }

    private static bool ProbeGamma(string deviceName, out string? warning)
    {
        warning = null;
        var dc = NativeMethods.CreateDC("DISPLAY", deviceName, null, IntPtr.Zero);
        if (dc == IntPtr.Zero)
        {
            warning = "Windows did not expose a gamma channel for this display.";
            return false;
        }

        try
        {
            var ramp = new ushort[768];
            if (!NativeMethods.GetDeviceGammaRamp(dc, ramp))
            {
                warning = "The display driver does not expose a readable gamma ramp.";
                return false;
            }

            return true;
        }
        catch (Exception)
        {
            warning = "The gamma interface is unavailable.";
            return false;
        }
        finally
        {
            NativeMethods.DeleteDC(dc);
        }
    }

    private bool ProbeDdc(IntPtr monitor, out IntPtr physicalHandle, out PhysicalMonitorLease? lease,
        out bool brightnessSupported, out bool contrastSupported, out uint brightnessMax,
        out uint contrastMax, out uint originalBrightness, out uint originalContrast)
    {
        physicalHandle = IntPtr.Zero;
        lease = null;
        brightnessSupported = false;
        contrastSupported = false;
        brightnessMax = 0;
        contrastMax = 0;
        originalBrightness = 0;
        originalContrast = 0;

        try
        {
            if (!NativeMethods.GetNumberOfPhysicalMonitorsFromHMONITOR(monitor, out var count) || count == 0)
            {
                return false;
            }

            var monitors = new NativeMethods.PHYSICAL_MONITOR[count];
            if (!NativeMethods.GetPhysicalMonitorsFromHMONITOR(monitor, count, monitors))
            {
                return false;
            }

            lease = new PhysicalMonitorLease(count, monitors);
            physicalHandle = monitors[0].hPhysicalMonitor;
            if (physicalHandle == IntPtr.Zero)
            {
                return false;
            }

            if (NativeMethods.GetVCPFeatureAndVCPFeatureReply(physicalHandle, VcpBrightness, out _,
                    out var current, out var max) && max > 0)
            {
                brightnessSupported = true;
                brightnessMax = max;
                originalBrightness = Math.Min(current, max);
            }

            if (NativeMethods.GetVCPFeatureAndVCPFeatureReply(physicalHandle, VcpContrast, out _,
                    out var contrastCurrent, out var cmax) && cmax > 0)
            {
                contrastSupported = true;
                contrastMax = cmax;
                originalContrast = Math.Min(contrastCurrent, cmax);
            }

            return brightnessSupported || contrastSupported;
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException or BadImageFormatException)
        {
            _log.Warn("DDC/CI is unavailable on this Windows build; software control will be used.");
            return false;
        }
        catch (Exception ex)
        {
            _log.Warn($"DDC/CI probe failed: {ex.Message}");
            return false;
        }
    }

    private static Dictionary<string, string> GetAdapterNames()
    {
        var names = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (uint i = 0; i < 16; i++)
        {
            var device = new NativeMethods.DISPLAY_DEVICE { cb = Marshal.SizeOf<NativeMethods.DISPLAY_DEVICE>() };
            if (!NativeMethods.EnumDisplayDevices(null, i, ref device, 0))
            {
                break;
            }

            if (!string.IsNullOrWhiteSpace(device.DeviceString))
            {
                names[device.DeviceName] = device.DeviceString;
            }
        }

        return names;
    }

    private static uint GetDpi(IntPtr monitor)
    {
        try
        {
            if (NativeMethods.GetDpiForMonitor(monitor, NativeMethods.MDT_EFFECTIVE_DPI, out var dpiX, out _) == 0 && dpiX > 0)
            {
                return dpiX;
            }
        }
        catch
        {
            // Shcore is present on supported Windows versions, but never let a
            // probe failure prevent the display from appearing.
        }

        return 96;
    }

    private static PixelRect ToRect(NativeMethods.RECT rect)
        => new() { X = rect.Left, Y = rect.Top, Width = rect.Width, Height = rect.Height };

    private void DisposeLeases()
    {
        foreach (var lease in _leases)
        {
            lease.Dispose();
        }

        _leases.Clear();
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed) return;
            DisposeLeases();
            _displays = Array.Empty<DisplayInfo>();
            _disposed = true;
        }
    }

    private sealed class PhysicalMonitorLease : IDisposable
    {
        private readonly uint _count;
        private readonly NativeMethods.PHYSICAL_MONITOR[] _monitors;

        public PhysicalMonitorLease(uint count, NativeMethods.PHYSICAL_MONITOR[] monitors)
        {
            _count = count;
            _monitors = monitors;
        }

        public void Dispose()
        {
            try
            {
                NativeMethods.DestroyPhysicalMonitors(_count, _monitors);
            }
            catch
            {
                // ignored
            }
        }
    }
}
