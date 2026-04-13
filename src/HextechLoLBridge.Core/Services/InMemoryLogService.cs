using System.Collections.Concurrent;
using HextechLoLBridge.Core.Models;

namespace HextechLoLBridge.Core.Services;

public sealed class InMemoryLogService : IAppLogger
{
    private readonly ConcurrentQueue<LogEntry> _entries = new();
    private readonly int _capacity;

    public InMemoryLogService(int capacity = 200)
    {
        _capacity = Math.Max(20, capacity);
    }

    public void Info(string message) => Add("INFO", message);

    public void Warn(string message) => Add("WARN", message);

    public void Error(string message) => Add("ERROR", message);

    public IReadOnlyList<LogEntry> GetEntries() => _entries.ToArray();

    public void Clear()
    {
        while (_entries.TryDequeue(out _))
        {
        }
    }

    private void Add(string level, string message)
    {
        _entries.Enqueue(new LogEntry(DateTimeOffset.Now, level, message));

        while (_entries.Count > _capacity && _entries.TryDequeue(out _))
        {
        }
    }
}
