using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PastPort.Application.DTOs.Response;
using PastPort.Application.Interfaces;
using PastPort.API.Extensions;

namespace PastPort.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
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
    /// رفع صورة Avatar
    /// </summary>
    [HttpPost("upload/avatar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadAvatar(IFormFile file)
    {
        try
        {
            // التحقق من الملف
            if (!_fileStorageService.ValidateFile(file, _imageExtensions, MaxImageSize))
            {
                return BadRequest(new
                {
                    success = false,
                    error = "ملف غير صحيح. الأنواع المسموحة: JPG, PNG, GIF, WebP. الحجم الأقصى: 5MB"
                });
            }

            // رفع الملف
            var fileUrl = await _fileStorageService.UploadFileAsync(file, "avatars");

            var response = new FileUploadResponseDto
            {
                FileName = file.FileName,
                FileUrl = fileUrl,
                FileType = file.ContentType ?? "image/jpeg",
                FileSize = file.Length,
                UploadedAt = DateTime.UtcNow
            };

            _logger.LogInformation("Avatar uploaded successfully: {FileUrl}", fileUrl);

            return Ok(new
            {
                success = true,
                data = response,
                message = "تم رفع الصورة بنجاح"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ في رفع الصورة");
            return HandleError(ex);
        }
    }

    /// <summary>
    /// رفع موديل 3D
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
                    success = false,
                    error = "ملف غير صحيح. الأنواع المسموحة: GLB, GLTF, OBJ, FBX. الحجم الأقصى: 50MB"
                });
            }

            var fileUrl = await _fileStorageService.UploadFileAsync(file, "models");

            var response = new FileUploadResponseDto
            {
                FileName = file.FileName,
                FileUrl = fileUrl,
                FileType = file.ContentType ?? "application/octet-stream",
                FileSize = file.Length,
                UploadedAt = DateTime.UtcNow
            };

            _logger.LogInformation("3D model uploaded successfully: {FileUrl}", fileUrl);

            return Ok(new
            {
                success = true,
                data = response,
                message = "تم رفع الموديل بنجاح"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ في رفع الموديل");
            return HandleError(ex);
        }
    }

    /// <summary>
    /// رفع صورة Scene
    /// </summary>
    [HttpPost("upload/scene-image")]
    public async Task<IActionResult> UploadSceneImage(IFormFile file)
    {
        try
        {
            if (!_fileStorageService.ValidateFile(file, _imageExtensions, MaxImageSize))
            {
                return BadRequest(new
                {
                    success = false,
                    error = "ملف صورة غير صحيح"
                });
            }

            var fileUrl = await _fileStorageService.UploadFileAsync(file, "scenes");

            return Ok(new
            {
                success = true,
                data = new { fileUrl, fileName = file.FileName, fileSize = file.Length },
                message = "تم رفع صورة Scene بنجاح"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ في رفع صورة Scene");
            return HandleError(ex);
        }
    }

    /// <summary>
    /// رفع ملف صوتي
    /// </summary>
    [HttpPost("upload/audio")]
    public async Task<IActionResult> UploadAudio(IFormFile file)
    {
        try
        {
            if (!_fileStorageService.ValidateFile(file, _audioExtensions, MaxAudioSize))
            {
                return BadRequest(new
                {
                    success = false,
                    error = "ملف صوتي غير صحيح. الأنواع المسموحة: MP3, WAV, OGG. الحجم الأقصى: 10MB"
                });
            }

            var fileUrl = await _fileStorageService.UploadFileAsync(file, "audio");

            return Ok(new
            {
                success = true,
                data = new { fileUrl, fileName = file.FileName, fileSize = file.Length },
                message = "تم رفع الملف الصوتي بنجاح"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ في رفع ملف صوتي");
            return HandleError(ex);
        }
    }

    /// <summary>
    /// حذف ملف
    /// </summary>
    [HttpDelete]
    public async Task<IActionResult> DeleteFile([FromQuery] string fileUrl)
    {
        try
        {
            if (string.IsNullOrEmpty(fileUrl))
                return BadRequest(new { error = "رابط الملف مفقود" });

            var result = await _fileStorageService.DeleteFileAsync(fileUrl);

            if (!result)
                return NotFound(new { error = "الملف غير موجود" });

            _logger.LogInformation("File deleted successfully: {FileUrl}", fileUrl);

            return Ok(new { success = true, message = "تم حذف الملف بنجاح" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ في حذف الملف");
            return HandleError(ex);
        }
    }

    /// <summary>
    /// الحصول على معلومات الملف
    /// </summary>
    [HttpGet("info")]
    public IActionResult GetFileInfo([FromQuery] string fileUrl)
    {
        try
        {
            if (!_fileStorageService.FileExists(fileUrl))
                return NotFound(new { error = "الملف غير موجود" });

            var fileSize = _fileStorageService.GetFileSize(fileUrl);

            return Ok(new
            {
                success = true,
                data = new
                {
                    fileUrl,
                    fileSize,
                    exists = true
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ في الحصول على معلومات الملف");
            return HandleError(ex);
        }
    }

    /// <summary>
    /// رفع ملفات متعددة
    /// </summary>
    [HttpPost("upload/multiple")]
    public async Task<IActionResult> UploadMultiple(
        [FromForm] IFormFileCollection files,
        [FromQuery] string folder = "general")
    {
        try
        {
            if (files == null || files.Count == 0)
                return BadRequest(new { error = "لم يتم اختيار ملفات" });

            var uploadedFiles = new List<FileUploadResponseDto>();
            var failedFiles = new List<string>();

            foreach (var file in files)
            {
                try
                {
                    // التحقق من الحد الأقصى لحجم الملف الواحد
                    if (file.Length > MaxImageSize)
                    {
                        failedFiles.Add($"{file.FileName}: يتجاوز الحد الأقصى للحجم");
                        continue;
                    }

                    var fileUrl = await _fileStorageService.UploadFileAsync(file, folder);

                    uploadedFiles.Add(new FileUploadResponseDto
                    {
                        FileName = file.FileName,
                        FileUrl = fileUrl,
                        FileType = file.ContentType ?? "application/octet-stream",
                        FileSize = file.Length,
                        UploadedAt = DateTime.UtcNow
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "خطأ في رفع الملف: {FileName}", file.FileName);
                    failedFiles.Add($"{file.FileName}: {ex.Message}");
                }
            }

            return Ok(new
            {
                success = uploadedFiles.Count > 0,
                data = new
                {
                    uploadedFiles,
                    failedFiles,
                    totalUploaded = uploadedFiles.Count,
                    totalFailed = failedFiles.Count
                },
                message = $"تم رفع {uploadedFiles.Count} ملف بنجاح"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ في رفع ملفات متعددة");
            return HandleError(ex);
        }
    }
}