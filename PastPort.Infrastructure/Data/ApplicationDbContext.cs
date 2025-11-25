using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PastPort.Domain.Entities;

namespace PastPort.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<HistoricalScene> HistoricalScenes { get; set; }
    public DbSet<Character> Characters { get; set; }
    public DbSet<Conversation> Conversations { get; set; }
    public DbSet<Subscription> Subscriptions { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<EmailVerificationCode> EmailVerificationCodes { get; set; } = null!;
    public DbSet<PasswordResetToken> PasswordResetTokens { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // HistoricalScene - Characters relationship
        builder.Entity<HistoricalScene>()
            .HasMany(s => s.Characters)
            .WithOne(c => c.Scene)
            .HasForeignKey(c => c.SceneId);

        // RefreshToken - User relationship
        builder.Entity<RefreshToken>()
            .HasOne(rt => rt.User)
            .WithMany()
            .HasForeignKey(rt => rt.UserId);

        // Subscription Price Configuration
        builder.Entity<Subscription>()
            .Property(s => s.Price)
            .HasColumnType("decimal(18,2)");

        // Email Verification
        builder.Entity<EmailVerificationCode>()
            .HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<EmailVerificationCode>()
            .HasIndex(e => e.Code);

        // Password Reset
        builder.Entity<PasswordResetToken>()
            .HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<PasswordResetToken>()
            .HasIndex(p => p.Code);

        builder.Entity<PasswordResetToken>()
            .HasIndex(p => p.Token);

    }
}