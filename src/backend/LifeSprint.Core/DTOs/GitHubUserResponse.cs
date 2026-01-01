using System.Text.Json.Serialization;

namespace LifeSprint.Core.DTOs;

public class GitHubUserResponse
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("login")]
    public string Login { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("avatar_url")]
    public string? AvatarUrl { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}
