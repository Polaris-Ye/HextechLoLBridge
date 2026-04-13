namespace HextechLoLBridge.Core.Models;

public sealed record EpicObjectiveSnapshot(
    string ObjectiveId,
    string DisplayName,
    int OrderCount,
    int ChaosCount,
    string LatestSummary,
    double? LastEventTimeSeconds = null)
{
    public static EpicObjectiveSnapshot Create(string objectiveId, string displayName) => new(objectiveId, displayName, 0, 0, "暂无", null);
}
