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

    #region GetContainersForUserAsync Tests

    [Fact]
    public async Task GetContainersForUserAsync_WithNoContainers_ReturnsEmptyList()
    {
        // Act
        var result = await _service.GetContainersForUserAsync(TestUserId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetContainersForUserAsync_WithMultipleContainers_ReturnsAllOrderedByStartDate()
    {
        // Arrange
        var container1 = new Container
        {
            UserId = TestUserId,
            Type = ContainerType.Weekly,
            StartDate = DateTime.UtcNow.Date.AddDays(-14),
            EndDate = DateTime.UtcNow.Date.AddDays(-8),
            Status = ContainerStatus.Completed,
            CreatedAt = DateTime.UtcNow.AddDays(-14)
        };
        var container2 = new Container
        {
            UserId = TestUserId,
            Type = ContainerType.Weekly,
            StartDate = DateTime.UtcNow.Date.AddDays(-7),
            EndDate = DateTime.UtcNow.Date.AddDays(-1),
            Status = ContainerStatus.Completed,
            CreatedAt = DateTime.UtcNow.AddDays(-7)
        };
        var container3 = new Container
        {
            UserId = TestUserId,
            Type = ContainerType.Weekly,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(6),
            Status = ContainerStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Containers.AddRangeAsync(container1, container2, container3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetContainersForUserAsync(TestUserId);

        // Assert
        result.Should().HaveCount(3);
        result[0].Id.Should().Be(container3.Id); // Most recent first
        result[1].Id.Should().Be(container2.Id);
        result[2].Id.Should().Be(container1.Id);
    }

    [Fact]
    public async Task GetContainersForUserAsync_WithTypeFilter_ReturnsOnlyMatchingType()
    {
        // Arrange
        var weeklyContainer = new Container
        {
            UserId = TestUserId,
            Type = ContainerType.Weekly,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(6),
            Status = ContainerStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        var annualContainer = new Container
        {
            UserId = TestUserId,
            Type = ContainerType.Annual,
            StartDate = new DateTime(DateTime.UtcNow.Year, 1, 1),
            EndDate = new DateTime(DateTime.UtcNow.Year, 12, 31),
            Status = ContainerStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Containers.AddRangeAsync(weeklyContainer, annualContainer);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetContainersForUserAsync(TestUserId, ContainerType.Weekly);

        // Assert
        result.Should().HaveCount(1);
        result[0].Type.Should().Be(ContainerType.Weekly);
        result[0].Id.Should().Be(weeklyContainer.Id);
    }

    [Fact]
    public async Task GetContainersForUserAsync_WithDifferentUser_ReturnsOnlyOwnContainers()
    {
        // Arrange
        var ownContainer = new Container
        {
            UserId = TestUserId,
            Type = ContainerType.Daily,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date,
            Status = ContainerStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        var otherContainer = new Container
        {
            UserId = "other_user",
            Type = ContainerType.Daily,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date,
            Status = ContainerStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Containers.AddRangeAsync(ownContainer, otherContainer);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetContainersForUserAsync(TestUserId);

        // Assert
        result.Should().HaveCount(1);
        result[0].UserId.Should().Be(TestUserId);
    }

    [Fact]
    public async Task GetContainersForUserAsync_ReturnsActivityCounts()
    {
        // Arrange
        var container = new Container
        {
            UserId = TestUserId,
            Type = ContainerType.Weekly,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(6),
            Status = ContainerStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Containers.AddAsync(container);
        await _context.SaveChangesAsync();

        var activity1 = new ActivityTemplate
        {
            UserId = TestUserId,
            Title = "Activity 1",
            Type = ActivityType.Task,
            CreatedAt = DateTime.UtcNow
        };
        var activity2 = new ActivityTemplate
        {
            UserId = TestUserId,
            Title = "Activity 2",
            Type = ActivityType.Task,
            CreatedAt = DateTime.UtcNow
        };
        await _context.ActivityTemplates.AddRangeAsync(activity1, activity2);
        await _context.SaveChangesAsync();

        var containerActivity1 = new ContainerActivity
        {
            ContainerId = container.Id,
            ActivityTemplateId = activity1.Id,
            AddedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow, // Completed
            Order = 0
        };
        var containerActivity2 = new ContainerActivity
        {
            ContainerId = container.Id,
            ActivityTemplateId = activity2.Id,
            AddedAt = DateTime.UtcNow,
            CompletedAt = null, // Not completed
            Order = 1
        };
        await _context.ContainerActivities.AddRangeAsync(containerActivity1, containerActivity2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetContainersForUserAsync(TestUserId);

        // Assert
        result.Should().HaveCount(1);
        result[0].TotalActivities.Should().Be(2);
        result[0].CompletedActivities.Should().Be(1);
    }

    #endregion

    #region UpdateContainerStatusAsync Tests

    [Fact]
    public async Task UpdateContainerStatusAsync_WithValidContainer_UpdatesStatus()
    {
        // Arrange
        var container = new Container
        {
            UserId = TestUserId,
            Type = ContainerType.Weekly,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(6),
            Status = ContainerStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Containers.AddAsync(container);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.UpdateContainerStatusAsync(TestUserId, container.Id, ContainerStatus.Completed);

        // Assert
        result.Should().NotBeNull();
        result!.Status.Should().Be(ContainerStatus.Completed);

        // Verify database was updated
        var updatedContainer = await _context.Containers.FindAsync(container.Id);
        updatedContainer!.Status.Should().Be(ContainerStatus.Completed);
    }

    [Fact]
    public async Task UpdateContainerStatusAsync_WithWrongUserId_ReturnsNull()
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

        // Act - Try to update another user's container
        var result = await _service.UpdateContainerStatusAsync(TestUserId, container.Id, ContainerStatus.Completed);

        // Assert
        result.Should().BeNull();

        // Verify database was not updated
        var unchangedContainer = await _context.Containers.FindAsync(container.Id);
        unchangedContainer!.Status.Should().Be(ContainerStatus.Active);
    }

    [Fact]
    public async Task UpdateContainerStatusAsync_WithNonExistentContainer_ReturnsNull()
    {
        // Act
        var result = await _service.UpdateContainerStatusAsync(TestUserId, 999, ContainerStatus.Completed);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateContainerStatusAsync_ToArchived_UpdatesStatus()
    {
        // Arrange
        var container = new Container
        {
            UserId = TestUserId,
            Type = ContainerType.Monthly,
            StartDate = new DateTime(2025, 1, 1),
            EndDate = new DateTime(2025, 1, 31),
            Status = ContainerStatus.Completed,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Containers.AddAsync(container);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.UpdateContainerStatusAsync(TestUserId, container.Id, ContainerStatus.Archived);

        // Assert
        result.Should().NotBeNull();
        result!.Status.Should().Be(ContainerStatus.Archived);
    }

    #endregion
}
