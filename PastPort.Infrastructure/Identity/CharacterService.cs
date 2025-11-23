using PastPort.Application.DTOs.Request;
using PastPort.Application.DTOs.Response;
using PastPort.Application.Interfaces;
using PastPort.Domain.Entities;
using PastPort.Domain.Interfaces;

namespace PastPort.Application.Identity;

public class CharacterService : ICharacterService
{
    private readonly ICharacterRepository _characterRepository;
    private readonly ISceneRepository _sceneRepository;

    public CharacterService(
        ICharacterRepository characterRepository,
        ISceneRepository sceneRepository)
    {
        _characterRepository = characterRepository;
        _sceneRepository = sceneRepository;
    }

    public async Task<CharacterResponseDto> CreateCharacterAsync(CreateCharacterRequestDto request)
    {
        // Verify scene exists
        var sceneExists = await _sceneRepository.ExistsAsync(request.SceneId);
        if (!sceneExists)
            throw new Exception("Scene not found");

        var character = new Character
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Role = request.Role,
            Background = request.Background,
            Personality = request.Personality,
            VoiceId = request.VoiceId ?? string.Empty,
            AvatarUrl = request.AvatarUrl ?? string.Empty,
            SceneId = request.SceneId,
            CreatedAt = DateTime.UtcNow
        };

        await _characterRepository.AddAsync(character);

        // Load scene for response
        var characterWithScene = await _characterRepository.GetCharacterWithSceneAsync(character.Id);
        return MapToResponseDto(characterWithScene!);
    }

    public async Task<CharacterResponseDto> GetCharacterByIdAsync(Guid id)
    {
        var character = await _characterRepository.GetCharacterWithSceneAsync(id);
        if (character == null)
            throw new Exception("Character not found");

        return MapToResponseDto(character);
    }

    public async Task<List<CharacterResponseDto>> GetAllCharactersAsync()
    {
        var characters = await _characterRepository.GetAllAsync();
        var charactersWithScenes = new List<CharacterResponseDto>();

        foreach (var character in characters)
        {
            var fullCharacter = await _characterRepository.GetCharacterWithSceneAsync(character.Id);
            if (fullCharacter != null)
                charactersWithScenes.Add(MapToResponseDto(fullCharacter));
        }

        return charactersWithScenes;
    }

    public async Task<List<CharacterResponseDto>> GetCharactersBySceneIdAsync(Guid sceneId)
    {
        var characters = await _characterRepository.GetCharactersBySceneIdAsync(sceneId);
        return characters.Select(MapToResponseDto).ToList();
    }

    public async Task<CharacterResponseDto> UpdateCharacterAsync(Guid id, UpdateCharacterRequestDto request)
    {
        var character = await _characterRepository.GetByIdAsync(id);
        if (character == null)
            throw new Exception("Character not found");

        if (!string.IsNullOrEmpty(request.Name))
            character.Name = request.Name;

        if (!string.IsNullOrEmpty(request.Role))
            character.Role = request.Role;

        if (!string.IsNullOrEmpty(request.Background))
            character.Background = request.Background;

        if (!string.IsNullOrEmpty(request.Personality))
            character.Personality = request.Personality;

        if (request.VoiceId != null)
            character.VoiceId = request.VoiceId;

        if (request.AvatarUrl != null)
            character.AvatarUrl = request.AvatarUrl;

        await _characterRepository.UpdateAsync(character);

        var updatedCharacter = await _characterRepository.GetCharacterWithSceneAsync(id);
        return MapToResponseDto(updatedCharacter!);
    }

    public async Task<bool> DeleteCharacterAsync(Guid id)
    {
        var character = await _characterRepository.GetByIdAsync(id);
        if (character == null)
            throw new Exception("Character not found");

        await _characterRepository.DeleteAsync(character);
        return true;
    }

    private static CharacterResponseDto MapToResponseDto(Character character)
    {
        return new CharacterResponseDto
        {
            Id = character.Id,
            Name = character.Name,
            Role = character.Role,
            Background = character.Background,
            Personality = character.Personality,
            VoiceId = character.VoiceId,
            AvatarUrl = character.AvatarUrl,
            SceneId = character.SceneId,
            SceneTitle = character.Scene?.Title ?? string.Empty,
            CreatedAt = character.CreatedAt
        };
    }
}