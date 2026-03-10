using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PastPort.Domain.Entities;

public class Character
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Background { get; set; } = string.Empty;
    public string Personality { get; set; } = string.Empty;
    public string VoiceId { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;

    // Foreign Key
    public Guid SceneId { get; set; }
    public HistoricalScene Scene { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}