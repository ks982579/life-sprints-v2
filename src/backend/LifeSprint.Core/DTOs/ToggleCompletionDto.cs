using System.ComponentModel.DataAnnotations;

namespace LifeSprint.Core.DTOs;

/// <summary>
/// DTO for toggling activity completion status within a container.
/// </summary>
public record ToggleCompletionDto
{
    /// <summary>
    /// Container ID where the activity completion should be toggled.
    /// </summary>
    [Required]
    public int ContainerId { get; init; }

    /// <summary>
    /// True to mark as completed, false to mark as incomplete.
    /// </summary>
    [Required]
    public bool IsCompleted { get; init; }
}
