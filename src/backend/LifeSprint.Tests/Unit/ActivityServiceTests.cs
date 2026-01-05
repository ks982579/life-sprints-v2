using Xunit;
using Moq;
using FluentAssertions;
using LifeSprint.Infrastructure.Services;
using LifeSprint.Infrastructure.Data;
using LifeSprint.Core.Models;
using LifeSprint.Core;
using LifeSprint.Core.DTOs;
using LifeSprint.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LifeSprint.Tests.Unit;

/// <summary>
/// Unit tests for ActivityService.
/// Tests activity creation, retrieval, and container associations.
/// </summary>
/// <remarks>
/// Related files:
/// - Service: LifeSprint.Infrastructure/Services/ActivityService.cs
/// - Interface: LifeSprint.Core/Interfaces/IActivityService.cs
/// - DTOs: LifeSprint.Core/DTOs/CreateActivityDto.cs, ActivityResponseDto.cs
/// - Dependencies: IContainerService (mocked)
/// </remarks>
[Trait("Category", "Unit")]
public class ActivityServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly Mock<IContainerService> _mockContainerService;
    private readonly ActivityService _service;
    private const string TestUserId = "test_user_123";

    public ActivityServiceTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);

        // Setup mock container service
        _mockContainerService = new Mock<IContainerService>();

        _service = new ActivityService(_context, _mockContainerService.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region CreateActivityAsync Tests

    [Fact]
    public async Task CreateActivityAsync_WithoutContainer_AddsToAnnualBacklog()
    {
        // Arrange
        var annualContainer = new Container
        {
            Id = 1,
            UserId = TestUserId,
            Type = ContainerType.Annual,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 12, 31),
            Status = ContainerStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        // Add container to context so navigation properties work
        await _context.Containers.AddAsync(annualContainer);
        await _context.SaveChangesAsync();

        _mockContainerService
            .Setup(x => x.GetOrCreateCurrentContainerAsync(TestUserId, ContainerType.Annual))
            .ReturnsAsync(annualContainer);

        var dto = new CreateActivityDto
        {
            Title = "Learn C# async/await",
            Description = "Deep dive into async programming",
            IsRecurring = false,
            RecurrenceType = RecurrenceType.None,
            ContainerId = null // No container specified
        };

        // Act
        var result = await _service.CreateActivityAsync(TestUserId, dto);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Learn C# async/await");
        result.Description.Should().Be("Deep dive into async programming");
        result.UserId.Should().Be(TestUserId);
        result.IsRecurring.Should().BeFalse();
        result.Containers.Should().HaveCount(1);
        result.Containers[0].ContainerType.Should().Be(ContainerType.Annual);
        result.Containers[0].IsRolledOver.Should().BeFalse();
        result.Containers[0].Order.Should().Be(1);

        // Verify database state
        var savedActivity = await _context.ActivityTemplates.FindAsync(result.Id);
        savedActivity.Should().NotBeNull();
        savedActivity!.Title.Should().Be("Learn C# async/await");

        // Verify container service was called
        _mockContainerService.Verify(
            x => x.GetOrCreateCurrentContainerAsync(TestUserId, ContainerType.Annual),
            Times.Once);
    }

    [Fact]
    public async Task CreateActivityAsync_WithSpecificContainer_AddsToThatContainer()
    {
        // Arrange
        var weeklyContainer = new Container
        {
            Id = 5,
            UserId = TestUserId,
            Type = ContainerType.Weekly,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(6),
            Status = ContainerStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        // Add container to context so navigation properties work
        await _context.Containers.AddAsync(weeklyContainer);
        await _context.SaveChangesAsync();

        _mockContainerService
            .Setup(x => x.GetContainerAsync(TestUserId, 5))
            .ReturnsAsync(weeklyContainer);

        var dto = new CreateActivityDto
        {
            Title = "Write unit tests",
            Description = "Complete test coverage for ActivityService",
            IsRecurring = false,
            RecurrenceType = RecurrenceType.None,
            ContainerId = 5 // Specific container
        };

        // Act
        var result = await _service.CreateActivityAsync(TestUserId, dto);

        // Assert
        result.Should().NotBeNull();
        result.Containers.Should().HaveCount(1);
        result.Containers[0].ContainerId.Should().Be(5);
        result.Containers[0].ContainerType.Should().Be(ContainerType.Weekly);

        // Verify container service was called correctly
        _mockContainerService.Verify(
            x => x.GetContainerAsync(TestUserId, 5),
            Times.Once);
        _mockContainerService.Verify(
            x => x.GetOrCreateCurrentContainerAsync(It.IsAny<string>(), It.IsAny<ContainerType>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateActivityAsync_WithInvalidContainerId_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _mockContainerService
            .Setup(x => x.GetContainerAsync(TestUserId, 999))
            .ReturnsAsync((Container?)null); // Container not found or unauthorized

        var dto = new CreateActivityDto
        {
            Title = "Test activity",
            ContainerId = 999
        };

        // Act & Assert
        var act = async () => await _service.CreateActivityAsync(TestUserId, dto);
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Container 999 not found or unauthorized");
    }

    [Fact]
    public async Task CreateActivityAsync_RecurringActivity_SetsRecurrenceCorrectly()
    {
        // Arrange
        var annualContainer = new Container
        {
            Id = 1,
            UserId = TestUserId,
            Type = ContainerType.Annual,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 12, 31),
            Status = ContainerStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        // Add container to context so navigation properties work
        await _context.Containers.AddAsync(annualContainer);
        await _context.SaveChangesAsync();

        _mockContainerService
            .Setup(x => x.GetOrCreateCurrentContainerAsync(TestUserId, ContainerType.Annual))
            .ReturnsAsync(annualContainer);

        var dto = new CreateActivityDto
        {
            Title = "Weekly sprint planning",
            Description = "Review and plan the week ahead",
            IsRecurring = true,
            RecurrenceType = RecurrenceType.Weekly
        };

        // Act
        var result = await _service.CreateActivityAsync(TestUserId, dto);

        // Assert
        result.Should().NotBeNull();
        result.IsRecurring.Should().BeTrue();
        result.RecurrenceType.Should().Be(RecurrenceType.Weekly);
    }

    [Fact]
    public async Task CreateActivityAsync_MultipleActivities_OrdersCorrectly()
    {
        // Arrange
        var container = new Container
        {
            Id = 1,
            UserId = TestUserId,
            Type = ContainerType.Annual,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 12, 31),
            Status = ContainerStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        // Add container to context so navigation properties work
        await _context.Containers.AddAsync(container);
        await _context.SaveChangesAsync();

        _mockContainerService
            .Setup(x => x.GetOrCreateCurrentContainerAsync(TestUserId, ContainerType.Annual))
            .ReturnsAsync(container);

        // Act - Create three activities
        var result1 = await _service.CreateActivityAsync(TestUserId, new CreateActivityDto { Title = "First" });
        var result2 = await _service.CreateActivityAsync(TestUserId, new CreateActivityDto { Title = "Second" });
        var result3 = await _service.CreateActivityAsync(TestUserId, new CreateActivityDto { Title = "Third" });

        // Assert
        result1.Containers[0].Order.Should().Be(1);
        result2.Containers[0].Order.Should().Be(2);
        result3.Containers[0].Order.Should().Be(3);
    }

    #endregion

    #region GetActivitiesForUserAsync Tests

    [Fact]
    public async Task GetActivitiesForUserAsync_ReturnsAllUserActivities()
    {
        // Arrange
        var container = new Container
        {
            UserId = TestUserId,
            Type = ContainerType.Annual,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddYears(1),
            Status = ContainerStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Containers.AddAsync(container);
        await _context.SaveChangesAsync();

        var activity1 = new ActivityTemplate
        {
            UserId = TestUserId,
            Title = "Activity 1",
            IsRecurring = false,
            RecurrenceType = RecurrenceType.None,
            CreatedAt = DateTime.UtcNow
        };
        var activity2 = new ActivityTemplate
        {
            UserId = TestUserId,
            Title = "Activity 2",
            IsRecurring = true,
            RecurrenceType = RecurrenceType.Weekly,
            CreatedAt = DateTime.UtcNow
        };
        await _context.ActivityTemplates.AddRangeAsync(activity1, activity2);
        await _context.SaveChangesAsync();

        // Add container associations
        await _context.ContainerActivities.AddRangeAsync(
            new ContainerActivity
            {
                ContainerId = container.Id,
                ActivityTemplateId = activity1.Id,
                AddedAt = DateTime.UtcNow,
                Order = 1,
                IsRolledOver = false
            },
            new ContainerActivity
            {
                ContainerId = container.Id,
                ActivityTemplateId = activity2.Id,
                AddedAt = DateTime.UtcNow,
                Order = 2,
                IsRolledOver = false
            }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetActivitiesForUserAsync(TestUserId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(a => a.Title == "Activity 1");
        result.Should().Contain(a => a.Title == "Activity 2");
        result.All(a => a.UserId == TestUserId).Should().BeTrue();
    }

    [Fact]
    public async Task GetActivitiesForUserAsync_DoesNotReturnOtherUsersActivities()
    {
        // Arrange
        var container1 = new Container
        {
            UserId = TestUserId,
            Type = ContainerType.Annual,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddYears(1),
            Status = ContainerStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        var container2 = new Container
        {
            UserId = "other_user",
            Type = ContainerType.Annual,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddYears(1),
            Status = ContainerStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Containers.AddRangeAsync(container1, container2);
        await _context.SaveChangesAsync();

        var myActivity = new ActivityTemplate
        {
            UserId = TestUserId,
            Title = "My Activity",
            IsRecurring = false,
            RecurrenceType = RecurrenceType.None,
            CreatedAt = DateTime.UtcNow
        };
        var otherActivity = new ActivityTemplate
        {
            UserId = "other_user",
            Title = "Other Activity",
            IsRecurring = false,
            RecurrenceType = RecurrenceType.None,
            CreatedAt = DateTime.UtcNow
        };
        await _context.ActivityTemplates.AddRangeAsync(myActivity, otherActivity);
        await _context.SaveChangesAsync();

        await _context.ContainerActivities.AddRangeAsync(
            new ContainerActivity
            {
                ContainerId = container1.Id,
                ActivityTemplateId = myActivity.Id,
                AddedAt = DateTime.UtcNow,
                Order = 1,
                IsRolledOver = false
            },
            new ContainerActivity
            {
                ContainerId = container2.Id,
                ActivityTemplateId = otherActivity.Id,
                AddedAt = DateTime.UtcNow,
                Order = 1,
                IsRolledOver = false
            }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetActivitiesForUserAsync(TestUserId);

        // Assert
        result.Should().HaveCount(1);
        result[0].Title.Should().Be("My Activity");
        result[0].UserId.Should().Be(TestUserId);
    }

    [Fact]
    public async Task GetActivitiesForUserAsync_DoesNotReturnArchivedActivities()
    {
        // Arrange
        var container = new Container
        {
            UserId = TestUserId,
            Type = ContainerType.Annual,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddYears(1),
            Status = ContainerStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Containers.AddAsync(container);
        await _context.SaveChangesAsync();

        var activeActivity = new ActivityTemplate
        {
            UserId = TestUserId,
            Title = "Active Activity",
            IsRecurring = false,
            RecurrenceType = RecurrenceType.None,
            CreatedAt = DateTime.UtcNow,
            ArchivedAt = null
        };
        var archivedActivity = new ActivityTemplate
        {
            UserId = TestUserId,
            Title = "Archived Activity",
            IsRecurring = false,
            RecurrenceType = RecurrenceType.None,
            CreatedAt = DateTime.UtcNow,
            ArchivedAt = DateTime.UtcNow.AddDays(-1) // Archived yesterday
        };
        await _context.ActivityTemplates.AddRangeAsync(activeActivity, archivedActivity);
        await _context.SaveChangesAsync();

        await _context.ContainerActivities.AddRangeAsync(
            new ContainerActivity
            {
                ContainerId = container.Id,
                ActivityTemplateId = activeActivity.Id,
                AddedAt = DateTime.UtcNow,
                Order = 1,
                IsRolledOver = false
            },
            new ContainerActivity
            {
                ContainerId = container.Id,
                ActivityTemplateId = archivedActivity.Id,
                AddedAt = DateTime.UtcNow,
                Order = 2,
                IsRolledOver = false
            }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetActivitiesForUserAsync(TestUserId);

        // Assert
        result.Should().HaveCount(1);
        result[0].Title.Should().Be("Active Activity");
    }

    [Fact]
    public async Task GetActivitiesForUserAsync_ReturnsActivitiesOrderedByCreatedDate()
    {
        // Arrange
        var container = new Container
        {
            UserId = TestUserId,
            Type = ContainerType.Annual,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddYears(1),
            Status = ContainerStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Containers.AddAsync(container);
        await _context.SaveChangesAsync();

        var activity1 = new ActivityTemplate
        {
            UserId = TestUserId,
            Title = "Oldest",
            IsRecurring = false,
            RecurrenceType = RecurrenceType.None,
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };
        var activity2 = new ActivityTemplate
        {
            UserId = TestUserId,
            Title = "Middle",
            IsRecurring = false,
            RecurrenceType = RecurrenceType.None,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };
        var activity3 = new ActivityTemplate
        {
            UserId = TestUserId,
            Title = "Newest",
            IsRecurring = false,
            RecurrenceType = RecurrenceType.None,
            CreatedAt = DateTime.UtcNow
        };
        await _context.ActivityTemplates.AddRangeAsync(activity1, activity2, activity3);
        await _context.SaveChangesAsync();

        await _context.ContainerActivities.AddRangeAsync(
            new ContainerActivity
            {
                ContainerId = container.Id,
                ActivityTemplateId = activity1.Id,
                AddedAt = DateTime.UtcNow,
                Order = 1,
                IsRolledOver = false
            },
            new ContainerActivity
            {
                ContainerId = container.Id,
                ActivityTemplateId = activity2.Id,
                AddedAt = DateTime.UtcNow,
                Order = 2,
                IsRolledOver = false
            },
            new ContainerActivity
            {
                ContainerId = container.Id,
                ActivityTemplateId = activity3.Id,
                AddedAt = DateTime.UtcNow,
                Order = 3,
                IsRolledOver = false
            }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetActivitiesForUserAsync(TestUserId);

        // Assert
        result.Should().HaveCount(3);
        result[0].Title.Should().Be("Newest"); // Most recent first
        result[1].Title.Should().Be("Middle");
        result[2].Title.Should().Be("Oldest");
    }

    #endregion

    #region GetActivityByIdAsync Tests

    [Fact]
    public async Task GetActivityByIdAsync_WithValidId_ReturnsActivity()
    {
        // Arrange
        var container = new Container
        {
            UserId = TestUserId,
            Type = ContainerType.Monthly,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddMonths(1),
            Status = ContainerStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Containers.AddAsync(container);
        await _context.SaveChangesAsync();

        var activity = new ActivityTemplate
        {
            UserId = TestUserId,
            Title = "Test Activity",
            Description = "Test Description",
            IsRecurring = false,
            RecurrenceType = RecurrenceType.None,
            CreatedAt = DateTime.UtcNow
        };
        await _context.ActivityTemplates.AddAsync(activity);
        await _context.SaveChangesAsync();

        await _context.ContainerActivities.AddAsync(new ContainerActivity
        {
            ContainerId = container.Id,
            ActivityTemplateId = activity.Id,
            AddedAt = DateTime.UtcNow,
            Order = 1,
            IsRolledOver = false
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetActivityByIdAsync(TestUserId, activity.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(activity.Id);
        result.Title.Should().Be("Test Activity");
        result.Description.Should().Be("Test Description");
        result.Containers.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetActivityByIdAsync_WithWrongUserId_ReturnsNull()
    {
        // Arrange
        var container = new Container
        {
            UserId = "other_user",
            Type = ContainerType.Annual,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddYears(1),
            Status = ContainerStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Containers.AddAsync(container);
        await _context.SaveChangesAsync();

        var activity = new ActivityTemplate
        {
            UserId = "other_user",
            Title = "Other User's Activity",
            IsRecurring = false,
            RecurrenceType = RecurrenceType.None,
            CreatedAt = DateTime.UtcNow
        };
        await _context.ActivityTemplates.AddAsync(activity);
        await _context.SaveChangesAsync();

        // Act - Try to access another user's activity
        var result = await _service.GetActivityByIdAsync(TestUserId, activity.Id);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetActivityByIdAsync_WithNonExistentId_ReturnsNull()
    {
        // Act
        var result = await _service.GetActivityByIdAsync(TestUserId, 999);

        // Assert
        result.Should().BeNull();
    }

    #endregion
}
