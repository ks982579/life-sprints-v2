namespace LifeSprint.Core.Models;

/// <summary>
/// Junction table linking ActivityTemplates to Containers (many-to-many).
/// Tracks completion status, order, and rollover state per container.
/// </summary>
/// <remarks>
/// Related files:
/// - Models: ActivityTemplate.cs, Container.cs
/// - DbContext: LifeSprint.Infrastructure/Data/AppDbContext.cs
/// - Tests: LifeSprint.Tests/Integration/ActivityServiceIntegrationTests.cs
/// </remarks>
public class ContainerActivity
{
    // Foreign keys (composite primary key)
    public required int ContainerId { get; set; }
    public required int ActivityTemplateId { get; set; }

    public DateTime AddedAt { get; set; }
    public DateTime? CompletedAt { get; set; } // Completion is tracked per-container

    public int Order { get; set; } // User-defined ordering within container
    public bool IsRolledOver { get; set; } // Did this task come from a previous sprint?

    // Navigation properties (one-to-one relationships in junction table)
    public Container Container { get; set; } = null!;
    public ActivityTemplate ActivityTemplate { get; set; } = null!;
}
