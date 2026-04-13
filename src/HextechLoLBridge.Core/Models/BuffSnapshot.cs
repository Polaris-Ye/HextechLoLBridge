namespace HextechLoLBridge.Core.Models;

public sealed record BuffSnapshot(
    string DisplayName,
    string RawId,
    string Category,
    int Count = 0,
    double? DurationSeconds = null);
