using System.Net.Http.Headers;
using System.Text.Json;
using HextechLoLBridge.Core.Catalog;
using HextechLoLBridge.Core.Models;
using HextechLoLBridge.Core.Services;

namespace HextechLoLBridge.Core.Riot;

public sealed class RiotLiveClientService
{
    private static readonly string[] AbilitySlots = new[] { "Passive", "Q", "W", "E", "R" };
    private static readonly string[] PrimarySlots = new[] { "Q", "W", "E" };

    private readonly HttpClient _httpClient;
    private readonly IAppLogger _logger;

    public RiotLiveClientService(RiotLiveClientSettings settings, IAppLogger logger)
    {
        _logger = logger;

        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        };

        _httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(settings.BaseAddress),
            Timeout = settings.EffectiveTimeout
        };
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<LeagueSnapshot> CaptureSnapshotAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _httpClient.GetAsync("liveclientdata/allgamedata", cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var note = $"LoL Live Client API 返回了 {(int)response.StatusCode}。";
                return LeagueSnapshot.Disconnected("http-error", note, "请确认你已经进入一局实际对局。", "自定义、人机、匹配开始后才会有数据。");
            }

            await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var document = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken).ConfigureAwait(false);
            return MapSnapshot(document.RootElement);
        }
        catch (TaskCanceledException)
        {
            return LeagueSnapshot.Disconnected("timeout", "连接 LoL 本地 API 超时。", "如果游戏刚加载，可稍后再试。");
        }
        catch (HttpRequestException ex)
        {
            return LeagueSnapshot.Disconnected("unavailable", "未检测到 LoL 本地实时接口。", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.Error($"解析 LoL 本地状态失败：{ex.Message}");
            return LeagueSnapshot.Disconnected("parse-error", "解析 LoL 状态失败。", ex.Message);
        }
    }

    private LeagueSnapshot MapSnapshot(JsonElement root)
    {
        var capturedAt = DateTimeOffset.Now;

        var gameData = root.GetPropertyOrNull("gameData");
        var activePlayer = root.GetPropertyOrNull("activePlayer");
        var allPlayers = root.GetPropertyOrNull("allPlayers");
        var eventsContainer = root.GetPropertyOrNull("events");

        var riotId = activePlayer?.GetStringOrNull("riotId")
                     ?? BuildRiotId(activePlayer)
                     ?? "-";

        var matchedPlayer = FindMatchingPlayer(allPlayers, riotId, activePlayer);
        var game = MapGame(gameData);
        var events = MapEvents(eventsContainer);
        var playerNameTeamMap = BuildPlayerTeamMap(allPlayers);
        var objectives = MapObjectives(eventsContainer, playerNameTeamMap);
        var player = MapPlayer(activePlayer, matchedPlayer, riotId);

        return new LeagueSnapshot(
            InGame: true,
            ConnectionState: "connected",
            CapturedAt: capturedAt,
            Game: game,
            ActivePlayer: player,
            Objectives: objectives,
            RecentEvents: events,
            Notes: new[]
            {
                "当前仍用 /allgamedata 快速打通主链路。",
                "红蓝方来自 allPlayers[].team 的 ORDER / CHAOS 映射。",
                "双招来自 allPlayers[].summonerSpells。",
                "推荐升级技能仍是本地启发式，不是 Riot 官方直接返回的推荐器。",
                player.BuffDataAvailable
                    ? "当前增益面板为最佳努力解析，已检测到可用 Buff 字段。"
                    : "本次返回内容里未检测到稳定 Buff 字段，因此当前增益可能为空。",
                "键盘默认保持熄灯；只有就绪且已映射的功能键才会点亮。",
                "如果 Live Client API 没返回技能或双招的 ready 字段，本版会在未读到 CD 时按兼容模式视为可用；一旦读到 CD>0 仍会熄灭。"
            },
            ClientPhase: ClientPhaseSnapshot.Empty);
    }

    private static GameSnapshot MapGame(JsonElement? gameData)
    {
        if (gameData is null)
        {
            return GameSnapshot.Empty;
        }

        return new GameSnapshot(
            GameMode: gameData.Value.GetStringOrNull("gameMode") ?? "-",
            MapName: gameData.Value.GetStringOrNull("mapName") ?? "-",
            MapNumber: gameData.Value.GetInt32OrDefault("mapNumber"),
            MapTerrain: gameData.Value.GetStringOrNull("mapTerrain") ?? "-",
            GameTimeSeconds: gameData.Value.GetDoubleOrDefault("gameTime"));
    }

    private static PlayerSnapshot MapPlayer(JsonElement? activePlayer, JsonElement? matchedPlayer, string riotId)
    {
        if (activePlayer is null)
        {
            return PlayerSnapshot.Empty with { RiotId = riotId };
        }

        var championStats = activePlayer.Value.GetPropertyOrNull("championStats");
        var abilitiesElement = activePlayer.Value.GetPropertyOrNull("abilities");
        var scores = matchedPlayer?.GetPropertyOrNull("scores");

        var currentHealth = championStats?.GetNullableDouble("currentHealth", "health", "currentHp") ?? 0;
        var maxHealth = championStats?.GetNullableDouble("maxHealth", "healthMax", "maxHp") ?? 0;
        var currentResource = championStats?.GetNullableDouble("resourceValue", "resourceCurrent", "currentMana", "currentResource") ?? 0;
        var maxResource = championStats?.GetNullableDouble("resourceMax", "resourceMaximum", "maxMana", "maxResource") ?? 0;
        var abilities = MapAbilities(abilitiesElement);
        var level = activePlayer.Value.GetInt32OrDefault("level");
        var skillPointsAvailable = Math.Max(0, level - abilities.Where(x => x.Slot is "Q" or "W" or "E" or "R").Sum(x => x.AbilityLevel));
        var recommendation = BuildLevelUpRecommendation(abilities, skillPointsAvailable);
        var summonerSpells = MapSummonerSpells(matchedPlayer?.GetPropertyOrNull("summonerSpells"));
        var buffs = MapBuffs(activePlayer, matchedPlayer);
        var buffDataAvailable = buffs.Count > 0 || HasBuffField(activePlayer) || HasBuffField(matchedPlayer);

        return new PlayerSnapshot(
            RiotId: riotId,
            RiotIdGameName: activePlayer.Value.GetStringOrNull("riotIdGameName") ?? "-",
            RiotIdTagLine: activePlayer.Value.GetStringOrNull("riotIdTagLine") ?? "-",
            ChampionName: matchedPlayer?.GetStringOrNull("championName") ?? "-",
            Team: matchedPlayer?.GetStringOrNull("team") ?? "-",
            TeamDisplayName: MapTeamDisplayName(matchedPlayer?.GetStringOrNull("team")),
            IsDead: matchedPlayer?.GetBooleanOrDefault("isDead") ?? false,
            Level: level,
            CurrentHealth: currentHealth,
            MaxHealth: maxHealth,
            HealthPercent: maxHealth <= 0 ? 0 : currentHealth / maxHealth,
            ResourceType: championStats?.GetStringOrNull("resourceType") ?? "NONE",
            CurrentResource: currentResource,
            MaxResource: maxResource,
            ResourcePercent: maxResource <= 0 ? 0 : currentResource / maxResource,
            Kills: scores?.GetInt32OrDefault("kills") ?? 0,
            Deaths: scores?.GetInt32OrDefault("deaths") ?? 0,
            Assists: scores?.GetInt32OrDefault("assists") ?? 0,
            CreepScore: scores?.GetInt32OrDefault("creepScore") ?? 0,
            SkillPointsAvailable: skillPointsAvailable,
            LevelUpRecommendation: recommendation,
            Abilities: abilities,
            SummonerSpells: summonerSpells,
            ActiveBuffs: buffs,
            BuffDataAvailable: buffDataAvailable,
            HasRedBuff: buffs.Any(x => x.Category == "red-buff"),
            HasBlueBuff: buffs.Any(x => x.Category == "blue-buff"),
            HasBaronBuff: buffs.Any(x => x.Category == "baron-buff"),
            RespawnTimerSeconds: matchedPlayer?.GetDoubleOrDefault("respawnTimer") ?? 0);
    }

    private static IReadOnlyList<AbilitySnapshot> MapAbilities(JsonElement? abilities)
    {
        if (abilities is null)
        {
            return Array.Empty<AbilitySnapshot>();
        }

        var result = new List<AbilitySnapshot>();

        foreach (var slot in AbilitySlots)
        {
            if (!abilities.Value.TryGetPropertyIgnoreCase(slot, out var abilityElement))
            {
                continue;
            }

            var abilityLevel = abilityElement.GetInt32OrDefault("abilityLevel");
            var isLearned = slot.Equals("Passive", StringComparison.OrdinalIgnoreCase) || abilityLevel > 0;
            var cooldown = abilityElement.GetNullableDouble("cooldown", "cooldownSeconds", "cooldownRemaining", "remainingCooldown");
            var isReady = abilityElement.GetNullableBoolean("isReady", "canCast", "abilityCanUse", "usable");
            if (isReady is null && cooldown.HasValue)
            {
                isReady = cooldown.Value <= 0.01;
            }

            result.Add(new AbilitySnapshot(
                Slot: slot,
                DisplayName: abilityElement.GetStringOrNull("displayName") ?? slot,
                AbilityId: abilityElement.GetStringOrNull("id") ?? slot,
                AbilityLevel: abilityLevel,
                IsLearned: isLearned,
                IsReady: isReady,
                CooldownSeconds: cooldown));
        }

        return result;
    }

    private static IReadOnlyList<SummonerSpellSnapshot> MapSummonerSpells(JsonElement? summonerSpells)
    {
        if (summonerSpells is null || summonerSpells.Value.ValueKind != JsonValueKind.Object)
        {
            return Array.Empty<SummonerSpellSnapshot>();
        }

        var list = new List<SummonerSpellSnapshot>();
        foreach (var property in summonerSpells.Value.EnumerateObject())
        {
            if (property.Value.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var slot = property.Name switch
            {
                "summonerSpellOne" => "双招一",
                "summonerSpellTwo" => "双招二",
                _ => property.Name
            };

            var cooldown = property.Value.GetNullableDouble("cooldown", "cooldownSeconds", "cooldownRemaining", "remainingCooldown");
            var isReady = property.Value.GetNullableBoolean("isReady", "canUse", "canCast", "usable");
            if (isReady is null && cooldown.HasValue)
            {
                isReady = cooldown.Value <= 0.01;
            }

            list.Add(SummonerSpellThemeCatalog.Enrich(new SummonerSpellSnapshot(
                Slot: slot,
                DisplayName: property.Value.GetStringOrNull("displayName") ?? slot,
                RawDisplayName: property.Value.GetStringOrNull("rawDisplayName") ?? property.Name,
                InternalKey: property.Name,
                IsReady: isReady,
                CooldownSeconds: cooldown)));
        }

        return list;
    }

    private static IReadOnlyList<BuffSnapshot> MapBuffs(JsonElement? activePlayer, JsonElement? matchedPlayer)
    {
        var result = new List<BuffSnapshot>();
        AppendBuffs(result, activePlayer?.GetPropertyOrNull("buffs"));
        AppendBuffs(result, activePlayer?.GetPropertyOrNull("activeBuffs"));
        AppendBuffs(result, matchedPlayer?.GetPropertyOrNull("buffs"));
        AppendBuffs(result, matchedPlayer?.GetPropertyOrNull("activeBuffs"));

        return result
            .GroupBy(x => (x.DisplayName, x.RawId, x.Category))
            .Select(g => g.OrderByDescending(x => x.DurationSeconds ?? 0).First())
            .ToArray();
    }

    private static void AppendBuffs(List<BuffSnapshot> target, JsonElement? buffsElement)
    {
        if (buffsElement is null || buffsElement.Value.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var item in buffsElement.Value.EnumerateArray())
        {
            switch (item.ValueKind)
            {
                case JsonValueKind.String:
                {
                    var text = item.GetString() ?? "未知 Buff";
                    target.Add(new BuffSnapshot(text, text, CategorizeBuff(text, text)));
                    break;
                }
                case JsonValueKind.Object:
                {
                    var displayName = item.GetStringOrNull("displayName")
                                      ?? item.GetStringOrNull("name")
                                      ?? item.GetStringOrNull("buffName")
                                      ?? item.GetStringOrNull("rawDisplayName")
                                      ?? item.GetStringOrNull("id")
                                      ?? "未知 Buff";
                    var rawId = item.GetStringOrNull("id")
                                ?? item.GetStringOrNull("buffName")
                                ?? displayName;
                    var count = item.GetInt32OrDefault("count", item.GetInt32OrDefault("stacks"));
                    var duration = item.GetDoubleOrDefault("duration", item.GetDoubleOrDefault("durationSeconds", double.NaN));
                    target.Add(new BuffSnapshot(
                        DisplayName: displayName,
                        RawId: rawId,
                        Category: CategorizeBuff(displayName, rawId),
                        Count: count,
                        DurationSeconds: double.IsNaN(duration) ? null : duration));
                    break;
                }
            }
        }
    }

    private static bool HasBuffField(JsonElement? element)
    {
        if (element is null || element.Value.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        return element.Value.TryGetPropertyIgnoreCase("buffs", out _) || element.Value.TryGetPropertyIgnoreCase("activeBuffs", out _);
    }

    private static string CategorizeBuff(string displayName, string rawId)
    {
        var text = $"{displayName} {rawId}".ToLowerInvariant();
        if (text.Contains("baron") || text.Contains("exaltedwithbaronnashor"))
        {
            return "baron-buff";
        }

        if (text.Contains("cinder") || text.Contains("redbuff") || text.Contains("brambleback") || text.Contains("crestofcinders"))
        {
            return "red-buff";
        }

        if (text.Contains("insight") || text.Contains("bluebuff") || text.Contains("sentinel") || text.Contains("crestofinsight"))
        {
            return "blue-buff";
        }

        return "general";
    }

    private static LevelUpRecommendationSnapshot BuildLevelUpRecommendation(IReadOnlyList<AbilitySnapshot> abilities, int skillPointsAvailable)
    {
        if (skillPointsAvailable <= 0)
        {
            return LevelUpRecommendationSnapshot.Empty;
        }

        var bySlot = abilities.ToDictionary(x => x.Slot, StringComparer.OrdinalIgnoreCase);
        if (bySlot.TryGetValue("R", out var ultimate) && ultimate.AbilityLevel < 3)
        {
            var unlockLevel = ultimate.AbilityLevel switch
            {
                0 => 6,
                1 => 11,
                2 => 16,
                _ => int.MaxValue
            };

            var spentLevels = abilities.Where(x => x.Slot is "Q" or "W" or "E" or "R").Sum(x => x.AbilityLevel);
            var simulatedLevel = spentLevels + skillPointsAvailable;
            if (simulatedLevel >= unlockLevel)
            {
                return new LevelUpRecommendationSnapshot(
                    Slot: "R",
                    DisplayName: ultimate.DisplayName,
                    Reason: "已到大招关键等级，优先补 R。",
                    HasAvailableSkillPoint: true);
            }
        }

        var primaries = abilities
            .Where(x => PrimarySlots.Contains(x.Slot, StringComparer.OrdinalIgnoreCase))
            .OrderByDescending(x => x.AbilityLevel)
            .ThenBy(x => Array.IndexOf(PrimarySlots, x.Slot))
            .ToArray();

        var preferred = primaries.FirstOrDefault(x => x.AbilityLevel > 0)
                        ?? primaries.FirstOrDefault()
                        ?? abilities.FirstOrDefault(x => x.Slot != "Passive");

        if (preferred is null)
        {
            return new LevelUpRecommendationSnapshot("Q", "Q", "尚未读取到技能数据，先默认推荐 Q。", true);
        }

        var reason = preferred.AbilityLevel > 0
            ? $"根据当前加点趋势，优先继续补 {preferred.Slot}。"
            : "当前还没有明显加点趋势，默认先从 Q 起手。";

        return new LevelUpRecommendationSnapshot(
            Slot: preferred.Slot,
            DisplayName: preferred.DisplayName,
            Reason: reason,
            HasAvailableSkillPoint: true);
    }

    private static IReadOnlyList<EventSnapshot> MapEvents(JsonElement? eventsContainer)
    {
        if (eventsContainer is null || !eventsContainer.Value.TryGetPropertyIgnoreCase("Events", out var eventsElement) || eventsElement.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<EventSnapshot>();
        }

        var list = new List<EventSnapshot>();

        foreach (var item in eventsElement.EnumerateArray())
        {
            var eventId = item.GetInt32OrDefault("EventID");
            var eventName = item.GetStringOrNull("EventName") ?? "UnknownEvent";
            var eventTime = item.GetDoubleOrDefault("EventTime");
            list.Add(new EventSnapshot(
                EventId: eventId,
                EventName: eventName,
                EventTimeSeconds: eventTime,
                Summary: BuildEventSummary(item, eventName)));
        }

        return list
            .OrderByDescending(x => x.EventId)
            .Take(12)
            .OrderBy(x => x.EventId)
            .ToArray();
    }

    private static ObjectivesSnapshot MapObjectives(JsonElement? eventsContainer, IReadOnlyDictionary<string, string> playerTeamMap)
    {
        var dragon = EpicObjectiveSnapshot.Create("dragon", "小龙");
        var elder = EpicObjectiveSnapshot.Create("elder", "远古龙");
        var baron = EpicObjectiveSnapshot.Create("baron", "大龙");
        var herald = EpicObjectiveSnapshot.Create("herald", "峡谷先锋");

        if (eventsContainer is null || !eventsContainer.Value.TryGetPropertyIgnoreCase("Events", out var eventsElement) || eventsElement.ValueKind != JsonValueKind.Array)
        {
            return new ObjectivesSnapshot(dragon, elder, baron, herald);
        }

        foreach (var item in eventsElement.EnumerateArray())
        {
            var eventName = item.GetStringOrNull("EventName") ?? string.Empty;
            var killerName = item.GetStringOrNull("KillerName");
            var dragonType = item.GetStringOrNull("DragonType") ?? string.Empty;
            var eventTime = item.GetDoubleOrDefault("EventTime");
            var killerTeam = ResolveKillerTeam(playerTeamMap, killerName);
            var killerSide = MapTeamDisplayName(killerTeam);

            switch (eventName)
            {
                case "DragonKill":
                    if (dragonType.Equals("Elder", StringComparison.OrdinalIgnoreCase) || dragonType.Contains("elder", StringComparison.OrdinalIgnoreCase))
                    {
                        elder = ApplyObjectiveKill(elder, killerTeam, $"{killerSide} 击杀远古龙", eventTime);
                    }
                    else
                    {
                        var dragonLabel = string.IsNullOrWhiteSpace(dragonType) ? "小龙" : $"{dragonType} 龙";
                        dragon = ApplyObjectiveKill(dragon, killerTeam, $"{killerSide} 击杀 {dragonLabel}", eventTime);
                    }
                    break;

                case "HeraldKill":
                    herald = ApplyObjectiveKill(herald, killerTeam, $"{killerSide} 击杀峡谷先锋", eventTime);
                    break;

                case "BaronKill":
                    baron = ApplyObjectiveKill(baron, killerTeam, $"{killerSide} 击杀大龙", eventTime);
                    break;
            }
        }

        return new ObjectivesSnapshot(dragon, elder, baron, herald);
    }

    private static EpicObjectiveSnapshot ApplyObjectiveKill(EpicObjectiveSnapshot snapshot, string? killerTeam, string summary, double eventTime)
    {
        var orderCount = snapshot.OrderCount;
        var chaosCount = snapshot.ChaosCount;

        if (string.Equals(killerTeam, "ORDER", StringComparison.OrdinalIgnoreCase))
        {
            orderCount++;
        }
        else if (string.Equals(killerTeam, "CHAOS", StringComparison.OrdinalIgnoreCase))
        {
            chaosCount++;
        }

        return snapshot with
        {
            OrderCount = orderCount,
            ChaosCount = chaosCount,
            LatestSummary = summary,
            LastEventTimeSeconds = eventTime
        };
    }

    private static IReadOnlyDictionary<string, string> BuildPlayerTeamMap(JsonElement? allPlayers)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (allPlayers is null || allPlayers.Value.ValueKind != JsonValueKind.Array)
        {
            return map;
        }

        foreach (var player in allPlayers.Value.EnumerateArray())
        {
            var team = player.GetStringOrNull("team") ?? string.Empty;
            var riotId = player.GetStringOrNull("riotId") ?? BuildRiotId(player);
            var summonerName = player.GetStringOrNull("summonerName");
            if (!string.IsNullOrWhiteSpace(riotId))
            {
                map[riotId] = team;
            }

            if (!string.IsNullOrWhiteSpace(summonerName))
            {
                map[summonerName] = team;
            }
        }

        return map;
    }

    private static string? ResolveKillerTeam(IReadOnlyDictionary<string, string> playerTeamMap, string? killerName)
    {
        if (string.IsNullOrWhiteSpace(killerName))
        {
            return null;
        }

        return playerTeamMap.TryGetValue(killerName, out var team) ? team : null;
    }

    private static string BuildEventSummary(JsonElement item, string eventName)
    {
        var killer = item.GetStringOrNull("KillerName");
        var victim = item.GetStringOrNull("VictimName");
        var dragonType = item.GetStringOrNull("DragonType");
        var laneType = item.GetStringOrNull("LaneType");
        var turretType = item.GetStringOrNull("TurretKilled");
        var inhibitor = item.GetStringOrNull("InhibKilled");

        return eventName switch
        {
            "ChampionKill" when !string.IsNullOrWhiteSpace(killer) && !string.IsNullOrWhiteSpace(victim) => $"{killer} 击杀了 {victim}",
            "Multikill" when !string.IsNullOrWhiteSpace(killer) => $"{killer} 触发多杀",
            "DragonKill" when !string.IsNullOrWhiteSpace(dragonType) => $"击杀了 {dragonType} 龙",
            "BaronKill" when !string.IsNullOrWhiteSpace(killer) => $"{killer} 击杀了大龙",
            "HeraldKill" when !string.IsNullOrWhiteSpace(killer) => $"{killer} 击杀了峡谷先锋",
            "TurretKilled" => $"摧毁防御塔：{laneType ?? "?"} / {turretType ?? "?"}",
            "InhibKilled" => $"摧毁水晶：{inhibitor ?? "?"}",
            _ => eventName
        };
    }

    private static JsonElement? FindMatchingPlayer(JsonElement? allPlayers, string riotId, JsonElement? activePlayer)
    {
        if (allPlayers is null || allPlayers.Value.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var activeGameName = activePlayer?.GetStringOrNull("riotIdGameName");
        var activeTagLine = activePlayer?.GetStringOrNull("riotIdTagLine");

        foreach (var player in allPlayers.Value.EnumerateArray())
        {
            var candidateRiotId = player.GetStringOrNull("riotId") ?? BuildRiotId(player);
            if (!string.IsNullOrWhiteSpace(candidateRiotId) && string.Equals(candidateRiotId, riotId, StringComparison.OrdinalIgnoreCase))
            {
                return player;
            }

            var gameName = player.GetStringOrNull("riotIdGameName");
            var tagLine = player.GetStringOrNull("riotIdTagLine");
            if (!string.IsNullOrWhiteSpace(activeGameName) && !string.IsNullOrWhiteSpace(gameName) &&
                string.Equals(activeGameName, gameName, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(activeTagLine ?? string.Empty, tagLine ?? string.Empty, StringComparison.OrdinalIgnoreCase))
            {
                return player;
            }

            var summonerName = player.GetStringOrNull("summonerName");
            if (!string.IsNullOrWhiteSpace(summonerName) && string.Equals(summonerName, riotId, StringComparison.OrdinalIgnoreCase))
            {
                return player;
            }
        }

        return null;
    }

    private static string MapTeamDisplayName(string? team)
    {
        return team?.ToUpperInvariant() switch
        {
            "ORDER" => "蓝方",
            "CHAOS" => "红方",
            _ => "未知"
        };
    }

    private static string? BuildRiotId(JsonElement? element)
    {
        if (element is null)
        {
            return null;
        }

        var riotId = element.Value.GetStringOrNull("riotId");
        if (!string.IsNullOrWhiteSpace(riotId))
        {
            return riotId;
        }

        var gameName = element.Value.GetStringOrNull("riotIdGameName");
        var tagLine = element.Value.GetStringOrNull("riotIdTagLine");
        if (string.IsNullOrWhiteSpace(gameName))
        {
            return element.Value.GetStringOrNull("summonerName");
        }

        return string.IsNullOrWhiteSpace(tagLine) ? gameName : $"{gameName}#{tagLine}";
    }
}
