using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PastPort.Application.DTOs.Request;
using PastPort.Application.Interfaces;

namespace PastPort.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ScenesController : BaseApiController
{
    private readonly ISceneService _sceneService;
    private readonly ILogger<ScenesController> _logger;

    public ScenesController(ISceneService sceneService, ILogger<ScenesController> logger)
    {
        _sceneService = sceneService;
        _logger = logger;
    }

    /// <summary>
    /// Get all historical scenes
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllScenes()
    {
        try
        {
            var scenes = await _sceneService.GetAllScenesAsync();
            return Ok(new { data = scenes, message = "Scenes retrieved successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve scenes");
            return HandleError(ex);
        }
    }

    /// <summary>
    /// Get scene by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSceneById(Guid id)
    {
        try
        {
            var scene = await _sceneService.GetSceneByIdAsync(id);
            return HandleResult(scene);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve scene {SceneId}", id);
            return HandleError(ex);
        }
    }

    /// <summary>
    /// Get scenes by era
    /// </summary>
    [HttpGet("era/{era}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetScenesByEra(string era)
    {
        try
        {
            var scenes = await _sceneService.GetScenesByEraAsync(era);
            return Ok(new { data = scenes, message = $"Scenes from {era} retrieved successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve scenes for era {Era}", era);
            return HandleError(ex);
        }
    }

    /// <summary>
    /// Search scenes
    /// </summary>
    [HttpGet("search")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchScenes([FromQuery] string searchTerm)
    {
        try
        {
            var scenes = await _sceneService.SearchScenesAsync(searchTerm);
            return Ok(new { data = scenes, message = "Search completed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search scenes with term {SearchTerm}", searchTerm);
            return HandleError(ex);
        }
    }

    /// <summary>
    /// Create new scene
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateScene([FromBody] CreateSceneRequestDto request)
    {
        try
        {
            var scene = await _sceneService.CreateSceneAsync(request);
            _logger.LogInformation("Scene created: {SceneId}", scene.Id);
            return CreatedAtAction(nameof(GetSceneById), new { id = scene.Id },
                new { data = scene, message = "Scene created successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create scene");
            return HandleError(ex);
        }
    }

    /// <summary>
    /// Update scene
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateScene(Guid id, [FromBody] UpdateSceneRequestDto request)
    {
        try
        {
            var scene = await _sceneService.UpdateSceneAsync(id, request);
            _logger.LogInformation("Scene updated: {SceneId}", id);
            return Ok(new { data = scene, message = "Scene updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update scene {SceneId}", id);
            return HandleError(ex);
        }
    }

    /// <summary>
    /// Delete scene
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteScene(Guid id)
    {
        try
        {
            await _sceneService.DeleteSceneAsync(id);
            _logger.LogInformation("Scene deleted: {SceneId}", id);
            return Ok(new { message = "Scene deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete scene {SceneId}", id);
            return HandleError(ex);
        }
    }
}