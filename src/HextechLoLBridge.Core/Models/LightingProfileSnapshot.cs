namespace HextechLoLBridge.Core.Models;

public sealed record LightingProfileSnapshot(
    IReadOnlyList<SpellThemeSettingSnapshot> SpellThemes,
    IReadOnlyList<KeyMappingEntrySnapshot> KeyMappings,
    IReadOnlyList<KeyboardKeySnapshot> KeyboardKeys,
    IReadOnlyList<HeroThemeSettingSnapshot> HeroThemes);
