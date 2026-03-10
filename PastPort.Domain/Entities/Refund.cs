using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PastPort.Domain.Enums;

namespace PastPort.Domain.Entities
{
    public class Refund
    {
        public Guid Id { get; set; }
        public Guid PaymentId { get; set; }
        public Payment Payment { get; set; } = null!;

        public decimal Amount { get; set; }
        public RefundStatus Status { get; set; }
        public RefundReason Reason { get; set; }

        public string? ProviderRefundId { get; set; }
        public string? Notes { get; set; }

        public string RequestedBy { get; set; } = string.Empty; // Admin User ID
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ProcessedAt { get; set; }
    }
}
