using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PastPort.Application.Interfaces;


namespace PastPort.Infrastructure.ExternalServices.AI;

public class NpcAISettings
{
    public string BaseUrl { get; set; } = string.Empty;
}

public class NpcAIService : INpcAIService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NpcAIService> _logger;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public NpcAIService(
        HttpClient httpClient,
        IOptions<NpcAISettings> settings,
        ILogger<NpcAIService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _httpClient.BaseAddress = new Uri(settings.Value.BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(60);
    }

    public async IAsyncEnumerable<NpcStreamChunk> SendAudioAndGetResponseAsync(
        byte[] audioBytes,
        NpcWorldDto world,
        string sessionId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // تحويل الـ world object لـ JSON string
        var worldJson = JsonSerializer.Serialize(new
        {
            year_range = world.YearRange,
            location_old_name = world.LocationOldName,
            civilization = world.Civilization,
            role_or_name = world.RoleOrName
        });

        using var form = new MultipartFormDataContent();

        // audio file
        var audioContent = new ByteArrayContent(audioBytes);
        audioContent.Headers.ContentType =
            new MediaTypeHeaderValue("application/octet-stream");
        form.Add(audioContent, "audio", "audio.wav");

        // world كـ string مش object
        form.Add(new StringContent(worldJson), "world");

        // session_id
        form.Add(new StringContent(sessionId), "session_id");

        HttpResponseMessage response;

        try
        {
            response = await _httpClient.PostAsync("/npc/stream", form, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("NPC API error {Status}: {Error}", response.StatusCode, error);
                throw new Exception($"NPC API failed: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call NPC API for session {SessionId}", sessionId);
            throw;
        }

        // قراءة الـ Streaming JSON Lines
        var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream, Encoding.UTF8);

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(line))
                continue;

            NpcStreamChunk? chunk = null;

            try
            {
                chunk = JsonSerializer.Deserialize<NpcStreamChunk>(line, _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse chunk: {Line}", line);
                continue;
            }

            if (chunk != null)
                yield return chunk;
        }
    }

  
}