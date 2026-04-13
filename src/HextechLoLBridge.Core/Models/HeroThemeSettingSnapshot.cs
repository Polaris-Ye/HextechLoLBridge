namespace HextechLoLBridge.Core.Models;

public sealed record HeroThemeSettingSnapshot(
    string ChampionKey,
    string DisplayName,
    string DefaultHex,
    string CurrentHex,
    string ThemeLabel,
    string? IconUrl,
    string SearchText);
