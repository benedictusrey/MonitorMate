namespace MonitorMate.Core.Models;

public enum DisplayControlPath
{
    HardwareDdc,
    SoftwareGamma,
    Unavailable
}

public sealed class PixelRect
{
    public int X { get; init; }
    public int Y { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }

    public int Right => X + Width;
    public int Bottom => Y + Height;

    public bool Contains(int x, int y) => x >= X && x < Right && y >= Y && y < Bottom;

    public override string ToString() => $"{X},{Y} {Width}x{Height}";
}

public sealed class DisplayInfo
{
    public required string Id { get; init; }
    public required string DeviceName { get; init; }
    public required string Name { get; init; }
    public required string AdapterName { get; init; }
    public required PixelRect Bounds { get; init; }
    public required PixelRect WorkArea { get; init; }
    public bool IsPrimary { get; init; }
    public uint Dpi { get; init; } = 96;
    public int MonitorNumber { get; init; }
    public bool HardwareBrightnessSupported { get; init; }
    public bool HardwareContrastSupported { get; init; }
    public uint HardwareBrightnessMin { get; init; }
    public uint HardwareBrightnessMax { get; init; }
    public uint HardwareContrastMax { get; init; }
    public uint OriginalBrightness { get; init; }
    public uint OriginalContrast { get; init; }
    public bool GammaSupported { get; init; }
    public string? GammaWarning { get; init; }
    public IntPtr PhysicalMonitorHandle { get; init; }
    public IntPtr MonitorHandle { get; init; }

    public string Resolution => $"{Bounds.Width} × {Bounds.Height}";
    public string FriendlyPosition => IsPrimary ? "Primary display" : "Secondary display";
    public DisplayControlPath ControlPath => HardwareBrightnessSupported
        ? DisplayControlPath.HardwareDdc
        : GammaSupported
            ? DisplayControlPath.SoftwareGamma
            : DisplayControlPath.Unavailable;
}

public sealed class DisplayState
{
    public required DisplayInfo Info { get; init; }
    public DisplayPreference Preference { get; init; } = new();
    public int AppliedBrightness { get; set; }
    public int AppliedContrast { get; set; } = -1;
    public int AppliedTemperatureK { get; set; } = 6500;
    public int AppliedSoftwareDim { get; set; }
    public string Status { get; set; } = "Ready";
    public string? LastError { get; set; }
    public bool IsUnconfirmed { get; set; }
    public bool IsHdrSuspected { get; set; }
}

public sealed class DisplaySnapshot
{
    public required string Id { get; init; }
    public required string DeviceName { get; init; }
    public required string Name { get; init; }
    public required string AdapterName { get; init; }
    public required PixelRect Bounds { get; init; }
    public required PixelRect WorkArea { get; init; }
    public bool IsPrimary { get; init; }
    public uint Dpi { get; init; }
    public bool HardwareBrightnessSupported { get; init; }
    public bool HardwareContrastSupported { get; init; }
    public uint BrightnessMax { get; init; }
    public uint ContrastMax { get; init; }
    public uint OriginalBrightness { get; init; }
    public uint OriginalContrast { get; init; }
    public bool GammaSupported { get; init; }
    public string? GammaWarning { get; init; }
    public IntPtr PhysicalMonitorHandle { get; init; }
    public IntPtr MonitorHandle { get; init; }
}
