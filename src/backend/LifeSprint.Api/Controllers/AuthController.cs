using LifeSprint.Core.DTOs;
using LifeSprint.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LifeSprint.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;
    private const string SessionCookieName = "lifesprint_session";

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Initiates GitHub OAuth flow
    /// </summary>
    [HttpGet("github/login")]
    public IActionResult GitHubLogin()
    {
        // Generate CSRF state token
        var state = Guid.NewGuid().ToString();

        // Store state in session/cookie for validation (simplified for now)
        Response.Cookies.Append("oauth_state", state, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            MaxAge = TimeSpan.FromMinutes(10)
        });

        var authUrl = _authService.GetAuthorizationUrl(state);

        _logger.LogInformation("Redirecting to GitHub OAuth: {AuthUrl}", authUrl);

        return Redirect(authUrl);
    }

    /// <summary>
    /// Handles GitHub OAuth callback
    /// </summary>
    [HttpGet("github/callback")]
    public async Task<IActionResult> GitHubCallback([FromQuery] string code, [FromQuery] string state)
    {
        if (string.IsNullOrEmpty(code))
        {
            _logger.LogWarning("GitHub callback received without code");
            return BadRequest("Authorization code is required");
        }

        // Validate state token (CSRF protection)
        if (!Request.Cookies.TryGetValue("oauth_state", out var storedState) || storedState != state)
        {
            _logger.LogWarning("Invalid OAuth state token");
            return BadRequest("Invalid state token");
        }

        // Clear the state cookie
        Response.Cookies.Delete("oauth_state");

        try
        {
            // Exchange code for user
            var user = await _authService.HandleCallbackAsync(code, state);

            // Create session
            var session = await _authService.CreateSessionAsync(user.Id);

            // Set session cookie
            Response.Cookies.Append(SessionCookieName, session.SessionToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Expires = session.ExpiresAt
            });

            _logger.LogInformation("User {Username} logged in successfully", user.GitHubUsername);

            // Redirect to frontend
            var frontendUrl = Request.Headers["Referer"].ToString();
            if (string.IsNullOrEmpty(frontendUrl))
            {
                frontendUrl = "http://localhost:3000"; // Default for development
            }

            return Redirect(frontendUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during GitHub OAuth callback");
            return StatusCode(500, "Authentication failed");
        }
    }

    /// <summary>
    /// Logs out the current user
    /// </summary>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        if (Request.Cookies.TryGetValue(SessionCookieName, out var sessionToken))
        {
            await _authService.DeleteSessionAsync(sessionToken);
            Response.Cookies.Delete(SessionCookieName);

            _logger.LogInformation("User logged out successfully");
        }

        return Ok(new { message = "Logged out successfully" });
    }

    /// <summary>
    /// Test-only login endpoint for E2E testing (Development/Test environments only)
    /// </summary>
    [HttpPost("test-login")]
    public async Task<IActionResult> TestLogin([FromBody] TestLoginDto request)
    {
        // Only allow in Development or Test environments
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        if (environment != "Development" && environment != "Test")
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(request.Username))
        {
            return BadRequest("Username is required");
        }

        try
        {
            // Create or get test user
            var user = await _authService.CreateOrGetTestUserAsync(
                request.Username,
                request.Email,
                request.AvatarUrl
            );

            // Create session
            var session = await _authService.CreateSessionAsync(user.Id);

            // Set session cookie
            Response.Cookies.Append(SessionCookieName, session.SessionToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Expires = session.ExpiresAt
            });

            _logger.LogInformation("Test user {Username} logged in successfully", user.GitHubUsername);

            return Ok(new CurrentUserDto
            {
                Id = user.Id,
                GitHubUsername = user.GitHubUsername,
                Email = user.Email,
                AvatarUrl = user.AvatarUrl
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during test login");
            return StatusCode(500, "Test login failed");
        }
    }

    /// <summary>
    /// Gets the current authenticated user
    /// </summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        if (!Request.Cookies.TryGetValue(SessionCookieName, out var sessionToken))
        {
            return Unauthorized();
        }

        var session = await _authService.GetSessionAsync(sessionToken);

        if (session == null || session.User == null)
        {
            Response.Cookies.Delete(SessionCookieName);
            return Unauthorized();
        }

        var userDto = new CurrentUserDto
        {
            Id = session.User.Id,
            GitHubUsername = session.User.GitHubUsername,
            Email = session.User.Email,
            AvatarUrl = session.User.AvatarUrl
        };

        return Ok(userDto);
    }
}
