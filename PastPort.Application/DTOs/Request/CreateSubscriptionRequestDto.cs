using System.ComponentModel.DataAnnotations;
using PastPort.Domain.Enums;

namespace PastPort.Application.DTOs.Request;

public class CreateSubscriptionRequestDto
{
    [Required]
    public SubscriptionPlan Plan { get; set; }

    [Required]
    public int DurationInMonths { get; set; } = 1;
}