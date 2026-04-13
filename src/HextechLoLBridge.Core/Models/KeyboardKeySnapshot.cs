namespace HextechLoLBridge.Core.Models;

public sealed record KeyboardKeySnapshot(
    string KeyCode,
    string DisplayName,
    int Row,
    int WidthUnits = 1,
    bool IsSpacer = false);
