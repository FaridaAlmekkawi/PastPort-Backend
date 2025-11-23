using PastPort.Application.DTOs.Request;
using PastPort.Application.DTOs.Response;

namespace PastPort.Application.Interfaces;

public interface ISceneService
{
    Task<SceneResponseDto> CreateSceneAsync(CreateSceneRequestDto request);
    Task<SceneResponseDto> GetSceneByIdAsync(Guid id);
    Task<List<SceneResponseDto>> GetAllScenesAsync();
    Task<List<SceneResponseDto>> GetScenesByEraAsync(string era);
    Task<List<SceneResponseDto>> SearchScenesAsync(string searchTerm);
    Task<SceneResponseDto> UpdateSceneAsync(Guid id, UpdateSceneRequestDto request);
    Task<bool> DeleteSceneAsync(Guid id);
}