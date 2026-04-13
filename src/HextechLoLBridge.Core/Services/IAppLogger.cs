using HextechLoLBridge.Core.Models;

namespace HextechLoLBridge.Core.Services;

public interface IAppLogger
{
    void Info(string message);
    void Warn(string message);
    void Error(string message);
    IReadOnlyList<LogEntry> GetEntries();
    void Clear();
}
