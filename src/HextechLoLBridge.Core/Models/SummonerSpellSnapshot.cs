namespace HextechLoLBridge.Core.Models;

public sealed record SummonerSpellSnapshot(
    string Slot,
    string DisplayName,
    string RawDisplayName,
    string? InternalKey = null,
    string SpellId = "default",
    string ThemeHex = "#6C7A89",
    string ThemeLabel = "默认冷灰",
    string? IconUrl = null,
    string ThemeNote = "未命中预设主题，先使用默认冷灰。",
    bool? IsReady = null,
    double? CooldownSeconds = null);
