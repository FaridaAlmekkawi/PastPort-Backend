
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

public class AssetCheckResult
{
    public string FileName { get; set; } = string.Empty;
    public bool Exists { get; set; }
    public bool NeedsUpdate { get; set; }
    public string Action { get; set; } = string.Empty; // "Download", "Update", "Skip", "NotFound"
    public AssetDto? AssetInfo { get; set; }
}

// ========== Unity Sync DTOs ==========

public class UnitySyncRequestDto
{
    public string UnityVersion { get; set; } = string.Empty;
    public List<LocalAssetInfo> LocalAssets { get; set; } = new();
}

public class LocalAssetInfo
{
    public string FileName { get; set; } = string.Empty;
    public string FileHash { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
}

public class UnitySyncResponseDto
{
    public bool Success { get; set; }
    public List<AssetSyncAction> Actions { get; set; } = new();
    public int TotalActions { get; set; }
    public int DownloadCount { get; set; }
    public int UpdateCount { get; set; }
    public int DeleteCount { get; set; }
}

public class AssetSyncAction
{
    public string FileName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty; // "Download", "Update", "Delete", "Keep"
    public string? DownloadUrl { get; set; }
    public long FileSize { get; set; }
    public string FileHash { get; set; } = string.Empty;
}