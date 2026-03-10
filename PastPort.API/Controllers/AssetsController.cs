using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PastPort.Application.DTOs.Response;
using PastPort.Domain.Entities;
using PastPort.Domain.Interfaces;
using PastPort.Application.Interfaces;
using System.ComponentModel.DataAnnotations;
using PastPort.Domain.Enums;
using PastPort.API.Extensions;


namespace PastPort.API.Controllers
{
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

        [AllowAnonymous]
        [HttpGet("scenes/{sceneId}")]
        public async Task<IActionResult> GetSceneAssets(Guid sceneId)
        {
            try
            {
                var assets = await _assetRepository.GetAssetsBySceneIdAsync(sceneId);
                return Ok(new { success = true, data = assets });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpPost("check")]
        public async Task<IActionResult> CheckAssets([FromBody] AssetCheckRequestDto request)
        {
            try
            {
                var results = new List<object>();
                foreach (var item in request.Assets)
                {
                    var asset = await _assetRepository.GetAssetByFileNameAsync(item.FileName);
                    results.Add(new
                    {
                        fileName = item.FileName,
                        exists = asset != null,
                        needsUpdate = asset != null && asset.FileHash != item.FileHash
                    });
                }
                return Ok(new { success = true, results = results });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpGet("download/{fileName}")]
        public async Task<IActionResult> DownloadAsset(string fileName)
        {
            try
            {
                var asset = await _assetRepository.GetAssetByFileNameAsync(fileName);
                if (asset == null)
                    return NotFound();

                var fileBytes = await _fileStorageService.GetFileAsync(asset.FileUrl);
                return File(fileBytes, GetContentType(fileName), fileName);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Upload Asset - Select file and fill form
        /// </summary>
        [Authorize]
        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadAsset(
            [Required]
            [FromForm(Name = "file")]
            IFormFile file,
            [Required]
            [FromForm(Name = "name")]
            string name,
            [Required]
            [FromForm(Name = "type")]
            int type,
            [FromForm(Name = "sceneId")]
            Guid? sceneId = null)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest(new { error = "File is required" });

                if (string.IsNullOrWhiteSpace(name))
                    return BadRequest(new { error = "Name is required" });

                if (!Enum.IsDefined(typeof(AssetType), type))
                    return BadRequest(new { error = "Invalid type. Use: 1=Model3D, 2=Texture, 4=Audio, 5=Animation" });

                var assetType = (AssetType)type;
                var folder = GetFolderByType(assetType);

                var fileUrl = await _fileStorageService.UploadFileAsync(file, folder);
                var fileBytes = await _fileStorageService.GetFileAsync(fileUrl);
                var fileHash = ComputeHash(fileBytes);

                var asset = new Asset
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    FileName = Path.GetFileName(fileUrl),
                    Type = assetType,
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

                return Ok(new
                {
                    success = true,
                    message = "Asset uploaded successfully",
                    asset = new
                    {
                        id = asset.Id,
                        name = asset.Name,
                        fileName = asset.FileName,
                        type = asset.Type.ToString(),
                        fileUrl = asset.FileUrl,
                        fileSize = asset.FileSize
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Upload error");
                return BadRequest(new { error = ex.Message });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{assetId}")]
        public async Task<IActionResult> DeleteAsset(Guid assetId)
        {
            try
            {
                var asset = await _assetRepository.GetByIdAsync(assetId);
                if (asset == null)
                    return NotFound();

                await _fileStorageService.DeleteFileAsync(asset.FileUrl);
                await _assetRepository.DeleteAsync(asset);

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        private string GetFolderByType(AssetType type) => type switch
        {
            AssetType.Model3D => "models",
            AssetType.Texture => "textures",
            AssetType.Audio => "audio",
            AssetType.Animation => "animations",
            _ => "assets"
        };

        private string GetContentType(string fileName) => Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".fbx" => "application/octet-stream",
            ".glb" => "model/gltf-binary",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".mp3" => "audio/mpeg",
            ".wav" => "audio/wav",
            _ => "application/octet-stream"
        };

        private string ComputeHash(byte[] fileBytes)
        {
            using var md5 = System.Security.Cryptography.MD5.Create();
            var hash = md5.ComputeHash(fileBytes);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
    }
}