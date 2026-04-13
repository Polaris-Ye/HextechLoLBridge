using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using HextechLoLBridge.Core.Catalog;
using HextechLoLBridge.Core.Models;
using HextechLoLBridge.Core.Services;

namespace HextechLoLBridge.Core.Riot;

public sealed class LeagueClientApiService
{
    private readonly IAppLogger _logger;
    private HttpClient? _httpClient;
    private string? _currentLockfileSignature;

    public LeagueClientApiService(IAppLogger logger)
    {
        _logger = logger;
    }

    public async Task<ClientPhaseSnapshot> CaptureSnapshotAsync(IReadOnlyDictionary<string, string>? spellOverrides = null, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!TryEnsureClient(out var reason))
            {
                return ClientPhaseSnapshot.Empty with { PhaseDisplayName = reason };
            }

            var phase = await GetStringAsync("lol-gameflow/v1/gameflow-phase", cancellationToken).ConfigureAwait(false) ?? "None";
            var readyCheck = phase.Equals("ReadyCheck", StringComparison.OrdinalIgnoreCase)
                ? await GetJsonAsync("lol-matchmaking/v1/ready-check", cancellationToken).ConfigureAwait(false)
                : null;
            var champSelect = phase.Equals("ChampSelect", StringComparison.OrdinalIgnoreCase)
                ? await GetJsonAsync("lol-champ-select/v1/session", cancellationToken).ConfigureAwait(false)
                : null;

