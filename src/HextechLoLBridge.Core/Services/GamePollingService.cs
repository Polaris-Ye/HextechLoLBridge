using HextechLoLBridge.Core.Models;
using HextechLoLBridge.Core.Riot;

namespace HextechLoLBridge.Core.Services;

public sealed class GamePollingService
{
    private readonly RiotLiveClientService _riotLiveClientService;
    private readonly LeagueClientApiService _leagueClientApiService;
    private readonly LightingProfileService _profileService;
    private readonly IAppLogger _logger;
    private readonly TimeSpan _interval;
    private readonly SemaphoreSlim _stateGate = new(1, 1);

    private CancellationTokenSource? _runLoopCts;
    private Task? _runLoopTask;
    private LeagueSnapshot _lastSnapshot = LeagueSnapshot.Disconnected(
        "idle",
        "尚未开始轮询。",
        "本版已接入 League Client API，可读取大厅 / 排队 / Ready Check / 选人阶段。",
        "窗口聚焦时，Ready Check 阶段可以按空格或点击按钮接受对局。",
        "本版已开始接 Logitech LIGHTSYNC，待 SDK 初始化成功后会尝试输出灯效。",
        "Q/W/E/R 的实时冷却/就绪状态后续再补。");

    public GamePollingService(
        RiotLiveClientService riotLiveClientService,
        LeagueClientApiService leagueClientApiService,
        LightingProfileService profileService,
        IAppLogger logger,
        TimeSpan interval)
    {
        _riotLiveClientService = riotLiveClientService;
        _leagueClientApiService = leagueClientApiService;
        _profileService = profileService;
        _logger = logger;
        _interval = interval;
        Status = PollingRuntimeStatus.Idle;
    }

    public event EventHandler<LeagueSnapshot>? SnapshotUpdated;

    public event EventHandler<PollingRuntimeStatus>? StatusChanged;

    public PollingRuntimeStatus Status { get; private set; }

    public LeagueSnapshot LastSnapshot => _lastSnapshot;

    public async Task StartAsync()
    {
        await _stateGate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_runLoopTask is { IsCompleted: false })
            {
                return;
            }

