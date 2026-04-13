using HextechLoLBridge.Core.Models;

namespace HextechLoLBridge.Core.Catalog;

public static class KeyboardLayoutCatalog
{
    private static readonly KeyMappingEntrySnapshot[] DefaultMappings =
    [
        new("ability-q-ready", "Q 技能就绪", "Q", "Q", "#3AA8FF", "ability"),
        new("ability-w-ready", "W 技能就绪", "W", "W", "#3AA8FF", "ability"),
        new("ability-e-ready", "E 技能就绪", "E", "E", "#3AA8FF", "ability"),
        new("ability-r-ready", "R 技能就绪", "R", "R", "#3AA8FF", "ability"),
        new("summoner-1-ready", "双招一 / D", "D", "D", "#FF8A3C", "summoner"),
        new("summoner-2-ready", "双招二 / F", "F", "F", "#9B73FF", "summoner"),
        new("ready-check-accept", "准备确认 / 空格接受", "SPACE", "SPACE", "#FFFFFF", "queue")
    ];

    private static readonly KeyboardKeySnapshot[] KeyboardKeys =
    [
        new("ESC", "Esc", 0),
        new("F1", "F1", 0), new("F2", "F2", 0), new("F3", "F3", 0), new("F4", "F4", 0),
        new("F5", "F5", 0), new("F6", "F6", 0), new("F7", "F7", 0), new("F8", "F8", 0),
        new("F9", "F9", 0), new("F10", "F10", 0), new("F11", "F11", 0), new("F12", "F12", 0),
        new("PRNTSCR", "Prnt", 0), new("SCROLLLOCK", "Lock", 0), new("PAUSEBREAK", "P/B", 0),

        new("GRAVE", "~", 1), new("1", "1", 1), new("2", "2", 1), new("3", "3", 1), new("4", "4", 1),
        new("5", "5", 1), new("6", "6", 1), new("7", "7", 1), new("8", "8", 1), new("9", "9", 1),
        new("0", "0", 1), new("MINUS", "-", 1), new("EQUALS", "=", 1), new("BACKSPACE", "Back", 1, 2),
        new("INS", "Ins", 1), new("HOME", "Home", 1), new("PGUP", "Pg▲", 1),

        new("TAB", "Tab", 2, 2), new("Q", "Q", 2), new("W", "W", 2), new("E", "E", 2), new("R", "R", 2),
        new("T", "T", 2), new("Y", "Y", 2), new("U", "U", 2), new("I", "I", 2), new("O", "O", 2), new("P", "P", 2),
        new("LBRACKET", "[", 2), new("RBRACKET", "]", 2), new("BACKSLASH", "\\", 2, 2),
        new("DEL", "Del", 2), new("END", "End", 2), new("PGDN", "Pg▼", 2),

        new("CAPSLOCK", "Caps", 3, 2), new("A", "A", 3), new("S", "S", 3), new("D", "D", 3), new("F", "F", 3),
        new("G", "G", 3), new("H", "H", 3), new("J", "J", 3), new("K", "K", 3), new("L", "L", 3),
        new("SEMICOLON", ";", 3), new("APOSTROPHE", "'", 3), new("ENTER", "Enter", 3, 3),

        new("LSHIFT", "Shift", 4, 2), new("Z", "Z", 4), new("X", "X", 4), new("C", "C", 4), new("V", "V", 4),
        new("B", "B", 4), new("N", "N", 4), new("M", "M", 4), new("COMMA", ",", 4), new("PERIOD", ".", 4), new("SLASH", "/", 4),
        new("RSHIFT", "Shift", 4, 3), new("UP", "▲", 4),

        new("LCTRL", "Ctrl", 5, 2), new("LWIN", "Win", 5), new("LALT", "Alt", 5), new("SPACE", "Space", 5, 6),
        new("RALT", "Alt", 5), new("FN", "Fn", 5), new("MENU", "≡", 5), new("RCTRL", "Ctrl", 5),
        new("LEFT", "◀", 5), new("DOWN", "▼", 5), new("RIGHT", "▶", 5)
    ];

    private static readonly Dictionary<string, int> ScanCodeMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ESC"] = 1,
        ["1"] = 2, ["2"] = 3, ["3"] = 4, ["4"] = 5, ["5"] = 6, ["6"] = 7, ["7"] = 8, ["8"] = 9, ["9"] = 10, ["0"] = 11,
        ["MINUS"] = 12, ["EQUALS"] = 13, ["BACKSPACE"] = 14, ["TAB"] = 15,
        ["Q"] = 16, ["W"] = 17, ["E"] = 18, ["R"] = 19, ["T"] = 20, ["Y"] = 21, ["U"] = 22, ["I"] = 23, ["O"] = 24, ["P"] = 25,
        ["LBRACKET"] = 26, ["RBRACKET"] = 27, ["ENTER"] = 28, ["LCTRL"] = 29,
        ["A"] = 30, ["S"] = 31, ["D"] = 32, ["F"] = 33, ["G"] = 34, ["H"] = 35, ["J"] = 36, ["K"] = 37, ["L"] = 38,
        ["SEMICOLON"] = 39, ["APOSTROPHE"] = 40, ["GRAVE"] = 41, ["LSHIFT"] = 42, ["BACKSLASH"] = 43,
        ["CAPSLOCK"] = 58,
        ["Z"] = 44, ["X"] = 45, ["C"] = 46, ["V"] = 47, ["B"] = 48, ["N"] = 49, ["M"] = 50, ["COMMA"] = 51, ["PERIOD"] = 52, ["SLASH"] = 53, ["RSHIFT"] = 54,
        ["LALT"] = 56, ["SPACE"] = 57,
        ["F1"] = 59, ["F2"] = 60, ["F3"] = 61, ["F4"] = 62, ["F5"] = 63, ["F6"] = 64, ["F7"] = 65, ["F8"] = 66, ["F9"] = 67, ["F10"] = 68,
        ["SCROLLLOCK"] = 70,
        ["F11"] = 87, ["F12"] = 88,
        ["RCTRL"] = 157, ["PRNTSCR"] = 183, ["RALT"] = 184, ["PAUSEBREAK"] = 197,
        ["HOME"] = 199, ["UP"] = 200, ["PGUP"] = 201,
        ["LEFT"] = 203, ["RIGHT"] = 205, ["END"] = 207, ["DOWN"] = 208, ["PGDN"] = 209, ["INS"] = 210, ["DEL"] = 211,
        ["LWIN"] = 219, ["RWIN"] = 220, ["MENU"] = 221
    };

    public static IReadOnlyList<KeyMappingEntrySnapshot> GetDefaultMappings() => DefaultMappings;

    public static IReadOnlyList<KeyboardKeySnapshot> GetKeyboardKeys() => KeyboardKeys;

    public static int ResolveScanCode(string? keyCode)
        => !string.IsNullOrWhiteSpace(keyCode) && ScanCodeMap.TryGetValue(keyCode, out var scanCode) ? scanCode : 0;
}
