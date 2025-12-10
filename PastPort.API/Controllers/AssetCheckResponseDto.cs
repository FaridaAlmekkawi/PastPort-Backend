using PastPort.Application.DTOs.Response;

namespace PastPort.API.Controllers
{
    internal class AssetCheckResponseDto
    {
        public bool Success { get; set; }
        public List<AssetCheckResult> Results { get; set; }
    }
}