using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PastPort.Application.Interfaces;
using PastPort.Domain.Interfaces;

namespace PastPort.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UnityAssetsController : ControllerBase
{
    private readonly IAssetRepository _assetRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly ILogger<UnityAssetsController> _logger;

    public UnityAssetsController(
        IAssetRepository assetRepository,
        IFileStorageService fileStorageService,
        ILogger<UnityAssetsController> logger)
    {
        _assetRepository = assetRepository;
        _fileStorageService = fileStorageService;
        _logger = logger;
    }

    /// <summary>
    /// البحث عن Asset بالاسم
    /// Unity استخدم: GET /api/unityassets/search?name=chair_01
    /// </summary>
    [HttpGet("search")]
    [AllowAnonymous]
    public async Task<IActionResult> SearchAsset([FromQuery] string name)
    {
        try
        {
            if (string.IsNullOrEmpty(name))
                return BadRequest(new { error = "Asset name is required" });

            // البحث عن الـ Asset
            var assets = await _assetRepository.GetAllAsync();
            var asset = assets.FirstOrDefault(a =>
                a.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (asset == null)
                return NotFound(new { error = "Asset not found" });

            return Ok(new
            {
                success = true,
                data = new
                {
                    id = asset.Id,
                    name = asset.Name,
                    fileName = asset.FileName,
                    type = asset.Type.ToString(),
                    fileUrl = asset.FileUrl,
                    fileSize = asset.FileSize,
                    fileHash = asset.FileHash,
                    version = asset.Version
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching asset");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// الحصول على جميع Assets الخاصة بـ Scene
    /// Unity استخدم: GET /api/unityassets/scene/sceneId
    /// </summary>
    [HttpGet("scene/{sceneId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetSceneAssets(Guid sceneId)
    {
        try
        {
            var assets = await _assetRepository.GetAssetsBySceneIdAsync(sceneId);

            return Ok(new
            {
                success = true,
                data = assets.Select(a => new
                {
                    id = a.Id,
                    name = a.Name,
                    fileName = a.FileName,
                    type = a.Type.ToString(),
                    fileUrl = a.FileUrl,
                    fileSize = a.FileSize,
                    fileHash = a.FileHash,
                    version = a.Version
                }).ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting scene assets");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// تحميل Asset
    /// Unity استخدم: GET /api/unityassets/download/assetId
    /// </summary>
    [HttpGet("download/{assetId}")]
    [AllowAnonymous]
    public async Task<IActionResult> DownloadAsset(Guid assetId)
    {
        try
        {
            var asset = await _assetRepository.GetByIdAsync(assetId);
            if (asset == null)
                return NotFound(new { error = "Asset not found" });

            if (!_fileStorageService.FileExists(asset.FileUrl))
                return NotFound(new { error = "File not found on server" });

            var fileBytes = await _fileStorageService.GetFileAsync(asset.FileUrl);
            var contentType = GetContentType(asset.FileName);

            _logger.LogInformation("Asset downloaded: {AssetId}", assetId);

            return File(fileBytes, contentType, asset.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading asset");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// التحقق من وجود Asset وحالته
    /// Unity استخدم: POST /api/unityassets/verify
    /// </summary>
    [HttpPost("verify")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyAsset([FromBody] VerifyAssetRequest request)
    {
        try
        {
            var asset = await _assetRepository.GetByIdAsync(request.AssetId);
            if (asset == null)
                return NotFound(new { error = "Asset not found" });

            var fileExists = _fileStorageService.FileExists(asset.FileUrl);
            var hashMatches = asset.FileHash == request.FileHash;

            return Ok(new
            {
                success = true,
                data = new
                {
                    exists = fileExists,
                    hashMatches = hashMatches,
                    needsDownload = !fileExists || !hashMatches,
                    asset = new
                    {
                        id = asset.Id,
                        name = asset.Name,
                        fileUrl = asset.FileUrl,
                        fileHash = asset.FileHash,
                        version = asset.Version
                    }
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying asset");
            return BadRequest(new { error = ex.Message });
        }
    }

    // Helper
    private string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".fbx" => "application/octet-stream",
            ".obj" => "application/octet-stream",
            ".glb" => "model/gltf-binary",
            ".gltf" => "model/gltf+json",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".mp3" => "audio/mpeg",
            ".wav" => "audio/wav",
            _ => "application/octet-stream"
        };
    }
}

public class VerifyAssetRequest
{
    public Guid AssetId { get; set; }
    public string FileHash { get; set; } = string.Empty;
}