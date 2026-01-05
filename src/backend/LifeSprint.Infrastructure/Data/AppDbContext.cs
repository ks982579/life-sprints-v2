using Microsoft.EntityFrameworkCore;
using LifeSprint.Core.Models;

namespace LifeSprint.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // Authentication
    public DbSet<User> Users => Set<User>();
    public DbSet<Session> Sessions => Set<Session>();

    // Activity Management (New Container Architecture)
    public DbSet<ActivityTemplate> ActivityTemplates => Set<ActivityTemplate>();
    public DbSet<Container> Containers => Set<Container>();
    public DbSet<ContainerActivity> ContainerActivities => Set<ContainerActivity>();

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
            entity.HasMany(u => u.Sessions)
                .WithOne(s => s.User)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ActivityTemplate configuration
        modelBuilder.Entity<ActivityTemplate>(entity =>
        {
            entity.HasKey(at => at.Id);
            entity.Property(at => at.UserId).IsRequired().HasMaxLength(255);
            entity.Property(at => at.Title).IsRequired().HasMaxLength(500);
            entity.Property(at => at.Description).HasColumnType("text");
            entity.Property(at => at.Type).IsRequired()
                .HasConversion<string>();
            entity.Property(at => at.IsRecurring).IsRequired();
            entity.Property(at => at.RecurrenceType).IsRequired()
                .HasConversion<string>();
            entity.Property(at => at.CreatedAt).IsRequired();

            // Self-referencing relationship for parent-child hierarchy
            entity.HasOne(at => at.ParentActivity)
                .WithMany(at => at.ChildActivities)
                .HasForeignKey(at => at.ParentActivityId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete

            // Indexes for common queries
            entity.HasIndex(at => at.UserId);
            entity.HasIndex(at => new { at.UserId, at.Type });
            entity.HasIndex(at => new { at.UserId, at.IsRecurring });
            entity.HasIndex(at => at.ParentActivityId); // For hierarchy queries
            entity.HasIndex(at => at.ArchivedAt); // For filtering out archived templates
        });

        // Container configuration
        modelBuilder.Entity<Container>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.UserId).IsRequired().HasMaxLength(255);
            entity.Property(c => c.Type).IsRequired()
                .HasConversion<string>();
            entity.Property(c => c.Status).IsRequired()
                .HasConversion<string>();
            entity.Property(c => c.StartDate).IsRequired();
            entity.Property(c => c.Comments).HasMaxLength(1000);
            entity.Property(c => c.CreatedAt).IsRequired();

            // Indexes for querying containers by type, status, and date range
            entity.HasIndex(c => c.UserId);
            entity.HasIndex(c => new { c.UserId, c.Type, c.Status });
            entity.HasIndex(c => new { c.UserId, c.StartDate, c.EndDate });
        });

        // ContainerActivity configuration (JUNCTION TABLE)
        modelBuilder.Entity<ContainerActivity>(entity =>
        {
            // Composite primary key (ContainerId + ActivityTemplateId)
            entity.HasKey(ca => new { ca.ContainerId, ca.ActivityTemplateId });

            entity.Property(ca => ca.AddedAt).IsRequired();
            entity.Property(ca => ca.Order).IsRequired();
            entity.Property(ca => ca.IsRolledOver).IsRequired();

            // Relationships - many-to-many via junction table
            entity.HasOne(ca => ca.Container)
                .WithMany(c => c.ContainerActivities)
                .HasForeignKey(ca => ca.ContainerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ca => ca.ActivityTemplate)
                .WithMany(at => at.ContainerActivities)
                .HasForeignKey(ca => ca.ActivityTemplateId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes for common queries
            entity.HasIndex(ca => ca.ContainerId);
            entity.HasIndex(ca => ca.ActivityTemplateId);
            entity.HasIndex(ca => new { ca.ContainerId, ca.Order }); // For ordered lists
            entity.HasIndex(ca => ca.CompletedAt); // For filtering completed/incomplete
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
        var now = DateTime.UtcNow;

        // Handle User timestamps
        var userEntries = ChangeTracker.Entries<User>()
            .Where(e => e.State == EntityState.Added);

        foreach (var entry in userEntries)
        {
            entry.Entity.CreatedAt = now;
        }

        // Handle Session timestamps
        var sessionEntries = ChangeTracker.Entries<Session>()
            .Where(e => e.State == EntityState.Added);

        foreach (var entry in sessionEntries)
        {
            entry.Entity.CreatedAt = now;
        }

        // Handle ActivityTemplate timestamps
        var templateEntries = ChangeTracker.Entries<ActivityTemplate>()
            .Where(e => e.State == EntityState.Added);

        foreach (var entry in templateEntries)
        {
            if (entry.Entity.CreatedAt == default)
            {
                entry.Entity.CreatedAt = now;
            }
        }

        // Handle Container timestamps
        var containerEntries = ChangeTracker.Entries<Container>()
            .Where(e => e.State == EntityState.Added);

        foreach (var entry in containerEntries)
        {
            if (entry.Entity.CreatedAt == default)
            {
                entry.Entity.CreatedAt = now;
            }
        }

        // Handle ContainerActivity timestamps
        var containerActivityEntries = ChangeTracker.Entries<ContainerActivity>()
            .Where(e => e.State == EntityState.Added);

        foreach (var entry in containerActivityEntries)
        {
            if (entry.Entity.AddedAt == default)
            {
                entry.Entity.AddedAt = now;
            }
        }
    }
}
