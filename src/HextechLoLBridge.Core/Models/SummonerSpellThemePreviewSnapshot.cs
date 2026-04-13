namespace HextechLoLBridge.Core.Models;

public sealed record SummonerSpellThemePreviewSnapshot(
    string Id,
    string DisplayName,
    string DefaultHex,
    string CurrentHex,
    string ThemeLabel,
    string IconUrl,
    string ThemeNote,
    bool IsExtension = false);
