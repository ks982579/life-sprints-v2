namespace LifeSprint.Core.Models;

public class Session
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public User? User { get; set; }
    public string SessionToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
