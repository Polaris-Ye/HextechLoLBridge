namespace HextechLoLBridge.Core.Models;

public sealed record SpellThemeSettingSnapshot(
    string SpellId,
    string DisplayName,
    string DefaultHex,
    string CurrentHex,
    string ThemeLabel,
    string IconUrl,
    bool IsExtension = false);
