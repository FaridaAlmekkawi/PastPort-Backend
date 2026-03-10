using PastPort.Application.DTOs.Request;
using PastPort.Application.DTOs.Response;
using PastPort.Application.Interfaces;
using PastPort.Domain.Entities;
using PastPort.Domain.Interfaces;

namespace PastPort.Application.Identity;

public class SceneService : ISceneService
{
    private readonly ISceneRepository _sceneRepository;

    public SceneService(ISceneRepository sceneRepository)
    {
        _sceneRepository = sceneRepository;
    }

    public async Task<SceneResponseDto> CreateSceneAsync(CreateSceneRequestDto request)
    {
        var scene = new HistoricalScene
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Era = request.Era,
            Location = request.Location,
            Description = request.Description,
            EnvironmentPrompt = request.EnvironmentPrompt,
            Model3DUrl = request.Model3DUrl ?? string.Empty,
            CreatedAt = DateTime.UtcNow
        };

        await _sceneRepository.AddAsync(scene);

        return MapToResponseDto(scene);
    }

    public async Task<SceneResponseDto> GetSceneByIdAsync(Guid id)
    {
        var scene = await _sceneRepository.GetSceneWithCharactersAsync(id);
        if (scene == null)
            throw new Exception("Scene not found");

        return MapToResponseDto(scene);
    }

    public async Task<List<SceneResponseDto>> GetAllScenesAsync()
    {
        var scenes = await _sceneRepository.GetAllAsync();
        return scenes.Select(MapToResponseDto).ToList();
    }

    public async Task<List<SceneResponseDto>> GetScenesByEraAsync(string era)
    {
        var scenes = await _sceneRepository.GetByEraAsync(era);
        return scenes.Select(MapToResponseDto).ToList();
    }

    public async Task<List<SceneResponseDto>> SearchScenesAsync(string searchTerm)
    {
        var scenes = await _sceneRepository.SearchScenesAsync(searchTerm);
        return scenes.Select(MapToResponseDto).ToList();
    }

    public async Task<SceneResponseDto> UpdateSceneAsync(Guid id, UpdateSceneRequestDto request)
    {
        var scene = await _sceneRepository.GetByIdAsync(id);
        if (scene == null)
            throw new Exception("Scene not found");

        if (!string.IsNullOrEmpty(request.Title))
            scene.Title = request.Title;

        if (!string.IsNullOrEmpty(request.Era))
            scene.Era = request.Era;

        if (!string.IsNullOrEmpty(request.Location))
            scene.Location = request.Location;

        if (!string.IsNullOrEmpty(request.Description))
            scene.Description = request.Description;

        if (!string.IsNullOrEmpty(request.EnvironmentPrompt))
            scene.EnvironmentPrompt = request.EnvironmentPrompt;

        if (request.Model3DUrl != null)
            scene.Model3DUrl = request.Model3DUrl;

        scene.UpdatedAt = DateTime.UtcNow;

        await _sceneRepository.UpdateAsync(scene);

        return MapToResponseDto(scene);
    }

    public async Task<bool> DeleteSceneAsync(Guid id)
    {
        var scene = await _sceneRepository.GetByIdAsync(id);
        if (scene == null)
            throw new Exception("Scene not found");

        await _sceneRepository.DeleteAsync(scene);
        return true;
    }

    private static SceneResponseDto MapToResponseDto(HistoricalScene scene)
    {
        return new SceneResponseDto
        {
            Id = scene.Id,
            Title = scene.Title,
            Era = scene.Era,
            Location = scene.Location,
            Description = scene.Description,
            EnvironmentPrompt = scene.EnvironmentPrompt,
            Model3DUrl = scene.Model3DUrl,
            CreatedAt = scene.CreatedAt,
            UpdatedAt = scene.UpdatedAt,
            CharactersCount = scene.Characters?.Count ?? 0,
            Characters = scene.Characters?.Select(c => new CharacterSummaryDto
            {
                Id = c.Id,
                Name = c.Name,
                Role = c.Role,
                AvatarUrl = c.AvatarUrl
            }).ToList() ?? new List<CharacterSummaryDto>()
        };
    }
}