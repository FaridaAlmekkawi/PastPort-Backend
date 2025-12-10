using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PastPort.Domain.Enums;

namespace PastPort.Domain.Entities
{
    public class Invoice
    {
        public Guid Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty; // INV-2025-0001

        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;

        public Guid? PaymentId { get; set; }
        public Payment? Payment { get; set; }

        public Guid? SubscriptionId { get; set; }
        public Subscription? Subscription { get; set; }

        // Invoice Details
        public decimal Amount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string Currency { get; set; } = "USD";

        public InvoiceStatus Status { get; set; }

        // Dates
        public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
        public DateTime DueDate { get; set; }
        public DateTime? PaidAt { get; set; }

        // URLs
        public string? PdfUrl { get; set; }
        public string? HostedInvoiceUrl { get; set; } // Stripe hosted invoice

        // Items
        public List<InvoiceItem> Items { get; set; } = new();
    }

}
