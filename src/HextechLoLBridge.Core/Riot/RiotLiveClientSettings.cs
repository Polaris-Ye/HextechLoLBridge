namespace HextechLoLBridge.Core.Riot;

public sealed record RiotLiveClientSettings(
    string BaseAddress = "https://127.0.0.1:2999/",
    TimeSpan Timeout = default)
{
    public TimeSpan EffectiveTimeout => Timeout == default ? TimeSpan.FromSeconds(2) : Timeout;
}
