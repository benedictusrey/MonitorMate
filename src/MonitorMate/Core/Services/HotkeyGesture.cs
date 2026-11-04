using System.Windows.Input;

namespace MonitorMate.Core.Services;

public sealed class HotkeyGesture
{
    public uint Modifiers { get; init; }
    public uint VirtualKey { get; init; }
    public string Display { get; init; } = string.Empty;

    public static HotkeyGesture? Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var parts = text.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            return null;
        }

        uint modifiers = 0;
        var keyName = string.Empty;
        foreach (var part in parts)
        {
            switch (part.ToLowerInvariant())
            {
                case "ctrl":
                case "control":
                    modifiers |= 0x0002;
                    break;
                case "alt":
                    modifiers |= 0x0001;
                    break;
                case "shift":
                    modifiers |= 0x0004;
                    break;
                case "win":
                case "windows":
                    modifiers |= 0x0008;
                    break;
                default:
                    keyName = part;
                    break;
            }
        }

        if (string.IsNullOrWhiteSpace(keyName))
        {
            return null;
        }

        var key = keyName.ToLowerInvariant() switch
        {
            "up" => Key.Up,
            "down" => Key.Down,
            "left" => Key.Left,
            "right" => Key.Right,
            "pageup" => Key.PageUp,
            "pagedown" => Key.PageDown,
            "home" => Key.Home,
            "end" => Key.End,
            "insert" => Key.Insert,
            "delete" => Key.Delete,
            "space" => Key.Space,
            "enter" => Key.Enter,
            "tab" => Key.Tab,
            "escape" or "esc" => Key.Escape,
            _ when keyName.Length == 1 && char.IsLetterOrDigit(keyName[0]) => KeyInterop.KeyFromVirtualKey(char.ToUpperInvariant(keyName[0])),
            _ when keyName.StartsWith('F') && int.TryParse(keyName[1..], out var f) && f is >= 1 and <= 24 => Key.F1 + (f - 1),
            _ => Key.None
        };

        if (key == Key.None)
        {
            return null;
        }

        var vk = (uint)KeyInterop.VirtualKeyFromKey(key);
        if (vk == 0)
        {
            return null;
        }

        var normalized = Normalize(modifiers, keyName);
        return new HotkeyGesture { Modifiers = modifiers | 0x4000, VirtualKey = vk, Display = normalized };
    }

    private static string Normalize(uint modifiers, string keyName)
    {
        var parts = new List<string>(4);
        if ((modifiers & 0x0002) != 0) parts.Add("Ctrl");
        if ((modifiers & 0x0001) != 0) parts.Add("Alt");
        if ((modifiers & 0x0004) != 0) parts.Add("Shift");
        if ((modifiers & 0x0008) != 0) parts.Add("Win");
        parts.Add(keyName.Length == 1 ? keyName.ToUpperInvariant() : keyName);
        return string.Join("+", parts);
    }
}
