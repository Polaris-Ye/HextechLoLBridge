namespace HextechLoLBridge.Core.Models;

public sealed record KeyMappingEntrySnapshot(
    string ActionId,
    string DisplayName,
    string DefaultKey,
    string CurrentKey,
    string AccentHex,
    string Group);
