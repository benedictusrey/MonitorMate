namespace MonitorMate.Core.Models;

public sealed record ComfortPreset(
    string Id,
    string Name,
    string Description,
    int Brightness,
    int TemperatureK,
    int SoftwareDim,
    string Accent);

public static class PresetCatalog
{
    public static IReadOnlyList<ComfortPreset> All { get; } =
    [
        new("day", "Daylight", "Neutral colour for bright rooms and colour-sensitive work.", 100, 6500, 0, "day"),
        new("evening", "Evening", "A softer, warmer screen as daylight fades.", 82, 4800, 6, "evening"),
        new("night", "Night", "Low light with a deep warm cast for late sessions.", 46, 3600, 18, "night"),
        new("reading", "Reading", "Slightly warm and calm for long-form reading.", 86, 5200, 4, "reading"),
        new("focus", "Focus", "Clear and neutral for concentrated work.", 92, 6000, 0, "focus"),
        new("cinema", "Cinema", "Dim, gentle colour for video in a dark room.", 64, 5600, 12, "cinema")
    ];

    public static ComfortPreset Get(string id) =>
        All.FirstOrDefault(p => string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase)) ?? All[0];

    public static bool Exists(string id) =>
        All.Any(p => string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase));
}
