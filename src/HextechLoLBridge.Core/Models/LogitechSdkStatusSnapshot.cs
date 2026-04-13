namespace HextechLoLBridge.Core.Models;

public sealed record LogitechSdkStatusSnapshot(
    bool DllFound,
    bool IsInitialized,
    string AdapterState,
    string Message,
    string? DllProbePath = null,
    string? ActiveEffect = null,
    string? ActiveHex = null,
    DateTimeOffset? LastAppliedAt = null)
{
    public static LogitechSdkStatusSnapshot NotStarted { get; } = new(
        DllFound: false,
        IsInitialized: false,
        AdapterState: "idle",
        Message: "尚未尝试初始化 Logitech LED SDK。",
        DllProbePath: null,
        ActiveEffect: "未应用",
        ActiveHex: null,
        LastAppliedAt: null);
}
