using Xunit;
using Moq;
using FluentAssertions;
using LifeSprint.Infrastructure.Services;
using LifeSprint.Infrastructure.Data;
using LifeSprint.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace LifeSprint.Tests.Unit;

[Trait("Category", "Unit")]
public class GitHubAuthServiceTests
{
    private readonly Mock<IConfiguration> _mockConfig;
    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
    private readonly AppDbContext _context;
    private readonly GitHubAuthService _service;

    public GitHubAuthServiceTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);

        // Setup mock configuration
        _mockConfig = new Mock<IConfiguration>();
        _mockConfig.Setup(c => c["GitHub:ClientId"]).Returns("test_client_id");
        _mockConfig.Setup(c => c["GitHub:ClientSecret"]).Returns("test_client_secret");
        _mockConfig.Setup(c => c["GitHub:CallbackUrl"]).Returns("http://localhost/api/auth/github/callback");

        _mockHttpClientFactory = new Mock<IHttpClientFactory>();

        _service = new GitHubAuthService(_context, _mockConfig.Object, _mockHttpClientFactory.Object);
    }

    [Fact]
    public void GetAuthorizationUrl_ShouldReturnCorrectGitHubUrl()
    {
        // Arrange
        var state = "random_state_token";

        // Act
        var url = _service.GetAuthorizationUrl(state);

        // Assert
        url.Should().NotBeNullOrEmpty();
        url.Should().Contain("https://github.com/login/oauth/authorize");
        url.Should().Contain("client_id=test_client_id");
        url.Should().Contain($"state={state}");
        url.Should().Contain("scope=read%3Auser%20user%3Aemail"); // URL-encoded: : becomes %3A, space becomes %20
    }

    [Fact]
    public async Task CreateSessionAsync_ShouldCreateValidSession()
    {
        // Arrange
        var user = new User
        {
            Id = "github_123",
            GitHubUsername = "testuser",
            Email = "test@example.com",
            CreatedAt = DateTime.UtcNow
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        var session = await _service.CreateSessionAsync(user.Id);

        // Assert
        session.Should().NotBeNull();
        session.UserId.Should().Be(user.Id);
        session.SessionToken.Should().NotBeNullOrEmpty();
        session.SessionToken.Length.Should().BeGreaterThan(32); // Should be a long random token
        session.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
        session.ExpiresAt.Should().BeBefore(DateTime.UtcNow.AddDays(31)); // Should expire in ~30 days

        // Verify it was saved to database
        var savedSession = await _context.Sessions.FindAsync(session.Id);
        savedSession.Should().NotBeNull();
        savedSession!.SessionToken.Should().Be(session.SessionToken);
    }

    [Fact]
    public async Task GetSessionAsync_WithValidToken_ShouldReturnSession()
    {
        // Arrange
        var user = new User
        {
            Id = "github_456",
            GitHubUsername = "testuser2",
            CreatedAt = DateTime.UtcNow
        };
        await _context.Users.AddAsync(user);

        var session = new Session
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            SessionToken = "valid_token_12345",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };
        await _context.Sessions.AddAsync(session);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetSessionAsync("valid_token_12345");

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(session.Id);
        result.UserId.Should().Be(user.Id);
        result.User.Should().NotBeNull();
        result.User!.GitHubUsername.Should().Be("testuser2");
    }

    [Fact]
    public async Task GetSessionAsync_WithExpiredToken_ShouldReturnNull()
    {
        // Arrange
        var session = new Session
        {
            Id = Guid.NewGuid(),
            UserId = "github_789",
            SessionToken = "expired_token",
            ExpiresAt = DateTime.UtcNow.AddDays(-1), // Expired yesterday
            CreatedAt = DateTime.UtcNow.AddDays(-8)
        };
        await _context.Sessions.AddAsync(session);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetSessionAsync("expired_token");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetSessionAsync_WithInvalidToken_ShouldReturnNull()
    {
        // Act
        var result = await _service.GetSessionAsync("non_existent_token");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteSessionAsync_ShouldRemoveSession()
    {
        // Arrange
        var session = new Session
        {
            Id = Guid.NewGuid(),
            UserId = "github_999",
            SessionToken = "token_to_delete",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };
        await _context.Sessions.AddAsync(session);
        await _context.SaveChangesAsync();

        // Act
        await _service.DeleteSessionAsync("token_to_delete");

        // Assert
        var deletedSession = await _context.Sessions.FindAsync(session.Id);
        deletedSession.Should().BeNull();
    }

    [Fact]
    public async Task DeleteSessionAsync_WithNonExistentToken_ShouldNotThrow()
    {
        // Act & Assert
        var act = async () => await _service.DeleteSessionAsync("non_existent_token");
        await act.Should().NotThrowAsync();
    }
}
