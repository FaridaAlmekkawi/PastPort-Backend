using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PastPort.Application.DTOs.Response;
using PastPort.Application.Interfaces;
using PastPort.Domain.Entities;
using PastPort.Domain.Enums;
using PastPort.Domain.Interfaces;
using System.Security.Claims;

namespace PastPort.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class AssetsController : ControllerBase
{
    private readonly IAssetRepository _assetRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly ILogger<AssetsController> _logger;

    public AssetsController(
        IAssetRepository assetRepository,
        IFileStorageService fileStorageService,
        ILogger<AssetsController> logger)
    {
        _assetRepository = assetRepository;
        _fileStorageService = fileStorageService;
        _logger = logger;
    }

    /// <summary>
    /// ?????? ??? ???? ??? Assets ?????? ?? Scene ?????
    /// </summary>
    [HttpGet("scenes/{sceneId}")]
    public async Task<IActionResult> GetSceneAssets(Guid sceneId)
    {
        try
        {
            var assets = await _assetRepository.GetAssetsBySceneIdAsync(sceneId);

            var assetsDto = assets.Select(a => new AssetDto
            {
                Id = a.Id,
                Name = a.Name,
                FileName = a.FileName,
                Type = a.Type.ToString(),
                FileUrl = a.FileUrl,
                FileSize = a.FileSize,
                FileHash = a.FileHash,
                Version = a.Version,
                Status = a.Status.ToString()
            }).ToList();

            return Ok(new AssetSyncResponseDto
            {
                Success = true,
                TotalAssets = assetsDto.Count,
                Assets = assetsDto,
                Message = "Assets retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting scene assets");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// ??? ?? Assets ????? ??? ?????
    /// </summary>
    [HttpPost("check")]
    public async Task<IActionResult> CheckAssets([FromBody] AssetCheckRequestDto request)
    {
        try
        {
            var results = new List<AssetCheckResult>();

            foreach (var item in request.Assets)
            {
                var asset = await _assetRepository.GetAssetByFileNameAsync(item.FileName);

                if (asset == null)
                {
                    results.Add(new AssetCheckResult
                    {
                        FileName = item.FileName,
                        Exists = false,
                        NeedsUpdate = false
                    });
                    continue;
                }

                var needsUpdate = !string.IsNullOrEmpty(item.FileHash) &&
                                 asset.FileHash != item.FileHash;

                results.Add(new AssetCheckResult
                {
                    FileName = item.FileName,
                    Exists = true,
                    NeedsUpdate = needsUpdate,
                    AssetInfo = needsUpdate ? new AssetDto
                    {
                        Id = asset.Id,
                        Name = asset.Name,
                        FileName = asset.FileName,
                        Type = asset.Type.ToString(),
                        FileUrl = asset.FileUrl,
                        FileSize = asset.FileSize,
                        FileHash = asset.FileHash,
                        Version = asset.Version,
                        Status = asset.Status.ToString()
                    } : null
                });
            }

            return Ok(new AssetCheckResponseDto
            {
                Success = true,
                Results = results
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking assets");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// ????? Asset ????
    /// </summary>
    [HttpGet("download/{fileName}")]
    [AllowAnonymous]
    public async Task<IActionResult> DownloadAsset(string fileName)
    {
        try
        {
            var asset = await _assetRepository.GetAssetByFileNameAsync(fileName);

            if (asset == null)
                return NotFound(new { message = "Asset not found" });

            if (!_fileStorageService.FileExists(asset.FileUrl))
                return NotFound(new { message = "Asset file not found on server" });

            var fileBytes = await _fileStorageService.GetFileAsync(asset.FileUrl);

            return File(fileBytes, GetContentType(fileName), fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading asset");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// ??? Asset ???? (Admin ???)
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPost("upload")]
    public async Task<IActionResult> UploadAsset(
        [FromForm] IFormFile file,
        [FromForm] string name,
        [FromForm] AssetType type,
        [FromForm] Guid? sceneId)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (file == null || file.Length == 0)
                return BadRequest(new { message = "No file uploaded" });

            // ????? ???? ????? ??? ?????
            var folder = type switch
            {
                AssetType.Model3D => "models",
                AssetType.Texture => "textures",
                AssetType.Audio => "audio",
                AssetType.Animation => "animations",
                _ => "assets"
            };

            // ??? ?????
            var fileUrl = await _fileStorageService.UploadFileAsync(file, folder);

            // ???? Hash
            var fileBytes = await _fileStorageService.GetFileAsync(fileUrl);
            var fileHash = ComputeHash(fileBytes);

            // ????? Asset record
            var asset = new Asset
            {
                Id = Guid.NewGuid(),
                Name = name,
                FileName = Path.GetFileName(fileUrl),
                Type = type,
                FilePath = fileUrl,
                FileUrl = fileUrl,
                FileSize = file.Length,
                FileHash = fileHash,
                Version = "1.0.0",
                SceneId = sceneId,
                Status = AssetStatus.Available,
                CreatedAt = DateTime.UtcNow
            };

            await _assetRepository.AddAsync(asset);

            _logger.LogInformation(
                "Asset uploaded by user {UserId}: {AssetId}",
                userId,
                asset.Id);

            return Ok(new
            {
                success = true,
                message = "Asset uploaded successfully",
                asset = new AssetDto
                {
                    Id = asset.Id,
                    Name = asset.Name,
                    FileName = asset.FileName,
                    Type = asset.Type.ToString(),
                    FileUrl = asset.FileUrl,
                    FileSize = asset.FileSize,
                    FileHash = asset.FileHash,
                    Version = asset.Version
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading asset");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// ??? Asset
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpDelete("{assetId}")]
    public async Task<IActionResult> DeleteAsset(Guid assetId)
    {
        try
        {
            var asset = await _assetRepository.GetByIdAsync(assetId);
            if (asset == null)
                return NotFound(new { message = "Asset not found" });

            // ??? ????? ?? ???????
            await _fileStorageService.DeleteFileAsync(asset.FileUrl);

            // ??? ????? ?? ????? ????????
            await _assetRepository.DeleteAsync(asset);

            _logger.LogInformation("Asset deleted: {AssetId}", assetId);

            return Ok(new { success = true, message = "Asset deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting asset");
            return BadRequest(new { error = ex.Message });
        }
    }

    // Helper Methods
    private string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".fbx" => "application/octet-stream",
            ".obj" => "application/octet-stream",
            ".gltf" => "model/gltf+json",
            ".glb" => "model/gltf-binary",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".mp3" => "audio/mpeg",
            ".wav" => "audio/wav",
            ".pdf" => "application/pdf",
            _ => "application/octet-stream"
        };
    }

    private string ComputeHash(byte[] fileBytes)
    {
        using var md5 = System.Security.Cryptography.MD5.Create();
        var hash = md5.ComputeHash(fileBytes);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }
}