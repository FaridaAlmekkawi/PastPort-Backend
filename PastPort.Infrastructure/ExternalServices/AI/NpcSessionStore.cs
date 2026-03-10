

using System.Collections.Concurrent;
using PastPort.Application.Interfaces;

namespace PastPort.Infrastructure.ExternalServices.AI;

public class NpcSessionStore : INpcSessionStore
{
    private readonly ConcurrentDictionary<string, NpcSessionData> _sessions = new();

    public string CreateSession(NpcSessionData data)
    {
        var sessionId = Guid.NewGuid().ToString();
        data.CreatedAt = DateTime.UtcNow;
        _sessions[sessionId] = data;
        return sessionId;
    }

    public NpcSessionData? GetSession(string sessionId)
    {
        _sessions.TryGetValue(sessionId, out var data);

        // تحقق إن الـ session مش expired (ساعة)
        if (data != null && DateTime.UtcNow - data.CreatedAt > TimeSpan.FromHours(1))
        {
            _sessions.TryRemove(sessionId, out _);
            return null;
        }

        return data;
    }

    public void RemoveSession(string sessionId)
    {
        _sessions.TryRemove(sessionId, out _);
    }
}