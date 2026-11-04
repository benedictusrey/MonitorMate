namespace MonitorMate.Core.Models;

public sealed class GammaBaselineRecord
{
    public string DisplayId { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public ushort[] Ramp { get; set; } = Array.Empty<ushort>();
    public string AppliedSignature { get; set; } = string.Empty;
    public DateTime CapturedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? AppliedUtc { get; set; }
}

public sealed class GammaBaselineData
{
    public int SchemaVersion { get; set; } = 1;
    public Dictionary<string, GammaBaselineRecord> Displays { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
