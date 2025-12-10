using PastPort.Application.DTOs.Response;

namespace PastPort.API.Controllers
{
    internal class AssetSyncResponseDto
    {
        public bool Success { get; set; }
        public int TotalAssets { get; set; }
        public List<AssetDto> Assets { get; set; }
        public string Message { get; set; }
    }
}