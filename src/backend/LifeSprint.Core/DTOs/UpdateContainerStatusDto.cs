using System.ComponentModel.DataAnnotations;

namespace LifeSprint.Core.DTOs;

/// <summary>
/// DTO for updating container status.
/// </summary>
public record UpdateContainerStatusDto
{
    /// <summary>
    /// New status for the container (Active, Completed, Archived).
    /// </summary>
    [Required]
    public ContainerStatus Status { get; init; }
}
