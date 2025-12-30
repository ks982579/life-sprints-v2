namespace LifeSprint.Core.Models;

public class User
{
    public string Id { get; set; } = string.Empty; // GitHub user ID
    public string GitHubUsername { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? AvatarUrl { get; set; }
    public string? AccessToken { get; set; } // Encrypted in production
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }

    // Navigation properties
    public ICollection<Activity> Activities { get; set; } = new List<Activity>();
    public ICollection<Session> Sessions { get; set; } = new List<Session>();
}
