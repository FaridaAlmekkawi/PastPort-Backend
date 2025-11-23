using Microsoft.AspNetCore.Identity;
using Microsoft.VisualBasic;

namespace PastPort.Domain.Entities;

public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }

    // Navigation Properties
    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
    public ICollection<Conversation> Conversations { get; set; } = new List<Conversation>();
}