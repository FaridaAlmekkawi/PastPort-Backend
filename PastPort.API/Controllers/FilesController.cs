using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PastPort.Application.DTOs.Response;
using PastPort.Application.Interfaces;
using PastPort.API.Extensions;

namespace PastPort.API.Controllers;

[Authorize]
public class FilesController : BaseApiController
{
    private readonly IFileStorageService _fileStorageService;
    private readonly ILogger<FilesController> _logger;

    // حدود الملفات
    private readonly string[] _imageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
    private readonly string[] _modelExtensions = { ".glb", ".gltf", ".obj", ".fbx" };
    private readonly string[] _audioExtensions = { ".mp3", ".wav", ".ogg" };
    private const long MaxImageSize = 5 * 1024 * 1024; // 5 MB
    private const long MaxModelSize = 50 * 1024 * 1024; // 50 MB
    private const long MaxAudioSize = 10 * 1024 * 1024; // 10 MB

    public FilesController(
        IFileStorageService fileStorageService,
        ILogger<FilesController> logger)
    {
        _fileStorageService = fileStorageService;
        _logger = logger;
    }

    /// <summary>
    /// Upload avatar/character image
    /// </summary>
    [HttpPost("upload/avatar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadAvatar(IFormFile file)
    {
        try
        {
            if (!_fileStorageService.ValidateFile(file, _imageExtensions, MaxImageSize))
            {
                return BadRequest(new
                {
                    error = "Invalid file. Allowed: JPG, PNG, GIF, WebP. Max size: 5MB"
                });
            }

            var fileUrl = await _fileStorageService.UploadFileAsync(file, "avatars");

            var response = new FileUploadResponseDto
            {
                FileName = file.FileName,
                FileUrl = fileUrl,
                FileType = file.ContentType,
                FileSize = file.Length,
                UploadedAt = DateTime.UtcNow
            };

            return Ok(new { data = response, message = "Avatar uploaded successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload avatar");
            return HandleError(ex);
        }
    }

    /// <summary>
    /// Upload 3D model
    /// </summary>
    [HttpPost("upload/model")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadModel(IFormFile file)
    {
        try
        {
            if (!_fileStorageService.ValidateFile(file, _modelExtensions, MaxModelSize))
            {
                return BadRequest(new
                {
                    error = "Invalid file. Allowed: GLB, GLTF, OBJ, FBX. Max size: 50MB"
                });
            }

            var fileUrl = await _fileStorageService.UploadFileAsync(file, "models");

            var response = new FileUploadResponseDto
            {
                FileName = file.FileName,
                FileUrl = fileUrl,
                FileType = file.ContentType,
                FileSize = file.Length,
                UploadedAt = DateTime.UtcNow
            };

            return Ok(new { data = response, message = "3D model uploaded successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload 3D model");
            return HandleError(ex);
        }
    }

    /// <summary>
    /// Upload scene image
    /// </summary>
    [HttpPost("upload/scene-image")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadSceneImage(IFormFile file)
    {
        try
        {
            if (!_fileStorageService.ValidateFile(file, _imageExtensions, MaxImageSize))
            {
                return BadRequest(new
                {
                    error = "Invalid file. Allowed: JPG, PNG, GIF, WebP. Max size: 5MB"
                });
            }

            var fileUrl = await _fileStorageService.UploadFileAsync(file, "scenes");

            var response = new FileUploadResponseDto
            {
                FileName = file.FileName,
                FileUrl = fileUrl,
                FileType = file.ContentType,
                FileSize = file.Length,
                UploadedAt = DateTime.UtcNow
            };

            return Ok(new { data = response, message = "Scene image uploaded successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload scene image");
            return HandleError(ex);
        }
    }

    /// <summary>
    /// Upload voice/audio file
    /// </summary>
    [HttpPost("upload/audio")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadAudio(IFormFile file)
    {
        try
        {
            if (!_fileStorageService.ValidateFile(file, _audioExtensions, MaxAudioSize))
            {
                return BadRequest(new
                {
                    error = "Invalid file. Allowed: MP3, WAV, OGG. Max size: 10MB"
                });
            }

            var fileUrl = await _fileStorageService.UploadFileAsync(file, "audio");

            var response = new FileUploadResponseDto
            {
                FileName = file.FileName,
                FileUrl = fileUrl,
                FileType = file.ContentType,
                FileSize = file.Length,
                UploadedAt = DateTime.UtcNow
            };

            return Ok(new { data = response, message = "Audio file uploaded successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload audio file");
            return HandleError(ex);
        }
    }

    /// <summary>
    /// Delete file
    /// </summary>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteFile([FromQuery] string fileUrl)
    {
        try
        {
            var result = await _fileStorageService.DeleteFileAsync(fileUrl);

            if (!result)
                return NotFound(new { error = "File not found" });

            return Ok(new { message = "File deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file: {FileUrl}", fileUrl);
            return HandleError(ex);
        }
    }
}