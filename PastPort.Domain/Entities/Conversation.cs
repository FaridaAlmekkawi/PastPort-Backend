using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PastPort.Domain.Entities;

public class Conversation
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public Guid CharacterId { get; set; }
    public string UserMessage { get; set; } = string.Empty;
    public string CharacterResponse { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public ApplicationUser User { get; set; } = null!;
}