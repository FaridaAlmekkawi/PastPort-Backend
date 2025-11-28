namespace PastPort.Domain.Entities;

public class Asset
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty; // اسم الملف
    public string FileName { get; set; } = string.Empty; // chair_01.fbx
    public AssetType Type { get; set; } // Model, Texture, Audio, etc.
    public string FilePath { get; set; } = string.Empty; // مسار الملف
    public string FileUrl { get; set; } = string.Empty; // URL للتحميل
    public long FileSize { get; set; } // حجم الملف بالـ bytes
    public string FileHash { get; set; } = string.Empty; // MD5/SHA256 للتحقق
    public string Version { get; set; } = "1.0.0"; // إصدار الملف
    
    // Scene Relationship
    public Guid? SceneId { get; set; }
    public HistoricalScene? Scene { get; set; }
    
    // Metadata
    public string? Description { get; set; }
    public string? Tags { get; set; } // JSON: ["furniture", "ancient"]
    public AssetStatus Status { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public enum AssetType
{
    Model3D = 1,      // .fbx, .obj, .gltf
    Texture = 2,      // .png, .jpg
    Material = 3,     // .mat
    Audio = 4,        // .mp3, .wav
    Animation = 5,    // .anim
    Prefab = 6,       // .prefab
    Scene = 7,        // .unity
    Other = 99
}

public enum AssetStatus
{
    Available = 1,
    Processing = 2,
    Unavailable = 3,
    Deprecated = 4
}