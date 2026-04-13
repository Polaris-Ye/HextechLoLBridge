namespace HextechLoLBridge.Core.Models;

public sealed record PollingRuntimeStatus(
    bool IsRunning,
    string State,
    int PollCount,
    DateTimeOffset? LastSuccessfulPollAt,
    string? LastError)
{
    public static PollingRuntimeStatus Idle { get; } = new(false, "idle", 0, null, null);
}
