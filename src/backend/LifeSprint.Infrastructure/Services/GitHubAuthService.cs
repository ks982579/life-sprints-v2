using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LifeSprint.Core.DTOs;
using LifeSprint.Core.Interfaces;
using LifeSprint.Core.Models;
using LifeSprint.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace LifeSprint.Infrastructure.Services;

public class GitHubAuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;

    public GitHubAuthService(
        AppDbContext context,
        IConfiguration config,
        IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _config = config;
        _httpClientFactory = httpClientFactory;
    }

    public string GetAuthorizationUrl(string state)
    {
        var clientId = _config["GitHub:ClientId"];
        var callbackUrl = _config["GitHub:CallbackUrl"];
        var scope = "read:user user:email";

        return $"https://github.com/login/oauth/authorize?" +
               $"client_id={clientId}&" +
               $"redirect_uri={Uri.EscapeDataString(callbackUrl!)}&" +
               $"scope={Uri.EscapeDataString(scope)}&" +
               $"state={state}";
    }

    public async Task<User> HandleCallbackAsync(string code, string state)
    {
        // Exchange code for access token
        var accessToken = await ExchangeCodeForTokenAsync(code);

        // Get user info from GitHub
        var githubUser = await GetGitHubUserAsync(accessToken);

        // Create or update user in database
        var user = await _context.Users.FindAsync(githubUser.Id.ToString());

        if (user == null)
        {
            user = new User
            {
                Id = githubUser.Id.ToString(),
                GitHubUsername = githubUser.Login,
                Email = githubUser.Email,
                AvatarUrl = githubUser.AvatarUrl,
                AccessToken = accessToken, // TODO: Encrypt in production
                CreatedAt = DateTime.UtcNow
            };
            _context.Users.Add(user);
        }
        else
        {
            user.GitHubUsername = githubUser.Login;
            user.Email = githubUser.Email;
            user.AvatarUrl = githubUser.AvatarUrl;
            user.AccessToken = accessToken;
            user.LastLoginAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return user;
    }

    public async Task<Session> CreateSessionAsync(string userId)
    {
        var session = new Session
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SessionToken = GenerateSecureToken(),
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow
        };

        _context.Sessions.Add(session);
        await _context.SaveChangesAsync();

        return session;
    }

    public async Task<Session?> GetSessionAsync(string sessionToken)
    {
        var session = await _context.Sessions
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.SessionToken == sessionToken);

        if (session == null || session.ExpiresAt < DateTime.UtcNow)
        {
            return null;
        }

        return session;
    }

    public async Task DeleteSessionAsync(string sessionToken)
    {
        var session = await _context.Sessions
            .FirstOrDefaultAsync(s => s.SessionToken == sessionToken);

        if (session != null)
        {
            _context.Sessions.Remove(session);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<User> CreateOrGetTestUserAsync(string username, string? email = null, string? avatarUrl = null)
    {
        // Use a consistent ID format for test users
        var testUserId = $"test_{username}";

        // Check if user already exists
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == testUserId);

        if (existingUser != null)
        {
            return existingUser;
        }

        // Create new test user
        var user = new User
        {
            Id = testUserId,
            GitHubUsername = username,
            Email = email ?? $"{username}@test.local",
            AvatarUrl = avatarUrl ?? $"https://api.dicebear.com/7.x/avataaars/svg?seed={username}",
            AccessToken = null, // No real token for test users
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return user;
    }

    private async Task<string> ExchangeCodeForTokenAsync(string code)
    {
        var clientId = _config["GitHub:ClientId"];
        var clientSecret = _config["GitHub:ClientSecret"];

        var client = _httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "https://github.com/login/oauth/access_token");

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "client_id", clientId! },
            { "client_secret", clientSecret! },
            { "code", code }
        });

        request.Content = content;
        request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        var tokenResponse = JsonSerializer.Deserialize<GitHubTokenResponse>(responseBody);

        return tokenResponse?.AccessToken ?? throw new Exception("Failed to get access token from GitHub");
    }

    private async Task<GitHubUserResponse> GetGitHubUserAsync(string accessToken)
    {
        var client = _httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user");

        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("LifeSprint", "1.0"));

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        var githubUser = JsonSerializer.Deserialize<GitHubUserResponse>(responseBody);

        return githubUser ?? throw new Exception("Failed to get user info from GitHub");
    }

    private static string GenerateSecureToken()
    {
        var randomBytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }
        return Convert.ToBase64String(randomBytes);
    }
}
