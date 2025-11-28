using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PastPort.Application.DTOs.Response;
using PastPort.Domain.Entities;
using PastPort.Domain.Interfaces;

namespace PastPort.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class AssetsController : ControllerBase
{
    private readonly IAssetRepository _assetRepository;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<AssetsController> _logger;

    public AssetsController(
        IAssetRepository assetRepository,
        IWebHostEnvironment environment,
        ILogger<AssetsController> logger)
    {
        _assetRepository = assetRepository;
        _environment = environment;
        _logger = logger;
    }

    /// <summary>
    /// Get all assets for a specific scene (Unity calls this)
    /// </summary>
    [HttpGet("scenes/{sceneId}")]
    public async Task<IActionResult> GetSceneAssets(Guid sceneId)
    {
        var assets = await _assetRepository.GetAssetsBySceneIdAsync(sceneId);

        var assetsDto = assets.Select(a => new AssetDto
        {
            Id = a.Id,
            Name = a.Name,
            FileName = a.FileName,
            Type = a.Type.ToString(),
            FileUrl = $"{Request.Scheme}://{Request.Host}/assets/{a.FileName}",
            FileSize = a.FileSize,
            FileHash = a.FileHash,
            Version = a.Version,
            Status = a.Status.ToString()
        }).ToList();

        return Ok(new AssetSyncResponseDto
        {
            Success = true,
            TotalAssets = assetsDto.Count,
            Assets = assetsDto
        });
    }

    /// <summary>
    /// Check which assets Unity needs to download
    /// </summary>
    [HttpPost("check")]
    public async Task<IActionResult> CheckAssets([FromBody] AssetCheckRequestDto request)
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
                AssetInfo = new AssetDto
                {
                    Id = asset.Id,
                    Name = asset.Name,
                    FileName = asset.FileName,
                    Type = asset.Type.ToString(),
                    FileUrl = $"{Request.Scheme}://{Request.Host}/assets/{asset.FileName}",
                    FileSize = asset.FileSize,
                    FileHash = asset.FileHash,
                    Version = asset.Version,
                    Status = asset.Status.ToString()
                }
            });
        }

        return Ok(new AssetCheckResponseDto
        {
            Success = true,
            Results = results
        });
    }

    /// <summary>
    /// Download a specific asset file
    /// </summary>
    [HttpGet("download/{fileName}")]
    public async Task<IActionResult> DownloadAsset(string fileName)
    {
        var asset = await _assetRepository.GetAssetByFileNameAsync(fileName);

        if (asset == null)
            return NotFound(new { message = "Asset not found" });

        var filePath = Path.Combine(_environment.WebRootPath, "assets", fileName);

        if (!System.IO.File.Exists(filePath))
            return NotFound(new { message = "Asset file not found on server" });

        var memory = new MemoryStream();
        using (var stream = new FileStream(filePath, FileMode.Open))
        {
            await stream.CopyToAsync(memory);
        }
        memory.Position = 0;

        var contentType = GetContentType(fileName);
        return File(memory, contentType, fileName);
    }

    /// <summary>
    /// Upload new asset (Admin only)
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPost("upload")]
    public async Task<IActionResult> UploadAsset(
        [FromForm] IFormFile file,
        [FromForm] string name,
        [FromForm] AssetType type,
        [FromForm] Guid? sceneId)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file uploaded" });

        // Create assets directory if not exists
        var assetsPath = Path.Combine(_environment.WebRootPath, "assets");
        if (!Directory.Exists(assetsPath))
            Directory.CreateDirectory(assetsPath);

        var fileName = $"{Guid.NewGuid()}_{file.FileName}";
        var filePath = Path.Combine(assetsPath, fileName);

        // Save file
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // Calculate file hash
        var fileHash = await CalculateFileHashAsync(filePath);

        // Create asset record
        var asset = new Asset
        {
            Id = Guid.NewGuid(),
            Name = name,
            FileName = fileName,
            Type = type,
            FilePath = filePath,
            FileUrl = $"/assets/{fileName}",
            FileSize = file.Length,
            FileHash = fileHash,
            Version = "1.0.0",
            SceneId = sceneId,
            Status = AssetStatus.Available,
            CreatedAt = DateTime.UtcNow
        };

        await _assetRepository.AddAsync(asset);

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
                FileUrl = $"{Request.Scheme}://{Request.Host}/assets/{asset.FileName}",
                FileSize = asset.FileSize,
                FileHash = asset.FileHash,
                Version = asset.Version
            }
        });
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
            _ => "application/octet-stream"
        };
    }

    private async Task<string> CalculateFileHashAsync(string filePath)
    {
        using var md5 = System.Security.Cryptography.MD5.Create();
        using var stream = System.IO.File.OpenRead(filePath);
        var hash = await md5.ComputeHashAsync(stream);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }
}