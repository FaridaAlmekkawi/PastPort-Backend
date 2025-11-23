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

    public DbSet<HistoricalScene> HistoricalScenes { get; set; } = null!;
    public DbSet<Character> Characters { get; set; } = null!;
    public DbSet<Conversation> Conversations { get; set; } = null!;
    public DbSet<Subscription> Subscriptions { get; set; } = null!;
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Apply configurations
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Custom configurations
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
    }
}