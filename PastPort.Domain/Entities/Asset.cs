using PastPort.Domain.Enums;

namespace PastPort.Domain.Entities;

public class Asset
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty; // الاسم الفريد للملف
    public PastPort.Domain.Enums.AssetType Type { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string FileHash { get; set; } = string.Empty; // MD5 Hash للتحقق
    public string Version { get; set; } = "1.0.0";

    // Scene Relationship
    public Guid? SceneId { get; set; }
    public HistoricalScene? Scene { get; set; }

    // Metadata
    public string? Description { get; set; }
    public string? Tags { get; set; }
    public PastPort.Domain.Enums.AssetStatus Status { get; set; }


    // Unity-Specific
    public string? UnityAssetPath { get; set; } // مسار الأصل في Unity
    public string? AssetBundleName { get; set; } // اسم AssetBundle

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}