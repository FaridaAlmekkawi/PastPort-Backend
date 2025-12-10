// PastPort.Infrastructure/Data/ApplicationDbContext.cs
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PastPort.Domain.Entities;
using PastPort.Domain.Enums;

namespace PastPort.Infrastructure.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{

    // Existing DbSets
    public required DbSet<HistoricalScene> HistoricalScenes { get; set; }
    public required DbSet<Character> Characters { get; set; }
    public required DbSet<Conversation> Conversations { get; set; }
    public required DbSet<Subscription> Subscriptions { get; set; }
    public required DbSet<RefreshToken> RefreshTokens { get; set; }
    public required DbSet<EmailVerificationCode> EmailVerificationCodes { get; set; }
    public required DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
    public required DbSet<Asset> Assets { get; set; }

    // NEW: Payment DbSets
    public required DbSet<Payment> Payments { get; set; }
    public required DbSet<PaymentTransaction> PaymentTransactions { get; set; }
    public required DbSet<Refund> Refunds { get; set; }
    public required DbSet<Invoice> Invoices { get; set; }
    public required DbSet<InvoiceItem> InvoiceItems { get; set; }
    public required DbSet<SavedPaymentMethod> SavedPaymentMethods { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ==================== Existing Configurations ====================

        builder.Entity<HistoricalScene>()
            .HasMany(s => s.Characters)
            .WithOne(c => c.Scene)
            .HasForeignKey(c => c.SceneId);

        builder.Entity<RefreshToken>()
            .HasOne(rt => rt.User)
            .WithMany()
            .HasForeignKey(rt => rt.UserId);

        builder.Entity<Subscription>()
            .Property(s => s.Price)
            .HasColumnType("decimal(18,2)");

        builder.Entity<EmailVerificationCode>()
            .HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<EmailVerificationCode>()
            .HasIndex(e => e.Code);

        builder.Entity<PasswordResetToken>()
            .HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<PasswordResetToken>()
            .HasIndex(p => p.Code);

        builder.Entity<PasswordResetToken>()
            .HasIndex(p => p.Token);

        builder.Entity<Asset>()
            .HasOne(a => a.Scene)
            .WithMany()
            .HasForeignKey(a => a.SceneId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Asset>()
            .HasIndex(a => a.FileName);

        builder.Entity<Asset>()
            .HasIndex(a => a.FileHash);

        // ==================== NEW: Payment Configurations ====================

        // Payment
        builder.Entity<Payment>(entity =>
        {
            entity.HasKey(p => p.Id);

            entity.Property(p => p.Amount)
                .HasColumnType("decimal(18,2)");

            entity.Property(p => p.SubtotalAmount)
                .HasColumnType("decimal(18,2)");

            entity.Property(p => p.TaxAmount)
                .HasColumnType("decimal(18,2)");

            entity.Property(p => p.DiscountAmount)
                .HasColumnType("decimal(18,2)");

            entity.HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(p => p.Subscription)
                .WithMany()
                .HasForeignKey(p => p.SubscriptionId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(p => p.ProviderPaymentId);
            entity.HasIndex(p => p.Status);
            entity.HasIndex(p => p.CreatedAt);
        });

        // PaymentTransaction
        builder.Entity<PaymentTransaction>(entity =>
        {
            entity.HasKey(pt => pt.Id);

            entity.HasOne(pt => pt.Payment)
                .WithMany()
                .HasForeignKey(pt => pt.PaymentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(pt => pt.CreatedAt);
        });

        // Refund
        builder.Entity<Refund>(entity =>
        {
            entity.HasKey(r => r.Id);

            entity.Property(r => r.Amount)
                .HasColumnType("decimal(18,2)");

            entity.HasOne(r => r.Payment)
                .WithMany()
                .HasForeignKey(r => r.PaymentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(r => r.ProviderRefundId);
            entity.HasIndex(r => r.Status);
        });

        // Invoice
        builder.Entity<Invoice>(entity =>
        {
            entity.HasKey(i => i.Id);

            entity.Property(i => i.Amount)
                .HasColumnType("decimal(18,2)");

            entity.Property(i => i.TaxAmount)
                .HasColumnType("decimal(18,2)");

            entity.Property(i => i.TotalAmount)
                .HasColumnType("decimal(18,2)");

            entity.HasOne(i => i.User)
                .WithMany()
                .HasForeignKey(i => i.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(i => i.Payment)
                .WithMany()
                .HasForeignKey(i => i.PaymentId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(i => i.Subscription)
                .WithMany()
                .HasForeignKey(i => i.SubscriptionId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(i => i.InvoiceNumber)
                .IsUnique();

            entity.HasIndex(i => i.Status);
            entity.HasIndex(i => i.IssuedAt);
        });

        // InvoiceItem
        builder.Entity<InvoiceItem>(entity =>
        {
            entity.HasKey(ii => ii.Id);

            entity.Property(ii => ii.UnitPrice)
                .HasColumnType("decimal(18,2)");

            entity.Property(ii => ii.Amount)
                .HasColumnType("decimal(18,2)");

            entity.HasOne(ii => ii.Invoice)
                .WithMany(i => i.Items)
                .HasForeignKey(ii => ii.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // SavedPaymentMethod
        builder.Entity<SavedPaymentMethod>(entity =>
        {
            entity.HasKey(spm => spm.Id);

            entity.HasOne(spm => spm.User)
                .WithMany()
                .HasForeignKey(spm => spm.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(spm => spm.ProviderPaymentMethodId);
            entity.HasIndex(spm => new { spm.UserId, spm.IsDefault });
        });
    }
}