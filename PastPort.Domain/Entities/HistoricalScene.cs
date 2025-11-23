using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PastPort.Domain.Entities;

public class HistoricalScene
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Era { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string EnvironmentPrompt { get; set; } = string.Empty;
    public string Model3DUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation Properties
    public ICollection<Character> Characters { get; set; } = new List<Character>();
}