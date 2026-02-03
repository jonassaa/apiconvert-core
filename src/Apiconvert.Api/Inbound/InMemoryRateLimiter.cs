namespace Apiconvert.Api.Inbound;

public sealed class InMemoryRateLimiter : IRateLimiter
{
    private readonly Dictionary<string, (long WindowStart, int Count)> _state = new();
    private readonly object _lock = new();

    public bool IsRateLimited(string key)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        lock (_lock)
        {
            if (!_state.TryGetValue(key, out var entry) || now - entry.WindowStart > InboundConstants.RateLimitWindowMs)
            {
                _state[key] = (now, 1);
                return false;
            }

            entry.Count += 1;
            _state[key] = entry;
            return entry.Count > InboundConstants.RateLimitMaxRequests;
        }
    }
}
