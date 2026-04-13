namespace HextechLoLBridge.Core.Models;

public sealed record ObjectivesSnapshot(
    EpicObjectiveSnapshot Dragon,
    EpicObjectiveSnapshot ElderDragon,
    EpicObjectiveSnapshot Baron,
    EpicObjectiveSnapshot RiftHerald)
{
    public static ObjectivesSnapshot Empty { get; } = new(
        EpicObjectiveSnapshot.Create("dragon", "小龙"),
        EpicObjectiveSnapshot.Create("elder", "远古龙"),
        EpicObjectiveSnapshot.Create("baron", "大龙"),
        EpicObjectiveSnapshot.Create("herald", "峡谷先锋"));
}