            return MapClientPhase(phase, readyCheck, champSelect, spellOverrides);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.Warn($"读取 League Client API 失败：{ex.Message}");
            return ClientPhaseSnapshot.Empty with { PhaseDisplayName = "客户端 API 暂不可用" };
        }
    }

    public async Task<(bool Success, string Message)> AcceptReadyCheckAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!TryEnsureClient(out var reason))
            {
                return (false, reason);
            }

            using var response = await _httpClient!.PostAsync("lol-matchmaking/v1/ready-check/accept", new StringContent("{}", Encoding.UTF8, "application/json"), cancellationToken).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                return (true, "已向 League Client 发送接受对局请求。");
            }

            return (false, $"接受对局失败：HTTP {(int)response.StatusCode}");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return (false, $"接受对局失败：{ex.Message}");
        }
    }

    private ClientPhaseSnapshot MapClientPhase(string phase, JsonElement? readyCheck, JsonElement? champSelect, IReadOnlyDictionary<string, string>? spellOverrides)
    {
        var canAccept = false;
        var readyState = "-";
        var playerResponse = "-";
        int? readyTimer = null;

        if (readyCheck is not null)
        {
            readyState = readyCheck.Value.GetStringOrNull("state") ?? "-";
            playerResponse = readyCheck.Value.GetStringOrNull("playerResponse") ?? "-";
            readyTimer = NormalizeTimerSeconds(readyCheck.Value.GetNullableDouble("timer", "timerInSeconds", "countdown", "secondsRemaining"));
            canAccept = readyState.Equals("InProgress", StringComparison.OrdinalIgnoreCase)
                        && playerResponse.Equals("None", StringComparison.OrdinalIgnoreCase);
        }

        var champSelectTimerPhase = "-";
        int? champCountdown = null;
        IReadOnlyList<SummonerSpellSnapshot> champSpells = Array.Empty<SummonerSpellSnapshot>();

        if (champSelect is not null)
        {
            var timer = champSelect.Value.GetPropertyOrNull("timer");
            champSelectTimerPhase = timer?.GetStringOrNull("phase") ?? timer?.GetStringOrNull("internalNowInEpochMs") ?? "-";
            champCountdown = NormalizeTimerSeconds(
                timer?.GetNullableDouble("adjustedTimeLeftInPhase", "timeLeftInPhase", "totalTimeInPhase", "millisecondsRemaining"));
            champSpells = MapChampSelectSpells(champSelect.Value, spellOverrides);
        }

        return new ClientPhaseSnapshot(
            IsClientConnected: true,
            Phase: phase,
            PhaseDisplayName: ToPhaseDisplayName(phase),
            CanAcceptReadyCheck: canAccept,
            ReadyCheckState: readyState,
            ReadyCheckPlayerResponse: playerResponse,
            ReadyCheckTimerSeconds: readyTimer,
            IsChampSelect: phase.Equals("ChampSelect", StringComparison.OrdinalIgnoreCase),
            ChampSelectCountdownSeconds: champCountdown,
            ChampSelectTimerPhase: champSelectTimerPhase,
            SummonerSpells: champSpells);
    }

    private IReadOnlyList<SummonerSpellSnapshot> MapChampSelectSpells(JsonElement session, IReadOnlyDictionary<string, string>? spellOverrides)
    {
        var localPlayerCellId = session.GetInt32OrDefault("localPlayerCellId", -1);
        var myTeam = session.GetPropertyOrNull("myTeam");
        if (myTeam is null || myTeam.Value.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<SummonerSpellSnapshot>();
        }

        JsonElement? localPlayer = null;
        foreach (var item in myTeam.Value.EnumerateArray())
        {
            if (item.GetInt32OrDefault("cellId", -1) == localPlayerCellId)
            {
                localPlayer = item;
                break;
            }
        }

        if (localPlayer is null)
        {
            localPlayer = myTeam.Value.EnumerateArray().FirstOrDefault();
            if (localPlayer.Value.ValueKind == JsonValueKind.Undefined)
            {
                return Array.Empty<SummonerSpellSnapshot>();
            }
        }

        var spell1Id = localPlayer.Value.GetNullableInt32("spell1Id", "summonerSpell1Id", "summoner1Id");
        var spell2Id = localPlayer.Value.GetNullableInt32("spell2Id", "summonerSpell2Id", "summoner2Id");
        var list = new List<SummonerSpellSnapshot>();
        if (spell1Id.HasValue && spell1Id.Value > 0)
        {
            list.Add(SummonerSpellThemeCatalog.Enrich(SummonerSpellThemeCatalog.FromSpellId(spell1Id.Value, "双招一", spellOverrides), spellOverrides));
        }
        if (spell2Id.HasValue && spell2Id.Value > 0)
        {
            list.Add(SummonerSpellThemeCatalog.Enrich(SummonerSpellThemeCatalog.FromSpellId(spell2Id.Value, "双招二", spellOverrides), spellOverrides));
        }

        return list;
    }

    private bool TryEnsureClient(out string reason)
    {
        var lockfile = TryReadLockfile();
        if (lockfile is null)
        {
            reason = "未找到 League Client lockfile";
            _httpClient = null;
            _currentLockfileSignature = null;
            return false;
        }

        var signature = $"{lockfile.Protocol}:{lockfile.Port}:{lockfile.Password}";
        if (_httpClient is not null && signature == _currentLockfileSignature)
        {
            reason = string.Empty;
            return true;
        }

        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        };

        _httpClient?.Dispose();
        _httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri($"{lockfile.Protocol}://127.0.0.1:{lockfile.Port}/"),
            Timeout = TimeSpan.FromSeconds(2)
        };
        var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"riot:{lockfile.Password}"));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", basic);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _currentLockfileSignature = signature;
        reason = string.Empty;
        return true;
    }

    private async Task<string?> GetStringAsync(string path, CancellationToken cancellationToken)
    {
        using var response = await _httpClient!.GetAsync(path, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var raw = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return raw.Trim().Trim('"');
    }

    private async Task<JsonElement?> GetJsonAsync(string path, CancellationToken cancellationToken)
    {
        using var response = await _httpClient!.GetAsync(path, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        return document.RootElement.Clone();
    }

    private static int? NormalizeTimerSeconds(double? raw)
    {
        if (!raw.HasValue)
        {
            return null;
        }

        var value = raw.Value;
        if (value > 1000)
        {
            value /= 1000d;
        }

        return Math.Max(0, (int)Math.Ceiling(value));
    }

    private static string ToPhaseDisplayName(string phase)
        => phase switch
        {
            "Lobby" => "大厅",
            "Matchmaking" => "排队中",
            "ReadyCheck" => "准备确认",
            "ChampSelect" => "选人中",
            "GameStart" => "进入对局",
            "InProgress" => "对局中",
            "Reconnect" => "重连中",
            "WaitingForStats" => "等待结算",
            "PreEndOfGame" => "结算前",
            "EndOfGame" => "结算中",
            _ => phase
        };

    private LeagueClientLockfile? TryReadLockfile()
    {
        foreach (var candidate in GetLockfileCandidates())
        {
            try
            {
                if (!File.Exists(candidate))
                {
                    continue;
                }

                var content = File.ReadAllText(candidate).Trim();
                if (string.IsNullOrWhiteSpace(content))
                {
                    continue;
                }

                var parts = content.Split(':');
                if (parts.Length < 5)
                {
                    continue;
                }

                if (!int.TryParse(parts[2], out var port))
                {
                    continue;
                }

                return new LeagueClientLockfile(candidate, port, parts[3], parts[4]);
            }
            catch
            {
            }
        }

        return null;
    }

    private static IEnumerable<string> GetLockfileCandidates()
    {
        var yielded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var candidates = new List<string>();

        foreach (var processName in new[] { "LeagueClientUx", "LeagueClient" })
        {
            Process[] processes;
            try
            {
                processes = Process.GetProcessesByName(processName);
            }
            catch
            {
                continue;
            }

            foreach (var process in processes)
            {
                try
                {
                    var mainModule = process.MainModule;
                    var fileName = mainModule?.FileName;
                    if (string.IsNullOrWhiteSpace(fileName))
                    {
                        continue;
                    }

                    var directory = Path.GetDirectoryName(fileName);
                    if (string.IsNullOrWhiteSpace(directory))
                    {
                        continue;
                    }

                    var candidate = Path.Combine(directory, "lockfile");
                    if (yielded.Add(candidate))
                    {
                        candidates.Add(candidate);
                    }
                }
                catch
                {
                }
            }
        }

        foreach (var candidate in new[]
                 {
                     @"C:\\Riot Games\\League of Legends\\lockfile",
                     @"C:\\Program Files\\Riot Games\\League of Legends\\lockfile",
                     @"D:\\Riot Games\\League of Legends\\lockfile",
                     @"E:\\Riot Games\\League of Legends\\lockfile"
                 })
        {
            if (yielded.Add(candidate))
            {
                candidates.Add(candidate);
            }
        }

        return candidates;
    }

    private sealed record LeagueClientLockfile(string Path, int Port, string Password, string Protocol);
}
