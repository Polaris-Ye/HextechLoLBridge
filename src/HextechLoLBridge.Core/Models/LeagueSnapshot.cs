namespace HextechLoLBridge.Core.Models;

public sealed record LeagueSnapshot(
    bool InGame,
    string ConnectionState,
    DateTimeOffset CapturedAt,
    GameSnapshot Game,
    PlayerSnapshot ActivePlayer,
    ObjectivesSnapshot Objectives,
    IReadOnlyList<EventSnapshot> RecentEvents,
    IReadOnlyList<string> Notes,
    ClientPhaseSnapshot ClientPhase)
{
    public static LeagueSnapshot Disconnected(string connectionState, params string[] notes) => new(
        InGame: false,
        ConnectionState: connectionState,
        CapturedAt: DateTimeOffset.Now,
        Game: GameSnapshot.Empty,
        ActivePlayer: PlayerSnapshot.Empty,
        Objectives: ObjectivesSnapshot.Empty,
        RecentEvents: Array.Empty<EventSnapshot>(),
        Notes: notes,
        ClientPhase: ClientPhaseSnapshot.Empty);
}
