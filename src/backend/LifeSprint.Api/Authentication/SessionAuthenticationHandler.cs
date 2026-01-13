using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using LifeSprint.Core.Interfaces;

namespace LifeSprint.Api.Authentication;

/// <summary>
/// Custom authentication handler that validates session cookies and creates claims principal.
/// </summary>
public class SessionAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IAuthService _authService;
    private const string SessionCookieName = "lifesprint_session";

    public SessionAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IAuthService authService)
        : base(options, logger, encoder)
    {
        _authService = authService;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Check if session cookie exists
        if (!Request.Cookies.TryGetValue(SessionCookieName, out var sessionToken))
        {
            return AuthenticateResult.NoResult();
        }

        if (string.IsNullOrWhiteSpace(sessionToken))
        {
            return AuthenticateResult.NoResult();
        }

        try
        {
            // Validate session token
            var session = await _authService.GetSessionAsync(sessionToken);

            if (session == null || session.User == null)
            {
                Logger.LogWarning("Invalid session token: {SessionToken}", sessionToken);
                return AuthenticateResult.Fail("Invalid session token");
            }

            // Check if session is expired
            if (session.ExpiresAt < DateTime.UtcNow)
            {
                Logger.LogWarning("Session expired for user: {UserId}", session.UserId);
                return AuthenticateResult.Fail("Session expired");
            }

            // Create claims from user
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, session.User.Id),
                new Claim(ClaimTypes.Name, session.User.GitHubUsername ?? string.Empty),
                new Claim(ClaimTypes.Email, session.User.Email ?? string.Empty),
                new Claim("avatar_url", session.User.AvatarUrl ?? string.Empty)
            };

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            Logger.LogDebug("Successfully authenticated user: {UserId}", session.User.Id);

            return AuthenticateResult.Success(ticket);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error authenticating session token");
            return AuthenticateResult.Fail("Authentication error");
        }
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = 401;
        return Task.CompletedTask;
    }

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = 403;
        return Task.CompletedTask;
    }
}