            _runLoopCts = new CancellationTokenSource();
            Status = Status with { IsRunning = true, State = "running", LastError = null };
            PublishStatus();
            _logger.Info("开始轮询 LoL Live Client API 与 League Client API。");
            _runLoopTask = Task.Run(() => RunLoopAsync(_runLoopCts.Token));
        }
        finally
        {
            _stateGate.Release();
        }
    }

    public async Task StopAsync()
    {
        Task? runTask = null;

        await _stateGate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_runLoopCts is null)
            {
                return;
            }

            _runLoopCts.Cancel();
            runTask = _runLoopTask;
        }
        finally
        {
            _stateGate.Release();
        }

        if (runTask is not null)
        {
            try
            {
                await runTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        }

        await _stateGate.WaitAsync().ConfigureAwait(false);
        try
        {
            _runLoopTask = null;
            _runLoopCts?.Dispose();
            _runLoopCts = null;
            Status = Status with { IsRunning = false, State = "stopped" };
            PublishStatus();
            _logger.Info("已停止轮询。");
        }
        finally
        {
            _stateGate.Release();
        }
    }

    public async Task RefreshOnceAsync(CancellationToken cancellationToken = default)
    {
        var snapshot = await CaptureMergedSnapshotAsync(cancellationToken).ConfigureAwait(false);
        PublishSnapshot(snapshot);
    }

    private async Task RunLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var snapshot = await CaptureMergedSnapshotAsync(cancellationToken).ConfigureAwait(false);
                PublishSnapshot(snapshot);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Status = Status with { LastError = ex.Message, State = "faulted" };
                PublishStatus();
                _logger.Error($"轮询异常：{ex.Message}");
            }

            await Task.Delay(_interval, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<LeagueSnapshot> CaptureMergedSnapshotAsync(CancellationToken cancellationToken)
    {
        var spellOverrides = _profileService.GetSpellColorOverrides();
        var clientPhase = await _leagueClientApiService.CaptureSnapshotAsync(spellOverrides, cancellationToken).ConfigureAwait(false);

        LeagueSnapshot liveSnapshot;
        if (ShouldQueryLiveClient(clientPhase))
        {
            liveSnapshot = await _riotLiveClientService.CaptureSnapshotAsync(cancellationToken).ConfigureAwait(false);
        }
        else
        {
            liveSnapshot = LeagueSnapshot.Disconnected(
                "live-client-idle",
                "当前不在实际对局中，已跳过 Live Client API 轮询。",
                "大厅、排队、Ready Check、选人等阶段优先读取 League Client API。");
        }

        return MergeSnapshots(liveSnapshot, clientPhase);
    }

    private static bool ShouldQueryLiveClient(ClientPhaseSnapshot clientPhase)
    {
        if (!clientPhase.IsClientConnected)
        {
            return true;
        }

        return clientPhase.Phase is "InProgress" or "GameStart" or "Reconnect" or "WaitingForStats";
    }

    private static LeagueSnapshot MergeSnapshots(LeagueSnapshot liveSnapshot, ClientPhaseSnapshot clientPhase)
    {
        var effectiveClientPhase = clientPhase;
        if (liveSnapshot.InGame && (!clientPhase.IsClientConnected || clientPhase.Phase is "ReadyCheck" or "Lobby" or "Matchmaking" or "ChampSelect"))
        {
            effectiveClientPhase = clientPhase with
            {
                IsClientConnected = clientPhase.IsClientConnected,
                Phase = "InProgress",
                PhaseDisplayName = clientPhase.IsClientConnected ? "对局中（局内优先）" : "对局中（未连接客户端）",
                CanAcceptReadyCheck = false,
                IsChampSelect = false,
                ReadyCheckState = "-",
                ReadyCheckPlayerResponse = "-",
                ReadyCheckTimerSeconds = null
            };
        }

        var notes = liveSnapshot.Notes.ToList();
        if (effectiveClientPhase.IsClientConnected || liveSnapshot.InGame)
        {
            notes.Insert(0, $"客户端阶段：{effectiveClientPhase.PhaseDisplayName}。");
            if (effectiveClientPhase.CanAcceptReadyCheck)
            {
                notes.Insert(1, "当前可接受对局：窗口聚焦时可按空格，或在界面里点“接受对局”。");
            }
            if (effectiveClientPhase.IsChampSelect && effectiveClientPhase.SummonerSpells.Count > 0)
            {
                notes.Insert(1, "选人阶段已读取当前双招，会直接映射到 D / F 颜色。");
            }
        }

        var player = liveSnapshot.ActivePlayer;
        if (!liveSnapshot.InGame && effectiveClientPhase.SummonerSpells.Count > 0)
        {
            player = player with { SummonerSpells = effectiveClientPhase.SummonerSpells };
        }

        var connectionState = liveSnapshot.InGame
            ? liveSnapshot.ConnectionState
            : effectiveClientPhase.IsClientConnected
                ? effectiveClientPhase.Phase.ToLowerInvariant()
                : liveSnapshot.ConnectionState;

        return liveSnapshot with
        {
            ConnectionState = connectionState,
            ActivePlayer = player,
            Notes = notes,
            ClientPhase = effectiveClientPhase,
            CapturedAt = DateTimeOffset.Now
        };
    }

    private void PublishSnapshot(LeagueSnapshot snapshot)
    {
        _lastSnapshot = snapshot;

        var nextPollCount = Status.PollCount + 1;
        Status = Status with
        {
            PollCount = nextPollCount,
            State = snapshot.InGame ? "in-game" : snapshot.ClientPhase.IsClientConnected ? snapshot.ClientPhase.Phase.ToLowerInvariant() : snapshot.ConnectionState,
            LastSuccessfulPollAt = snapshot.CapturedAt,
            LastError = snapshot.InGame || snapshot.ClientPhase.IsClientConnected ? null : Status.LastError
        };

        PublishStatus();
        SnapshotUpdated?.Invoke(this, snapshot);
    }

    private void PublishStatus() => StatusChanged?.Invoke(this, Status);
}
