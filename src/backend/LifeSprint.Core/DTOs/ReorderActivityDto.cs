using System.ComponentModel.DataAnnotations;

namespace LifeSprint.Core.DTOs;

public record ReorderActivityDto
{
    public int ContainerId { get; init; }

    [Required]
    public required string Direction { get; init; } // "up" or "down"
}
