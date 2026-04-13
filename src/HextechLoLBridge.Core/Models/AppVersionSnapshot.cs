namespace HextechLoLBridge.Core.Models;

public sealed record AppVersionSnapshot(
    string Product,
    string Version,
    string Framework,
    string RuntimeDescription);
