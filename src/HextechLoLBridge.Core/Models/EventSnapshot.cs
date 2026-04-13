namespace HextechLoLBridge.Core.Models;

public sealed record EventSnapshot(
    int EventId,
    string EventName,
    double EventTimeSeconds,
    string Summary);
