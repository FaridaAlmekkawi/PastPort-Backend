using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PastPort.Application.Interfaces;
using System.Text;
using System.Text.Json;

namespace PastPort.API.Controllers;

[Authorize]
public class NpcController : BaseApiController
{
    private readonly INpcAIService _npcAIService;
    private readonly INpcSessionStore _sessionStore;   // ✅ جديد
    private readonly ILogger<NpcController> _logger;

    public NpcController(
        INpcAIService npcAIService,
        INpcSessionStore sessionStore,                 // ✅ جديد
        ILogger<NpcController> logger)
    {
        _npcAIService = npcAIService;
        _sessionStore = sessionStore;                  // ✅ جديد
        _logger = logger;
    }

    // ============================================================
    // FLUTTER → بيبعت world data ويستلم sessionId
    // ============================================================
    [HttpPost("session/start")]
    public IActionResult StartSession([FromBody] StartSessionRequest request)
    {
        var sessionData = new NpcSessionData
        {
            YearRange = request.YearRange,
            LocationOldName = request.LocationOldName,
            Civilization = request.Civilization
        };

        // ✅ خزّن في Memory وجيب sessionId
        var sessionId = _sessionStore.CreateSession(sessionData);

        _logger.LogInformation(
            "Session created: {SessionId} | Civilization: {Civ}",
            sessionId, request.Civilization);

        return Ok(new { sessionId });
    }

    // ============================================================
    // UNITY → بيبعت audio + sessionId + roleOrName بس
    // ============================================================
    [HttpPost("stream")]
    public async Task Stream(
        [FromForm] IFormFile audio,
        [FromForm] string sessionId,
        [FromForm] string roleOrName,          // ✅ Unity بيبعت ده بس
        CancellationToken cancellationToken)
    {
        // ✅ جيب الـ world data من Memory
        var sessionData = _sessionStore.GetSession(sessionId);

        if (sessionData == null)
        {
            Response.StatusCode = 404;
            await Response.WriteAsync("Session not found or expired");
            return;
        }

        if (audio == null || audio.Length == 0)
        {
            Response.StatusCode = 400;
            await Response.WriteAsync("Audio is required");
            return;
        }

        // ✅ دمج session data مع roleOrName من Unity
        var world = new NpcWorldDto
        {
            YearRange = sessionData.YearRange,
            LocationOldName = sessionData.LocationOldName,
            Civilization = sessionData.Civilization,
            RoleOrName = roleOrName
        };

        _logger.LogInformation(
            "NPC stream - Session: {SessionId} | Role: {Role} | Civ: {Civ}",
            sessionId, roleOrName, world.Civilization);

        using var ms = new MemoryStream();
        await audio.CopyToAsync(ms, cancellationToken);
        var audioBytes = ms.ToArray();

        Response.ContentType = "application/x-ndjson";
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("X-Accel-Buffering", "no");

        await foreach (var chunk in _npcAIService.SendAudioAndGetResponseAsync(
            audioBytes, world, sessionId, cancellationToken))
        {
            var json = JsonSerializer.Serialize(chunk,
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                });

            await Response.WriteAsync(json + "\n", Encoding.UTF8, cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
    }

    // ============================================================
    // إنهاء الـ Session
    // ============================================================
    [HttpPost("session/end")]
    public IActionResult EndSession([FromBody] EndSessionRequest request)
    {
        _sessionStore.RemoveSession(request.SessionId);
        _logger.LogInformation("Session ended: {SessionId}", request.SessionId);
        return Ok(new { message = "Session ended" });
    }
}

// ✅ عدّلت StartSessionRequest عشان يستقبل كل الـ world data
public class StartSessionRequest
{
    public string YearRange { get; set; } = string.Empty;
    public string LocationOldName { get; set; } = string.Empty;
    public string Civilization { get; set; } = string.Empty;
}

public class EndSessionRequest
{
    public string SessionId { get; set; } = string.Empty;
}