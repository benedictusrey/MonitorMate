using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using MonitorMate.Core.Interop;
using MonitorMate.Core.Models;

namespace MonitorMate.Core.Services;

public sealed record GammaApplyResult(bool Success, bool Unconfirmed, string? Message = null)
{
    public static GammaApplyResult Ok() => new(true, false);
    public static GammaApplyResult Unverified(string message) => new(true, true, message);
    public static GammaApplyResult Failed(string message) => new(false, false, message);
}

/// <summary>
/// Owns the GPU gamma ramp. A baseline ramp is captured once per display and
/// every comfort adjustment is composed from it, never from a previous tint.
/// </summary>
public sealed class GammaService : IDisposable
{
    private readonly LogService _log;
    private readonly object _gate = new();
    private readonly Dictionary<string, GammaBaselineRecord> _records;
    private readonly JsonSerializerOptions _json;
    private DateTime _lastSaveUtc = DateTime.MinValue;
    private bool _disposed;

    public GammaService(LogService log)
    {
        _log = log;
        AppPaths.EnsureCreated();
        _json = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };
        _records = Load().Displays;
    }

    public bool HasBaseline(string displayId)
    {
        lock (_gate)
        {
            return _records.TryGetValue(displayId, out var record) && IsValidRamp(record.Ramp);
        }
    }

    public GammaApplyResult Apply(DisplayInfo info, int brightness, int temperatureK, int softwareDim)
    {
        lock (_gate)
        {
            if (_disposed)
            {
                return GammaApplyResult.Failed("The display controller is shutting down.");
            }

            var dc = NativeMethods.CreateDC("DISPLAY", info.DeviceName, null, IntPtr.Zero);
            if (dc == IntPtr.Zero)
            {
                return GammaApplyResult.Failed("Windows did not expose a gamma channel for this display.");
            }

            try
            {
                var current = ReadRamp(dc);
                if (current is null)
                {
                    return GammaApplyResult.Failed("This display driver does not expose a readable gamma ramp.");
                }

                var record = EnsureBaseline(info, current);
                if (record is null)
                {
                    return GammaApplyResult.Failed("A safe colour baseline could not be established.");
                }

                // A ramp we previously wrote may have survived a crash. Put the
                // baseline back before composing this request.
                var currentSignature = Signature(current);
                if (!string.IsNullOrEmpty(record.AppliedSignature) && currentSignature == record.AppliedSignature)
                {
                    WriteRamp(dc, record.Ramp);
                    current = ReadRamp(dc) ?? current;
                }

                var scale = ComputeScale(info.HardwareBrightnessSupported, brightness, softwareDim);
                var rgb = KelvinMultipliers(temperatureK);
                var target = Compose(record.Ramp, scale, rgb);
                var targetSignature = Signature(target);
                var effectiveCurrent = ReadRamp(dc) ?? current;
                var currentNow = Signature(effectiveCurrent);

                if (currentNow == targetSignature)
                {
                    record.AppliedSignature = targetSignature;
                    record.AppliedUtc = DateTime.UtcNow;
                    SaveIfDue(force: false);
                    return GammaApplyResult.Ok();
                }

                var wrote = WriteRamp(dc, target);
                var verified = ReadRamp(dc);
                var verifiedSignature = verified is null ? string.Empty : Signature(verified);
                var unconfirmed = !wrote || verified is null || verifiedSignature != targetSignature;
                if (!wrote)
                {
                    return GammaApplyResult.Failed("Windows rejected the colour adjustment.");
                }

                record.AppliedSignature = targetSignature;
                record.AppliedUtc = DateTime.UtcNow;
                SaveIfDue(force: false);
                return unconfirmed
                    ? GammaApplyResult.Unverified("Windows accepted the request, but the driver did not report the new ramp.")
                    : GammaApplyResult.Ok();
            }
            catch (Exception ex)
            {
                _log.Error($"Gamma adjustment failed for {info.Name}.", ex);
                return GammaApplyResult.Failed("The display driver rejected the colour adjustment.");
            }
            finally
            {
                NativeMethods.DeleteDC(dc);
            }
        }
    }

    public bool Restore(DisplayInfo info)
    {
        lock (_gate)
        {
            if (!_records.TryGetValue(info.Id, out var record) || !IsValidRamp(record.Ramp))
            {
                return false;
            }

            var dc = NativeMethods.CreateDC("DISPLAY", info.DeviceName, null, IntPtr.Zero);
            if (dc == IntPtr.Zero)
            {
                return false;
            }

            try
            {
                var wrote = WriteRamp(dc, record.Ramp);
                if (wrote)
                {
                    record.AppliedSignature = string.Empty;
                    record.AppliedUtc = null;
                    SaveIfDue(force: true);
                }

                return wrote;
            }
            finally
            {
                NativeMethods.DeleteDC(dc);
            }
        }
    }

    public void RestoreAll()
    {
        lock (_gate)
        {
            foreach (var record in _records.Values.ToList())
            {
                if (!IsValidRamp(record.Ramp) || string.IsNullOrWhiteSpace(record.DeviceName) || string.IsNullOrWhiteSpace(record.AppliedSignature))
                {
                    continue;
                }

                var dc = NativeMethods.CreateDC("DISPLAY", record.DeviceName, null, IntPtr.Zero);
                if (dc == IntPtr.Zero)
                {
                    continue;
                }

                try
                {
                    if (WriteRamp(dc, record.Ramp))
                    {
                        record.AppliedSignature = string.Empty;
                        record.AppliedUtc = null;
                    }
                }
                catch (Exception ex)
                {
                    _log.Warn($"Could not restore gamma for {record.DeviceName}: {ex.Message}");
                }
                finally
                {
                    NativeMethods.DeleteDC(dc);
                }
            }

            Save(force: true);
        }
    }

    public void RecaptureBaseline(DisplayInfo info)
    {
        lock (_gate)
        {
            var dc = NativeMethods.CreateDC("DISPLAY", info.DeviceName, null, IntPtr.Zero);
            if (dc == IntPtr.Zero) return;
            try
            {
                var ramp = ReadRamp(dc);
                if (ramp is null) return;
                if (_records.TryGetValue(info.Id, out var previous) &&
                    IsValidRamp(previous.Ramp) &&
                    !string.IsNullOrEmpty(previous.AppliedSignature) &&
                    Signature(ramp) == previous.AppliedSignature)
                {
                    if (!WriteRamp(dc, previous.Ramp))
                        throw new InvalidOperationException("Restore original colour before recapturing the baseline.");
                    ramp = ReadRamp(dc);
                    if (ramp is null || Signature(ramp) != Signature(previous.Ramp))
                        throw new InvalidOperationException("The driver did not confirm the original baseline. Recapture was cancelled.");
                }
                _records[info.Id] = new GammaBaselineRecord
                {
                    DisplayId = info.Id,
                    DeviceName = info.DeviceName,
                    Ramp = ramp,
                    AppliedSignature = string.Empty,
                    CapturedUtc = DateTime.UtcNow
                };
                Save(force: true);
            }
            finally
            {
                NativeMethods.DeleteDC(dc);
            }
        }
    }

    private GammaBaselineRecord? EnsureBaseline(DisplayInfo info, ushort[] current)
    {
        if (_records.TryGetValue(info.Id, out var record) && IsValidRamp(record.Ramp))
        {
            record.DeviceName = info.DeviceName;
            return record;
        }

        record = new GammaBaselineRecord
        {
            DisplayId = info.Id,
            DeviceName = info.DeviceName,
            Ramp = current.ToArray(),
            AppliedSignature = string.Empty,
            CapturedUtc = DateTime.UtcNow
        };
        _records[info.Id] = record;
        Save(force: true);
        return record;
    }

    private static double ComputeScale(bool hardwareBrightness, int brightness, int softwareDim)
    {
        var extra = Math.Clamp(100 - softwareDim, 10, 100) / 100.0;
        if (hardwareBrightness)
        {
            return extra;
        }

        var softwareBrightness = Math.Clamp(brightness, 10, 100) / 100.0;
        return Math.Clamp(softwareBrightness * extra, 0.10, 1.0);
    }

    private static (double R, double G, double B) KelvinMultipliers(int kelvin)
    {
        kelvin = Math.Clamp(kelvin, 2000, 6500);
        if (kelvin >= 6500)
        {
            return (1, 1, 1);
        }

        var (r, g, b) = KelvinToRgb(kelvin);
        var (nr, ng, nb) = KelvinToRgb(6500);
        return (Math.Clamp(r / nr, 0, 1), Math.Clamp(g / ng, 0, 1), Math.Clamp(b / nb, 0, 1));
    }

    private static (double R, double G, double B) KelvinToRgb(double kelvin)
    {
        var temp = kelvin / 100.0;
        double r;
        double g;
        double b;
        if (temp <= 66)
        {
            r = 255;
            g = 99.4708025861 * Math.Log(temp) - 161.1195681661;
            b = temp <= 19 ? 0 : 138.5177312231 * Math.Log(temp - 10) - 305.0447927307;
        }
        else
        {
            r = 329.698727446 * Math.Pow(temp - 60, -0.1332047592);
            g = 288.1221695283 * Math.Pow(temp - 60, -0.0755148492);
            b = 255;
        }

        return (Math.Clamp(r, 0, 255), Math.Clamp(g, 0, 255), Math.Clamp(b, 0, 255));
    }

    private static ushort[] Compose(ushort[] baseline, double scale, (double R, double G, double B) rgb)
    {
        var result = new ushort[768];
        for (var channel = 0; channel < 3; channel++)
        {
            var multiplier = channel switch { 0 => rgb.R, 1 => rgb.G, _ => rgb.B };
            var factor = scale * multiplier;
            for (var i = 0; i < 256; i++)
            {
                var value = (int)Math.Round(baseline[channel * 256 + i] * factor);
                result[channel * 256 + i] = (ushort)Math.Clamp(value, 0, 65535);
            }
        }

        return result;
    }

    private static ushort[]? ReadRamp(IntPtr dc)
    {
        var ramp = new ushort[768];
        return NativeMethods.GetDeviceGammaRamp(dc, ramp) ? ramp : null;
    }

    private static bool WriteRamp(IntPtr dc, ushort[] ramp)
    {
        try
        {
            return NativeMethods.SetDeviceGammaRamp(dc, ramp);
        }
        catch (EntryPointNotFoundException)
        {
            return false;
        }
    }

    private static string Signature(ushort[] ramp)
        => Convert.ToHexString(SHA256.HashData(MemoryMarshal.AsBytes(ramp.AsSpan())));

    private static bool IsValidRamp(ushort[]? ramp) => ramp is { Length: 768 };

    private GammaBaselineData Load()
    {
        try
        {
            if (!File.Exists(AppPaths.GammaFile))
            {
                return new GammaBaselineData();
            }

            var json = File.ReadAllText(AppPaths.GammaFile);
            var data = JsonSerializer.Deserialize<GammaBaselineData>(json, _json) ?? new GammaBaselineData();
            data.Displays ??= new Dictionary<string, GammaBaselineRecord>(StringComparer.OrdinalIgnoreCase);
            foreach (var key in data.Displays.Keys.ToList())
            {
                var record = data.Displays[key];
                if (!IsValidRamp(record.Ramp))
                {
                    data.Displays.Remove(key);
                }
            }

            return data;
        }
        catch (Exception ex)
        {
            _log.Error("Could not load gamma baselines.", ex);
            TryBackup();
            return new GammaBaselineData();
        }
    }

    private void TryBackup()
    {
        try
        {
            if (File.Exists(AppPaths.GammaFile))
            {
                File.Copy(AppPaths.GammaFile, AppPaths.GammaFile + $".corrupt-{DateTime.Now:yyyyMMdd-HHmmss}.bak", overwrite: false);
            }
        }
        catch
        {
            // ignored
        }
    }

    private void SaveIfDue(bool force)
    {
        if (!force && DateTime.UtcNow - _lastSaveUtc < TimeSpan.FromSeconds(2))
        {
            return;
        }

        Save(force);
    }

    private void Save(bool force)
    {
        try
        {
            var data = new GammaBaselineData { Displays = _records };
            var json = JsonSerializer.Serialize(data, _json);
            var temp = AppPaths.GammaFile + ".tmp";
            File.WriteAllText(temp, json);
            File.Move(temp, AppPaths.GammaFile, overwrite: true);
            _lastSaveUtc = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            _log.Error("Could not save gamma baselines.", ex);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        RestoreAll();
        _disposed = true;
    }
}
