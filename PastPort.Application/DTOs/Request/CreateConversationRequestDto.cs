using System.ComponentModel.DataAnnotations;

namespace PastPort.Application.DTOs.Request;

public class CreateConversationRequestDto
{
    [Required]
    public Guid CharacterId { get; set; }

    [Required]
    [MaxLength(2000)]
    public string UserMessage { get; set; } = string.Empty;
}