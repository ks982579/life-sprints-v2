using System.ComponentModel.DataAnnotations;

namespace LifeSprint.Core.DTOs;

/// <summary>
/// DTO for creating a new container (backlog/sprint) for the current period.
/// </summary>
public record CreateNewContainerDto
{
    /// <summary>
    /// The type of container to create (Annual/Monthly/Weekly/Daily).
    /// </summary>
    [Required]
    public ContainerType Type { get; init; }

    /// <summary>
    /// If true, incomplete items from the most recent previous container of the same
    /// type will be carried forward into the new container (IsRolledOver = true).
    /// </summary>
    public bool RolloverIncomplete { get; init; }
}
