namespace LifeSprint.Core.Models;

/// <summary>
/// Master task/goal definition. Can be instantiated into multiple containers.
/// Supports recurring tasks (e.g., "Weekly Planning", "Annual Review").
/// </summary>
/// <remarks>
/// Related files:
/// - Enums: RecurrenceType.cs
/// - Junction: ContainerActivity.cs
/// - DTOs: CreateActivityDto.cs, ActivityResponseDto.cs
/// - Service: LifeSprint.Infrastructure/Services/ActivityService.cs
/// </remarks>
public class ActivityTemplate
{
    public required int Id { get; set; }
    public required string UserId { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }

    public bool IsRecurring { get; set; } // Can this template be automatically added to new containers?
    public RecurrenceType RecurrenceType { get; set; } // How often does it recur?

    public DateTime CreatedAt { get; set; }
    public DateTime? ArchivedAt { get; set; } // Soft delete for historical tracking

    // Navigation property - containers this template appears in
    public ICollection<ContainerActivity> ContainerActivities { get; set; } = new List<ContainerActivity>();
}
