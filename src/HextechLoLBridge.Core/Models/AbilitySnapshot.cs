namespace HextechLoLBridge.Core.Models;

public sealed record AbilitySnapshot(
    string Slot,
    string DisplayName,
    string AbilityId,
    int AbilityLevel,
    bool IsLearned,
    bool? IsReady = null,
    double? CooldownSeconds = null);
