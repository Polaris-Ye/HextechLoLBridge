namespace HextechLoLBridge.Core.Models;

public sealed record LogEntry(DateTimeOffset Timestamp, string Level, string Message);
