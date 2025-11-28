namespace PastPort.Application.DTOs.Response;

public class AssetDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string FileHash { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class AssetSyncResponseDto
{
    public bool Success { get; set; }
    public int TotalAssets { get; set; }
    public List<AssetDto> Assets { get; set; } = new();
    public string? Message { get; set; }
}

public class AssetCheckRequestDto
{
    public List<AssetCheckItem> Assets { get; set; } = new();
}

public class AssetCheckItem
{
    public string FileName { get; set; } = string.Empty;
    public string? FileHash { get; set; }
    public string? Version { get; set; }
}

public class AssetCheckResponseDto
{
    public bool Success { get; set; }
    public List<AssetCheckResult> Results { get; set; } = new();
}

public class AssetCheckResult
{
    public string FileName { get; set; } = string.Empty;
    public bool Exists { get; set; }
    public bool NeedsUpdate { get; set; }
    public AssetDto? AssetInfo { get; set; }
}