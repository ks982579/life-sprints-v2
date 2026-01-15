namespace LifeSprint.Core.DTOs;

/// <summary>
/// DTO for container response with activity counts.
/// </summary>
public record ContainerResponseDto
{
    public int Id { get; init; }
    public string UserId { get; init; } = string.Empty;
    public ContainerType Type { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public ContainerStatus Status { get; init; }
    public string? Comments { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ArchivedAt { get; init; }

    /// <summary>
    /// Total number of activities in this container.
    /// </summary>
    public int TotalActivities { get; init; }

    /// <summary>
    /// Number of completed activities in this container.
    /// </summary>
    public int CompletedActivities { get; init; }
}
