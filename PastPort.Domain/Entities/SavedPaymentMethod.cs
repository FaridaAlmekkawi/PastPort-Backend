using PastPort.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PastPort.Domain.Entities
{
    public class SavedPaymentMethod
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;

        public PaymentProvider Provider { get; set; }
        public string ProviderPaymentMethodId { get; set; } = string.Empty;

        public PaymentMethod Type { get; set; }

        // Card Details (masked)
        public string? CardLast4 { get; set; }
        public string? CardBrand { get; set; } // Visa, MasterCard
        public int? CardExpMonth { get; set; }
        public int? CardExpYear { get; set; }

        public bool IsDefault { get; set; }
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? DeletedAt { get; set; }
    }
}
