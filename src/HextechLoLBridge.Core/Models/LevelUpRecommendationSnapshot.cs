namespace HextechLoLBridge.Core.Models;

public sealed record LevelUpRecommendationSnapshot(
    string Slot,
    string DisplayName,
    string Reason,
    bool HasAvailableSkillPoint)
{
    public static LevelUpRecommendationSnapshot Empty { get; } = new("-", "-", "当前没有可分配技能点。", false);
}
