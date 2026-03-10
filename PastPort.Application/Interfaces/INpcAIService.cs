namespace PastPort.Application.Interfaces;

public interface INpcAIService
{
    IAsyncEnumerable<NpcStreamChunk> SendAudioAndGetResponseAsync(
        byte[] audioBytes,
        NpcWorldDto world,
        string sessionId,
        CancellationToken cancellationToken);
}


public interface INpcSessionStore
{
    string CreateSession(NpcSessionData data);
    NpcSessionData? GetSession(string sessionId);
    void RemoveSession(string sessionId);
}


public class NpcSessionData
{
    public string YearRange { get; set; } = string.Empty;
    public string LocationOldName { get; set; } = string.Empty;
    public string Civilization { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class NpcWorldDto
{
    public string YearRange { get; set; } = string.Empty;
    public string LocationOldName { get; set; } = string.Empty;
    public string Civilization { get; set; } = string.Empty;
    public string RoleOrName { get; set; } = string.Empty;
}

public class NpcStreamChunk
{
    public string AudioChunk { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string Emotion { get; set; } = string.Empty;
    public float Amplitude { get; set; }
    public int CurrentYear { get; set; }
}