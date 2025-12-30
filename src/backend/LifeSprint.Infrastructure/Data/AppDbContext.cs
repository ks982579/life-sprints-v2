using Microsoft.EntityFrameworkCore;
using LifeSprint.Core.Models;

namespace LifeSprint.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Session> Sessions => Set<Session>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Id).HasMaxLength(255);
            entity.Property(u => u.GitHubUsername).IsRequired().HasMaxLength(255);
            entity.Property(u => u.Email).HasMaxLength(255);
            entity.Property(u => u.AvatarUrl).HasMaxLength(500);
            entity.Property(u => u.AccessToken).HasMaxLength(500);
            entity.Property(u => u.CreatedAt).IsRequired();

            // Relationships
            entity.HasMany(u => u.Activities)
                .WithOne()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(u => u.Sessions)
                .WithOne(s => s.User)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Activity configuration
        modelBuilder.Entity<Activity>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.Property(a => a.UserId).IsRequired().HasMaxLength(255);
            entity.Property(a => a.Title).IsRequired().HasMaxLength(500);
            entity.Property(a => a.Description).HasColumnType("text");
            entity.Property(a => a.Type).IsRequired()
                .HasConversion<string>()
                .HasMaxLength(50);
            entity.Property(a => a.State).IsRequired()
                .HasConversion<string>()
                .HasMaxLength(50);
            entity.Property(a => a.EstimatedHours).HasColumnType("decimal(10,2)");
            entity.Property(a => a.ActualHours).HasColumnType("decimal(10,2)");
            entity.Property(a => a.CreatedAt).IsRequired();
            entity.Property(a => a.UpdatedAt).IsRequired();

            // Indexes
            entity.HasIndex(a => a.UserId);
            entity.HasIndex(a => a.Type);
            entity.HasIndex(a => a.State);
            entity.HasIndex(a => a.ParentId);
            entity.HasIndex(a => new { a.InAnnualBacklog, a.InMonthlyBacklog, a.InWeeklySprint, a.InDailyChecklist })
                .HasDatabaseName("IX_Activity_BacklogFlags");

            // Self-referencing relationship for hierarchy
            entity.HasOne(a => a.Parent)
                .WithMany(a => a.Children)
                .HasForeignKey(a => a.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Session configuration
        modelBuilder.Entity<Session>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.UserId).IsRequired().HasMaxLength(255);
            entity.Property(s => s.SessionToken).IsRequired().HasMaxLength(500);
            entity.Property(s => s.ExpiresAt).IsRequired();
            entity.Property(s => s.CreatedAt).IsRequired();

            // Indexes
            entity.HasIndex(s => s.SessionToken).IsUnique();
            entity.HasIndex(s => s.UserId);
        });
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is Activity && (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entry in entries)
        {
            var activity = (Activity)entry.Entity;

            if (entry.State == EntityState.Added)
            {
                activity.CreatedAt = DateTime.UtcNow;
                activity.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                activity.UpdatedAt = DateTime.UtcNow;
            }
        }

        var userEntries = ChangeTracker.Entries()
            .Where(e => e.Entity is User && e.State == EntityState.Added);

        foreach (var entry in userEntries)
        {
            var user = (User)entry.Entity;
            user.CreatedAt = DateTime.UtcNow;
        }

        var sessionEntries = ChangeTracker.Entries()
            .Where(e => e.Entity is Session && e.State == EntityState.Added);

        foreach (var entry in sessionEntries)
        {
            var session = (Session)entry.Entity;
            session.CreatedAt = DateTime.UtcNow;
        }
    }
}
