namespace LifeSprint.Core.Models;

/// <summary>
/// Unified container for all backlog/sprint types (Annual/Monthly/Weekly/Daily).
/// Uses Type discriminator to differentiate timescales.
/// </summary>
/// <remarks>
/// Related files:
/// - Enums: ContainerType.cs, ContainerStatus.cs
/// - Junction: ContainerActivity.cs
/// - Service: LifeSprint.Infrastructure/Services/ContainerService.cs
/// </remarks>
public class Container
{
    public required int Id { get; set; }
    public required string UserId { get; set; }
    public required ContainerType Type { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; } // Nullable for ongoing backlogs, or future planning

    public required ContainerStatus Status { get; set; }

    public string? Comments { get; set; } // Optional notes about this container/sprint

    public DateTime CreatedAt { get; set; }
    public DateTime? ArchivedAt { get; set; } // Soft delete for historical tracking

    // Navigation property - activities in this container
    public ICollection<ContainerActivity> ContainerActivities { get; set; } = new List<ContainerActivity>();
}
