namespace LifeSprint.Core.DTOs;

/// <summary>
/// DTO for returning activity data with container associations.
/// </summary>
/// <remarks>
/// Related files:
/// - Model: LifeSprint.Core/Models/ActivityTemplate.cs
/// - Service: LifeSprint.Infrastructure/Services/ActivityService.cs
/// - Controller: LifeSprint.Api/Controllers/ActivitiesController.cs
/// </remarks>
public record ActivityResponseDto
{
    /// <summary>
    /// Activity template ID.
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    /// User ID who owns this activity.
    /// </summary>
    public string UserId { get; init; } = string.Empty;

    /// <summary>
    /// Activity title.
    /// </summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// Activity description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Is this a recurring activity?
    /// </summary>
    public bool IsRecurring { get; init; }

    /// <summary>
    /// Recurrence frequency.
    /// </summary>
    public RecurrenceType RecurrenceType { get; init; }

    /// <summary>
    /// When this activity was created.
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// When this activity was archived (null if active).
    /// </summary>
    public DateTime? ArchivedAt { get; init; }

    /// <summary>
    /// Containers this activity appears in (with completion status per container).
    /// </summary>
    public List<ContainerAssociationDto> Containers { get; init; } = new();
}

/// <summary>
/// Represents an activity's association with a specific container.
/// Includes completion status which is tracked per-container.
/// </summary>
public record ContainerAssociationDto
{
    /// <summary>
    /// Container ID.
    /// </summary>
    public int ContainerId { get; init; }

    /// <summary>
    /// Container type (Annual, Monthly, Weekly, Daily).
    /// </summary>
    public ContainerType ContainerType { get; init; }

    /// <summary>
    /// When this activity was added to the container.
    /// </summary>
    public DateTime AddedAt { get; init; }

    /// <summary>
    /// When this activity was completed in this container (null if incomplete).
    /// </summary>
    public DateTime? CompletedAt { get; init; }

    /// <summary>
    /// Order within the container.
    /// </summary>
    public int Order { get; init; }

    /// <summary>
    /// Was this activity rolled over from a previous container?
    /// </summary>
    public bool IsRolledOver { get; init; }
}
