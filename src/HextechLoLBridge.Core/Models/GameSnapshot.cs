namespace HextechLoLBridge.Core.Models;

public sealed record GameSnapshot(
    string GameMode,
    string MapName,
    int MapNumber,
    string MapTerrain,
    double GameTimeSeconds)
{
    public static GameSnapshot Empty { get; } = new("-", "-", 0, "-", 0);
}
