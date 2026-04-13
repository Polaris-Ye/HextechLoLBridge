using HextechLoLBridge.Core.Models;

namespace HextechLoLBridge.Core.Services;

public sealed class NoopLightingService : ILightingService
{
    private readonly IAppLogger _logger;
    private bool _hasLogged;

    public NoopLightingService(IAppLogger logger)
    {
        _logger = logger;
        Status = LogitechSdkStatusSnapshot.NotStarted with
        {
            AdapterState = "noop",
            Message = "当前仍是空实现占位，不会实际输出灯效。"
        };
    }

    public LogitechSdkStatusSnapshot Status { get; }

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (!_hasLogged)
        {
            _logger.Info(Status.Message);
            _hasLogged = true;
        }

        return Task.CompletedTask;
    }

    public Task ApplyPlaceholderFrameAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task ApplyThemeHexAsync(string hex, string label, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task ApplySnapshotAsync(LeagueSnapshot snapshot, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task ShutdownAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
