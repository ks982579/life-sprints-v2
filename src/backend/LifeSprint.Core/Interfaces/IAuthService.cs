using LifeSprint.Core.Models;

namespace LifeSprint.Core.Interfaces;

public interface IAuthService
{
    /// <summary>
    /// Generates the GitHub OAuth authorization URL
    /// </summary>
    /// <param name="state">CSRF protection state token</param>
    /// <returns>GitHub authorization URL</returns>
    string GetAuthorizationUrl(string state);

    /// <summary>
    /// Handles the OAuth callback and creates/updates user
    /// </summary>
    /// <param name="code">Authorization code from GitHub</param>
    /// <param name="state">State token for CSRF validation</param>
    /// <returns>User entity</returns>
    Task<User> HandleCallbackAsync(string code, string state);

    /// <summary>
    /// Creates a new session for the user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Session entity</returns>
    Task<Session> CreateSessionAsync(string userId);

    /// <summary>
    /// Validates and retrieves a session
    /// </summary>
    /// <param name="sessionToken">Session token</param>
    /// <returns>Session entity or null if invalid/expired</returns>
    Task<Session?> GetSessionAsync(string sessionToken);

    /// <summary>
    /// Deletes a session (logout)
    /// </summary>
    /// <param name="sessionToken">Session token</param>
    Task DeleteSessionAsync(string sessionToken);
}
