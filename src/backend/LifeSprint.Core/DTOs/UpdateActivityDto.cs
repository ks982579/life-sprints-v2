using System.ComponentModel.DataAnnotations;

namespace LifeSprint.Core.DTOs;

/// <summary>
/// DTO for updating an existing activity template.
/// </summary>
/// <remarks>
/// All fields are optional - only provided fields will be updated.
/// Related files:
/// - Model: LifeSprint.Core/Models/ActivityTemplate.cs
/// - Service: LifeSprint.Infrastructure/Services/ActivityService.cs
/// - Controller: LifeSprint.Api/Controllers/ActivitiesController.cs
/// </remarks>
public record UpdateActivityDto
{
    /// <summary>
    /// Title of the activity (max 500 chars).
    /// </summary>
    [MaxLength(500, ErrorMessage = "Title cannot exceed 500 characters")]
    public string? Title { get; init; }

    /// <summary>
    /// Detailed description of the activity.
    /// </summary>
    [MaxLength(5000, ErrorMessage = "Description cannot exceed 5000 characters")]
    public string? Description { get; init; }

    /// <summary>
    /// Type of activity: Project, Epic, Story, or Task.
    /// </summary>
    public ActivityType? Type { get; init; }

    /// <summary>
    /// Parent activity ID for hierarchy (e.g., Story belongs to Epic).
    /// Set to null to remove parent relationship.
    /// </summary>
    public int? ParentActivityId { get; init; }

    /// <summary>
    /// Is this a recurring activity?
    /// </summary>
    public bool? IsRecurring { get; init; }

    /// <summary>
    /// How often does this activity recur?
    /// </summary>
    public RecurrenceType? RecurrenceType { get; init; }
}
