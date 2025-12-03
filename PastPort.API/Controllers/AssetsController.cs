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
    /// Unity: Get all available assets (?????? ?????)
    /// </summary>
    [HttpGet("sync")]
    public async Task<IActionResult> SyncAssets()
    {
        try
        {
            var assets = await _assetRepository.GetAllAsync();

            var assetsDto = assets
                .Where(a => a.Status == AssetStatus.Available)
                .Select(a => new AssetDto
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

            return Ok(new
            {
                success = true,
                totalAssets = assetsDto.Count,
                assets = assetsDto,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync assets");
            return StatusCode(500, new { error = "Failed to sync assets" });
        }
    }

    /// <summary>
    /// Unity: Get assets for specific scene
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
                FileUrl = $"{Request.Scheme}://{Request.Host}/assets/{a.FileName}",
                FileSize = a.FileSize,
                FileHash = a.FileHash,
                Version = a.Version,
                Status = a.Status.ToString()
            }).ToList();

            return Ok(new
            {
                success = true,
                sceneId,
                totalAssets = assetsDto.Count,
                assets = assetsDto
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get scene assets for {SceneId}", sceneId);
            return StatusCode(500, new { error = "Failed to retrieve scene assets" });
        }
    }

    /// <summary>
    /// Unity: Check which assets need to be downloaded/updated
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
                    // Asset ?? ???? ?? ?????????
                    results.Add(new AssetCheckResult
                    {
                        FileName = item.FileName,
                        Exists = false,
                        NeedsUpdate = false,
                        Action = "NotFound"
                    });
                    continue;
                }

                // ?????? ?? ??? Hash
                var needsUpdate = !string.IsNullOrEmpty(item.FileHash) &&
                                 asset.FileHash != item.FileHash;

                results.Add(new AssetCheckResult
                {
                    FileName = item.FileName,
                    Exists = true,
                    NeedsUpdate = needsUpdate,
                    Action = needsUpdate ? "Update" : "Skip",
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

            return Ok(new
            {
                success = true,
                totalChecked = results.Count,
                needsDownload = results.Count(r => !r.Exists),
                needsUpdate = results.Count(r => r.NeedsUpdate),
                upToDate = results.Count(r => r.Action == "Skip"),
                results
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check assets");
            return StatusCode(500, new { error = "Failed to check assets" });
        }
    }

    /// <summary>
    /// Unity: Download specific asset file
    /// </summary>
    [HttpGet("download/{fileName}")]
    public async Task<IActionResult> DownloadAsset(string fileName)
    {
        try
        {
            var asset = await _assetRepository.GetAssetByFileNameAsync(fileName);

            if (asset == null)
            {
                _logger.LogWarning("Asset not found in database: {FileName}", fileName);
                return NotFound(new { error = "Asset not found in database" });
            }

            var filePath = Path.Combine(_environment.WebRootPath, "assets", fileName);

            if (!System.IO.File.Exists(filePath))
            {
                _logger.LogError("Asset file not found on disk: {FilePath}", filePath);
                return NotFound(new { error = "Asset file not found on server" });
            }

            var memory = new MemoryStream();
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;

            var contentType = GetContentType(fileName);

            _logger.LogInformation("Asset downloaded: {FileName} ({FileSize} bytes)",
                fileName, memory.Length);

            return File(memory, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download asset: {FileName}", fileName);
            return StatusCode(500, new { error = "Failed to download asset" });
        }
    }

    /// <summary>
    /// Unity: Bulk download multiple assets
    /// </summary>
    [HttpPost("download/bulk")]
    public async Task<IActionResult> BulkDownloadAssets([FromBody] BulkDownloadRequestDto request)
    {
        try
        {
            var results = new List<BulkDownloadResult>();

            foreach (var fileName in request.FileNames)
            {
                var asset = await _assetRepository.GetAssetByFileNameAsync(fileName);

                if (asset == null)
                {
                    results.Add(new BulkDownloadResult
                    {
                        FileName = fileName,
                        Success = false,
                        Error = "Asset not found"
                    });
                    continue;
                }

                var downloadUrl = $"{Request.Scheme}://{Request.Host}/api/assets/download/{fileName}";

                results.Add(new BulkDownloadResult
                {
                    FileName = fileName,
                    Success = true,
                    DownloadUrl = downloadUrl,
                    FileSize = asset.FileSize,
                    FileHash = asset.FileHash
                });
            }

            return Ok(new
            {
                success = true,
                totalRequested = request.FileNames.Count,
                successful = results.Count(r => r.Success),
                failed = results.Count(r => !r.Success),
                results
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process bulk download");
            return StatusCode(500, new { error = "Failed to process bulk download" });
        }
    }

    /// <summary>
    /// Admin: Upload new asset
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPost("upload")]
    public async Task<IActionResult> UploadAsset(
        [FromForm] IFormFile file,
        [FromForm] string name,
        [FromForm] AssetType type,
        [FromForm] Guid? sceneId,
        [FromForm] string? description)
    {
        try
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { error = "No file uploaded" });

            // Create assets directory
            var assetsPath = Path.Combine(_environment.WebRootPath, "assets");
            if (!Directory.Exists(assetsPath))
                Directory.CreateDirectory(assetsPath);

            var fileName = $"{Guid.NewGuid()}_{file.FileName}";
            var filePath = Path.Combine(assetsPath, fileName);

            // Save file
            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Calculate hash
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
                Description = description,
                Status = AssetStatus.Available,
                CreatedAt = DateTime.UtcNow
            };

            await _assetRepository.AddAsync(asset);

            _logger.LogInformation("Asset uploaded: {FileName} ({FileSize} bytes)",
                fileName, file.Length);

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
                    Version = asset.Version,
                    Status = asset.Status.ToString()
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload asset");
            return StatusCode(500, new { error = "Failed to upload asset" });
        }
    }

    /// <summary>
    /// Admin: Delete asset
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAsset(Guid id)
    {
        try
        {
            var asset = await _assetRepository.GetByIdAsync(id);
            if (asset == null)
                return NotFound(new { error = "Asset not found" });

            // Delete file
            var filePath = Path.Combine(_environment.WebRootPath, "assets", asset.FileName);
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }

            // Delete from database
            await _assetRepository.DeleteAsync(asset);

            _logger.LogInformation("Asset deleted: {FileName}", asset.FileName);

            return Ok(new { success = true, message = "Asset deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete asset {AssetId}", id);
            return StatusCode(500, new { error = "Failed to delete asset" });
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
            ".prefab" => "application/octet-stream",
            ".mat" => "application/octet-stream",
            ".unity" => "application/octet-stream",
            _ => "application/octet-stream"
        };
    }

    private async Task<string> CalculateFileHashAsync(string filePath)
    {
        using var md5 = System.Security.Cryptography.MD5.Create();
        await using var stream = System.IO.File.OpenRead(filePath);
        var hash = await md5.ComputeHashAsync(stream);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }
}

// DTOs
public class BulkDownloadRequestDto
{
    public List<string> FileNames { get; set; } = new();
}

public class BulkDownloadResult
{
    public string FileName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? DownloadUrl { get; set; }
    public long FileSize { get; set; }
    public string? FileHash { get; set; }
    public string? Error { get; set; }
}