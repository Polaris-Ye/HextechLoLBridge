using System.Text.Json;
using HextechLoLBridge.Core.Catalog;
using HextechLoLBridge.Core.Models;

namespace HextechLoLBridge.Core.Services;

public sealed class LightingProfileService
{
    private readonly IAppLogger _logger;
    private readonly string _profilePath;
    private readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private Dictionary<string, string> _spellColorOverrides = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, string> _keyMappings = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, string> _heroColorOverrides = new(StringComparer.OrdinalIgnoreCase);

    public LightingProfileService(IAppLogger logger)
    {
        _logger = logger;
        var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HextechLoLBridge");
        Directory.CreateDirectory(directory);
        _profilePath = Path.Combine(directory, "lighting-profile.json");
        Load();
    }

    public LightingProfileSnapshot GetSnapshot(string? activeChampionName = null)
    {
        var spellThemes = SummonerSpellThemeCatalog.CreateDefaultSettings()
            .Select(item => item with
            {
                CurrentHex = GetSpellHex(item.SpellId, item.DefaultHex)
            })
            .ToArray();

        var keyMappings = KeyboardLayoutCatalog.GetDefaultMappings()
            .Select(item => item with
            {
                CurrentKey = GetMappedKey(item.ActionId, item.DefaultKey)
            })
            .ToArray();

        var heroThemes = HeroThemeCatalog.CreateDefaultSettings(activeChampionName)
            .Select(item => item with
            {
                CurrentHex = GetHeroHex(item.ChampionKey, item.DefaultHex)
            })
            .ToArray();

        return new LightingProfileSnapshot(spellThemes, keyMappings, KeyboardLayoutCatalog.GetKeyboardKeys(), heroThemes);
    }

    public IReadOnlyDictionary<string, string> GetSpellColorOverrides()
        => new Dictionary<string, string>(_spellColorOverrides, StringComparer.OrdinalIgnoreCase);

    public string GetSpellHex(string spellId, string fallback)
        => _spellColorOverrides.TryGetValue(spellId, out var configured)
            ? SummonerSpellThemeCatalog.NormalizeHex(configured, fallback)
            : fallback.ToUpperInvariant();

    public void SetSpellColor(string spellId, string hex)
    {
        var defaults = SummonerSpellThemeCatalog.CreateDefaultSettings().FirstOrDefault(x => x.SpellId == spellId);
        if (defaults is null)
        {
            return;
        }

        _spellColorOverrides[spellId] = SummonerSpellThemeCatalog.NormalizeHex(hex, defaults.DefaultHex);
        Save();
    }

    public void ResetSpellColor(string spellId)
    {
        if (_spellColorOverrides.Remove(spellId))
        {
            Save();
        }
    }

    public string GetHeroHex(string championName, string? fallback = null)
    {
        var resolvedKey = HeroThemeCatalog.ResolveChampionKey(championName);
        var defaultHex = fallback ?? HeroThemeCatalog.ResolveChampionHex(resolvedKey);
        var normalized = HeroThemeCatalog.NormalizeKey(resolvedKey);
        return _heroColorOverrides.TryGetValue(normalized, out var configured)
            ? SummonerSpellThemeCatalog.NormalizeHex(configured, defaultHex)
            : defaultHex.ToUpperInvariant();
    }

    public void SetHeroColor(string championName, string hex)
    {
        if (string.IsNullOrWhiteSpace(championName))
        {
            return;
        }

        var resolvedKey = HeroThemeCatalog.ResolveChampionKey(championName);
        var normalized = HeroThemeCatalog.NormalizeKey(resolvedKey);
        var fallback = HeroThemeCatalog.ResolveChampionHex(resolvedKey);
        _heroColorOverrides[normalized] = SummonerSpellThemeCatalog.NormalizeHex(hex, fallback);
        Save();
    }

    public void ResetHeroColor(string championName)
    {
        if (string.IsNullOrWhiteSpace(championName))
        {
            return;
        }

        if (_heroColorOverrides.Remove(HeroThemeCatalog.NormalizeKey(HeroThemeCatalog.ResolveChampionKey(championName))))
        {
            Save();
        }
    }

    public string GetMappedKey(string actionId, string fallback)
        => _keyMappings.TryGetValue(actionId, out var keyCode) ? keyCode : fallback;

    public void SetKeyMapping(string actionId, string keyCode)
    {
        if (string.IsNullOrWhiteSpace(actionId) || string.IsNullOrWhiteSpace(keyCode))
        {
            return;
        }

        _keyMappings[actionId] = keyCode.Trim().ToUpperInvariant();
        Save();
    }

    public void ResetKeyMapping(string actionId)
    {
        if (_keyMappings.Remove(actionId))
        {
            Save();
        }
    }

    public int ResolveScanCodeForAction(string actionId)
    {
        var fallback = KeyboardLayoutCatalog.GetDefaultMappings().FirstOrDefault(x => x.ActionId == actionId)?.DefaultKey ?? string.Empty;
        var keyCode = GetMappedKey(actionId, fallback);
        return KeyboardLayoutCatalog.ResolveScanCode(keyCode);
    }

    public LeagueSnapshot ApplyOverrides(LeagueSnapshot snapshot)
    {
        var overrides = GetSpellColorOverrides();
        var player = snapshot.ActivePlayer;
        var remappedSpells = player.SummonerSpells.Select(spell => SummonerSpellThemeCatalog.Enrich(spell, overrides)).ToArray();
        var updatedPlayer = player with { SummonerSpells = remappedSpells };
        return snapshot with { ActivePlayer = updatedPlayer };
    }

    private void Load()
    {
        try
        {
            if (!File.Exists(_profilePath))
            {
                Save();
                return;
            }

            var json = File.ReadAllText(_profilePath);
            var persisted = JsonSerializer.Deserialize<PersistedLightingProfile>(json, _serializerOptions);
            _spellColorOverrides = persisted?.SpellColorOverrides is not null
                ? new Dictionary<string, string>(persisted.SpellColorOverrides, StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _keyMappings = persisted?.KeyMappings is not null
                ? new Dictionary<string, string>(persisted.KeyMappings, StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _heroColorOverrides = persisted?.HeroColorOverrides is not null
                ? new Dictionary<string, string>(persisted.HeroColorOverrides, StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.Warn($"读取灯光配置失败，将使用默认配置：{ex.Message}");
            _spellColorOverrides = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _keyMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _heroColorOverrides = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private void Save()
    {
        try
        {
            var payload = new PersistedLightingProfile
            {
                SpellColorOverrides = _spellColorOverrides,
                KeyMappings = _keyMappings,
                HeroColorOverrides = _heroColorOverrides
            };
            var json = JsonSerializer.Serialize(payload, _serializerOptions);
            File.WriteAllText(_profilePath, json);
        }
        catch (Exception ex)
        {
            _logger.Warn($"保存灯光配置失败：{ex.Message}");
        }
    }

    private sealed class PersistedLightingProfile
    {
        public Dictionary<string, string>? SpellColorOverrides { get; set; }
        public Dictionary<string, string>? KeyMappings { get; set; }
        public Dictionary<string, string>? HeroColorOverrides { get; set; }
    }
}
