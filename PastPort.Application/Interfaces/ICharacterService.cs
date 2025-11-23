using PastPort.Application.DTOs.Request;
using PastPort.Application.DTOs.Response;

namespace PastPort.Application.Interfaces;

public interface ICharacterService
{
    Task<CharacterResponseDto> CreateCharacterAsync(CreateCharacterRequestDto request);
    Task<CharacterResponseDto> GetCharacterByIdAsync(Guid id);
    Task<List<CharacterResponseDto>> GetAllCharactersAsync();
    Task<List<CharacterResponseDto>> GetCharactersBySceneIdAsync(Guid sceneId);
    Task<CharacterResponseDto> UpdateCharacterAsync(Guid id, UpdateCharacterRequestDto request);
    Task<bool> DeleteCharacterAsync(Guid id);
}