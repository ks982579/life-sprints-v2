namespace LifeSprint.Core.DTOs;

public record TestLoginDto
{
    public string Username { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? AvatarUrl { get; init; }
}
