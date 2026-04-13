using HextechLoLBridge.Core.Models;

namespace HextechLoLBridge.Core.Catalog;

public static class SummonerSpellThemeCatalog
{
    public const string DataDragonVersion = "16.7.1";
    private const string BaseUrl = "https://ddragon.leagueoflegends.com/cdn/" + DataDragonVersion + "/img/spell/";
    private const string LocalHexflashIcon = "https://appassets/icons/hextechflashtraption.png";

    private static readonly SummonerSpellThemePreviewSnapshot[] Defaults =
    [
        new("flash", "闪现", "#FFF152", "#FFF152", "闪光黄", BaseUrl + "SummonerFlash.png", "位移瞬发感最强，适合做高优先级亮键。"),
        new("teleport", "传送", "#A03EAC", "#A03EAC", "星界紫", BaseUrl + "SummonerTeleport.png", "传送门和空间感明显，适合做紫色脉冲。"),
        new("ghost", "疾跑", "#24A0FF", "#24A0FF", "疾速蓝", BaseUrl + "SummonerHaste.png", "图标整体偏冷蓝，适合做速度流动效果。"),
        new("ignite", "点燃", "#D82613", "#D82613", "点燃红", BaseUrl + "SummonerDot.png", "火焰感最强，适合做短促爆闪。"),
        new("heal", "治疗", "#4ABD42", "#4ABD42", "高饱和治疗绿", BaseUrl + "SummonerHeal.png", "恢复感更强，适合做抬血脉冲。"),
        new("smite", "惩戒", "#EEB62D", "#EEB62D", "惩戒金", BaseUrl + "SummonerSmite.png", "改成更高亮的惩戒金，方便和闪现区分。"),
        new("barrier", "屏障", "#D5A64A", "#D5A64A", "护盾金", BaseUrl + "SummonerBarrier.png", "金色护盾感明显，适合稳定常亮。"),
        new("exhaust", "虚弱", "#8C7652", "#8C7652", "枯沙褐", BaseUrl + "SummonerExhaust.png", "有压制与削弱感，适合低饱和减速色。"),
        new("cleanse", "净化", "#9FE8FF", "#9FE8FF", "净澈白蓝", BaseUrl + "SummonerBoost.png", "净白和浅蓝明显，适合清除类灯效。"),
        new("clarity", "清晰术", "#4C8FFF", "#4C8FFF", "法力亮蓝", BaseUrl + "SummonerMana.png", "更深一点、也更亮的法力蓝。"),
        new("snowball", "雪球 / 标记", "#F8FBFF", "#F8FBFF", "雪域白", BaseUrl + "SummonerSnowball.png", "嚎哭深渊专用，适合白色点亮或短闪。"),
        new("hexflash", "海克斯闪现（扩展）", "#DC9C78", "#DC9C78", "海闪铜橙", LocalHexflashIcon, "改成当前测试页里使用的海克斯闪现配色。", true)
    ];

    private static readonly Dictionary<int, string> SpellIdMap = new()
    {
        [1] = "cleanse",
        [3] = "exhaust",
        [4] = "flash",
        [6] = "ghost",
        [7] = "heal",
        [11] = "smite",
        [12] = "teleport",
        [13] = "clarity",
        [14] = "ignite",
        [21] = "barrier",
        [32] = "snowball"
    };

    public static IReadOnlyList<SummonerSpellThemePreviewSnapshot> GetDefaultItems() => Defaults;

    public static IReadOnlyList<SpellThemeSettingSnapshot> CreateDefaultSettings()
        => Defaults.Select(x => new SpellThemeSettingSnapshot(x.Id, x.DisplayName, x.DefaultHex, x.CurrentHex, x.ThemeLabel, x.IconUrl, x.IsExtension)).ToArray();

