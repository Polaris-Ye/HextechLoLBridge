namespace HextechLoLBridge.Core.Models;

public sealed record ClientPhaseSnapshot(
    bool IsClientConnected,
    string Phase,
    string PhaseDisplayName,
    bool CanAcceptReadyCheck,
    string ReadyCheckState,
    string ReadyCheckPlayerResponse,
    int? ReadyCheckTimerSeconds,
    bool IsChampSelect,
    int? ChampSelectCountdownSeconds,
    string ChampSelectTimerPhase,
    IReadOnlyList<SummonerSpellSnapshot> SummonerSpells)
{
    public static ClientPhaseSnapshot Empty { get; } = new(
        IsClientConnected: false,
        Phase: "Disconnected",
        PhaseDisplayName: "客户端未连接",
        CanAcceptReadyCheck: false,
        ReadyCheckState: "-",
        ReadyCheckPlayerResponse: "-",
        ReadyCheckTimerSeconds: null,
        IsChampSelect: false,
        ChampSelectCountdownSeconds: null,
        ChampSelectTimerPhase: "-",
        SummonerSpells: Array.Empty<SummonerSpellSnapshot>());
}
