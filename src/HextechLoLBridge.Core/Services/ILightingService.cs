using HextechLoLBridge.Core.Models;

namespace HextechLoLBridge.Core.Services;

public interface ILightingService
{
    LogitechSdkStatusSnapshot Status { get; }
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task ApplyPlaceholderFrameAsync(CancellationToken cancellationToken = default);
    Task ApplyThemeHexAsync(string hex, string label, CancellationToken cancellationToken = default);
    Task ApplySnapshotAsync(LeagueSnapshot snapshot, CancellationToken cancellationToken = default);
    Task ShutdownAsync(CancellationToken cancellationToken = default);
}