    public static SummonerSpellThemePreviewSnapshot ResolveDefaultPreview(string? displayName, string? rawDisplayName, string? internalKey)
    {
        var text = $"{displayName} {rawDisplayName} {internalKey}".ToLowerInvariant();

        if (ContainsAny(text, "flash", "闪现") && !ContainsAny(text, "hex", "海克斯")) return Defaults.First(x => x.Id == "flash");
        if (ContainsAny(text, "teleport", "传送")) return Defaults.First(x => x.Id == "teleport");
        if (ContainsAny(text, "haste", "ghost", "疾跑")) return Defaults.First(x => x.Id == "ghost");
        if (ContainsAny(text, "dot", "ignite", "点燃")) return Defaults.First(x => x.Id == "ignite");
        if (ContainsAny(text, "heal", "治疗")) return Defaults.First(x => x.Id == "heal");
        if (ContainsAny(text, "smite", "惩戒")) return Defaults.First(x => x.Id == "smite");
        if (ContainsAny(text, "barrier", "屏障")) return Defaults.First(x => x.Id == "barrier");
        if (ContainsAny(text, "exhaust", "虚弱")) return Defaults.First(x => x.Id == "exhaust");
        if (ContainsAny(text, "boost", "cleanse", "净化")) return Defaults.First(x => x.Id == "cleanse");
        if (ContainsAny(text, "mana", "clarity", "清晰")) return Defaults.First(x => x.Id == "clarity");
        if (ContainsAny(text, "snowball", "snow", "雪球", "标记")) return Defaults.First(x => x.Id == "snowball");
        if (ContainsAny(text, "hexflash", "hextechflash", "海克斯闪现", "海闪")) return Defaults.First(x => x.Id == "hexflash");

        return new SummonerSpellThemePreviewSnapshot(
            "default",
            SnapshotName(displayName, rawDisplayName),
            "#6C7A89",
            "#6C7A89",
            "默认冷灰",
            BaseUrl + "SummonerFlash.png",
            "当前没有命中预设主题，先使用默认冷灰。",
            false);
    }

    public static SummonerSpellThemePreviewSnapshot ResolveDefaultPreviewByNumericId(int spellId)
    {
        if (SpellIdMap.TryGetValue(spellId, out var key))
        {
            return Defaults.First(x => x.Id == key);
        }

        return new SummonerSpellThemePreviewSnapshot(
            $"spell-{spellId}",
            $"未知技能 {spellId}",
            "#6C7A89",
            "#6C7A89",
            "默认冷灰",
            BaseUrl + "SummonerFlash.png",
            "当前没有命中预设主题，先使用默认冷灰。",
            false);
    }

    public static SummonerSpellSnapshot FromSpellId(int spellId, string slot, IReadOnlyDictionary<string, string>? overrides = null)
    {
        var theme = ResolveDefaultPreviewByNumericId(spellId);
        var currentHex = overrides is not null && overrides.TryGetValue(theme.Id, out var configuredHex)
            ? NormalizeHex(configuredHex, theme.DefaultHex)
            : theme.DefaultHex;

        return new SummonerSpellSnapshot(
            Slot: slot,
            DisplayName: theme.DisplayName,
            RawDisplayName: theme.DisplayName,
            InternalKey: $"spellId:{spellId}",
            SpellId: theme.Id,
            ThemeHex: currentHex,
            ThemeLabel: theme.ThemeLabel,
            IconUrl: theme.IconUrl,
            ThemeNote: "来自客户端阶段选中的召唤师技能。",
            IsReady: null,
            CooldownSeconds: null);
    }

    public static SummonerSpellSnapshot Enrich(SummonerSpellSnapshot snapshot, IReadOnlyDictionary<string, string>? overrides = null)
    {
        var theme = ResolveDefaultPreview(snapshot.DisplayName, snapshot.RawDisplayName, snapshot.InternalKey);
        var currentHex = overrides is not null && overrides.TryGetValue(theme.Id, out var configuredHex)
            ? NormalizeHex(configuredHex, theme.DefaultHex)
            : theme.DefaultHex;

        return snapshot with
        {
            SpellId = theme.Id,
            ThemeHex = currentHex,
            ThemeLabel = theme.ThemeLabel,
            IconUrl = theme.IconUrl,
            ThemeNote = theme.ThemeNote
        };
    }

    public static string NormalizeHex(string? hex, string fallback)
    {
        if (string.IsNullOrWhiteSpace(hex))
        {
            return fallback.ToUpperInvariant();
        }

        var value = hex.Trim();
        if (!value.StartsWith('#'))
        {
            value = $"#{value}";
        }

        if (value.Length != 7)
        {
            return fallback.ToUpperInvariant();
        }

        return value.ToUpperInvariant();
    }

    private static string SnapshotName(string? display, string? raw)
        => !string.IsNullOrWhiteSpace(display) ? display! : !string.IsNullOrWhiteSpace(raw) ? raw! : "未知技能";

    private static bool ContainsAny(string text, params string[] values)
        => values.Any(value => text.Contains(value, StringComparison.OrdinalIgnoreCase));
}
