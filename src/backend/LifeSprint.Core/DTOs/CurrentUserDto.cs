namespace LifeSprint.Core.DTOs;

public class CurrentUserDto
{
    public string Id { get; set; } = string.Empty;
    public string GitHubUsername { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? AvatarUrl { get; set; }
}
