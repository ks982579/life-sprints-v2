using Xunit;
using FluentAssertions;
using LifeSprint.Infrastructure.Services;
using LifeSprint.Infrastructure.Data;
using LifeSprint.Core.Models;
using LifeSprint.Core;
using Microsoft.EntityFrameworkCore;

namespace LifeSprint.Tests.Unit;

/// <summary>
/// Unit tests for ContainerService.
/// Tests container creation, date range calculations, and retrieval logic.
/// </summary>
/// <remarks>
/// Related files:
/// - Service: LifeSprint.Infrastructure/Services/ContainerService.cs
/// - Interface: LifeSprint.Core/Interfaces/IContainerService.cs
/// - Model: LifeSprint.Core/Models/Container.cs
/// </remarks>
[Trait("Category", "Unit")]
public class ContainerServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly ContainerService _service;
    private const string TestUserId = "test_user_123";

    public ContainerServiceTests()
    {
        // Setup in-memory database with unique name per test instance
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);
        _service = new ContainerService(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region GetOrCreateCurrentContainerAsync Tests

    [Fact]
    public async Task GetOrCreateCurrentContainerAsync_Annual_CreatesNewContainerIfNotExists()
    {
        // Arrange
        var today = DateTime.UtcNow.Date;
        var expectedStartDate = new DateTime(today.Year, 1, 1);
        var expectedEndDate = new DateTime(today.Year, 12, 31);

        // Act
        var container = await _service.GetOrCreateCurrentContainerAsync(TestUserId, ContainerType.Annual);

        // Assert
        container.Should().NotBeNull();
        container.UserId.Should().Be(TestUserId);
        container.Type.Should().Be(ContainerType.Annual);
        container.Status.Should().Be(ContainerStatus.Active);
        container.StartDate.Should().Be(expectedStartDate);
        container.EndDate.Should().Be(expectedEndDate);
        container.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // Verify it was saved to database
        var savedContainer = await _context.Containers.FindAsync(container.Id);
        savedContainer.Should().NotBeNull();
    }

    [Fact]
    public async Task GetOrCreateCurrentContainerAsync_Annual_ReturnsExistingIfActive()
    {
        // Arrange
        var today = DateTime.UtcNow.Date;
        var startDate = new DateTime(today.Year, 1, 1);
        var existingContainer = new Container
        {
            UserId = TestUserId,
            Type = ContainerType.Annual,
            StartDate = startDate,
            EndDate = new DateTime(today.Year, 12, 31),
            Status = ContainerStatus.Active,
            CreatedAt = DateTime.UtcNow.AddDays(-10)
        };
        await _context.Containers.AddAsync(existingContainer);
        await _context.SaveChangesAsync();

        // Act
        var container = await _service.GetOrCreateCurrentContainerAsync(TestUserId, ContainerType.Annual);

        // Assert
        container.Id.Should().Be(existingContainer.Id);
        container.CreatedAt.Should().Be(existingContainer.CreatedAt);

        // Verify no new container was created
        var allContainers = await _context.Containers.ToListAsync();
        allContainers.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetOrCreateCurrentContainerAsync_Monthly_CreatesWithCorrectDateRange()
    {
        // Arrange
        var today = DateTime.UtcNow.Date;
        var expectedStartDate = new DateTime(today.Year, today.Month, 1);
        var expectedEndDate = expectedStartDate.AddMonths(1).AddDays(-1);

        // Act
        var container = await _service.GetOrCreateCurrentContainerAsync(TestUserId, ContainerType.Monthly);

        // Assert
        container.Should().NotBeNull();
        container.Type.Should().Be(ContainerType.Monthly);
        container.StartDate.Should().Be(expectedStartDate);
        container.EndDate.Should().Be(expectedEndDate);
        container.Status.Should().Be(ContainerStatus.Active);
    }

    [Fact]
    public async Task GetOrCreateCurrentContainerAsync_Weekly_CreatesWithCorrectDateRange()
    {
        // Arrange
        var today = DateTime.UtcNow.Date;
        var dayOfWeek = (int)today.DayOfWeek;
        var daysSinceMonday = dayOfWeek == 0 ? 6 : dayOfWeek - 1; // Sunday = 0 in C#, should be 6
        var expectedStartDate = today.AddDays(-daysSinceMonday);
        var expectedEndDate = expectedStartDate.AddDays(6);

        // Act
        var container = await _service.GetOrCreateCurrentContainerAsync(TestUserId, ContainerType.Weekly);

        // Assert
        container.Should().NotBeNull();
        container.Type.Should().Be(ContainerType.Weekly);
        container.StartDate.Should().Be(expectedStartDate);
        container.EndDate.Should().Be(expectedEndDate);
        container.Status.Should().Be(ContainerStatus.Active);
    }

    [Fact]
    public async Task GetOrCreateCurrentContainerAsync_Daily_CreatesWithSameStartAndEndDate()
    {
        // Arrange
        var today = DateTime.UtcNow.Date;

        // Act
        var container = await _service.GetOrCreateCurrentContainerAsync(TestUserId, ContainerType.Daily);

        // Assert
        container.Should().NotBeNull();
        container.Type.Should().Be(ContainerType.Daily);
        container.StartDate.Should().Be(today);
        container.EndDate.Should().Be(today);
        container.Status.Should().Be(ContainerStatus.Active);
    }

    [Fact]
    public async Task GetOrCreateCurrentContainerAsync_DifferentUsers_CreatesSeparateContainers()
    {
        // Arrange
        const string user1 = "user_1";
        const string user2 = "user_2";

        // Act
        var container1 = await _service.GetOrCreateCurrentContainerAsync(user1, ContainerType.Annual);
        var container2 = await _service.GetOrCreateCurrentContainerAsync(user2, ContainerType.Annual);

        // Assert
        container1.Id.Should().NotBe(container2.Id);
        container1.UserId.Should().Be(user1);
        container2.UserId.Should().Be(user2);

        var allContainers = await _context.Containers.ToListAsync();
        allContainers.Should().HaveCount(2);
    }

    #endregion

    #region GetContainerAsync Tests

    [Fact]
    public async Task GetContainerAsync_WithValidId_ReturnsContainer()
    {
        // Arrange
        var container = new Container
        {
            UserId = TestUserId,
            Type = ContainerType.Monthly,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(30),
            Status = ContainerStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Containers.AddAsync(container);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetContainerAsync(TestUserId, container.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(container.Id);
        result.Type.Should().Be(ContainerType.Monthly);
    }

    [Fact]
    public async Task GetContainerAsync_WithWrongUserId_ReturnsNull()
    {
        // Arrange
        var container = new Container
        {
            UserId = "other_user",
            Type = ContainerType.Weekly,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(6),
            Status = ContainerStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Containers.AddAsync(container);
        await _context.SaveChangesAsync();

        // Act - Try to access another user's container
        var result = await _service.GetContainerAsync(TestUserId, container.Id);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetContainerAsync_WithNonExistentId_ReturnsNull()
    {
        // Act
        var result = await _service.GetContainerAsync(TestUserId, 999);

        // Assert
        result.Should().BeNull();
    }

    #endregion
}
