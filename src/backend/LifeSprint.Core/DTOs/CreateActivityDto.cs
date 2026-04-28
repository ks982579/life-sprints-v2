using System.ComponentModel.DataAnnotations;

namespace LifeSprint.Core.DTOs;

/// <summary>
/// DTO for creating a new activity template.
/// </summary>
/// <remarks>
/// Related files:
/// - Model: LifeSprint.Core/Models/ActivityTemplate.cs
/// - Service: LifeSprint.Infrastructure/Services/ActivityService.cs
/// - Controller: LifeSprint.Api/Controllers/ActivitiesController.cs
/// </remarks>
public record CreateActivityDto
{
    /// <summary>
    /// Title of the activity (required, max 500 chars).
    /// </summary>
    [Required(ErrorMessage = "Title is required")]
    [MaxLength(500, ErrorMessage = "Title cannot exceed 500 characters")]
    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// Optional detailed description of the activity.
    /// </summary>
    [MaxLength(5000, ErrorMessage = "Description cannot exceed 5000 characters")]
    public string? Description { get; init; }

    /// <summary>
    /// Type of activity: Project, Epic, Story, or Task.
    /// </summary>
    [Required(ErrorMessage = "Activity type is required")]
    public ActivityType Type { get; init; }

    /// <summary>
    /// Optional: Parent activity ID for hierarchy (e.g., Story belongs to Epic).
    /// </summary>
    public int? ParentActivityId { get; init; }

    /// <summary>
    /// Is this a recurring activity? (e.g., "Weekly Planning", "Monthly Review")
    /// </summary>
    public bool IsRecurring { get; init; }

    /// <summary>
    /// How often does this activity recur? (Only relevant if IsRecurring = true)
    /// </summary>
    public RecurrenceType RecurrenceType { get; init; } = RecurrenceType.None;

    /// <summary>
    /// Optional: ID of the container to add this activity to.
    /// If null, falls back to ContainerType or defaults to Annual.
    /// </summary>
    public int? ContainerId { get; init; }

    /// <summary>
    /// Optional: Container type to resolve the current container when ContainerId is not provided.
    /// Defaults to Annual if neither ContainerId nor DefaultContainerType is provided.
    /// </summary>
    public ContainerType? DefaultContainerType { get; init; }

    /// <summary>
    /// When true, the activity is created as a pure template with no container association.
    /// Used for recurring item templates created from the Recurring Items section.
    /// </summary>
    public bool SkipContainerLink { get; init; } = false;
}
