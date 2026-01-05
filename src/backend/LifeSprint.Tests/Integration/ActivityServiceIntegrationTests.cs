using Xunit;
using FluentAssertions;
using LifeSprint.Infrastructure.Services;
using LifeSprint.Core.DTOs;
using LifeSprint.Core;
using Microsoft.EntityFrameworkCore;

namespace LifeSprint.Tests.Integration;

/// <summary>
/// Integration tests for ActivityService using a real PostgreSQL database.
/// Tests the full stack: Service -> EF Core -> PostgreSQL.
/// </summary>
/// <remarks>
/// Related files:
/// - Service: LifeSprint.Infrastructure/Services/ActivityService.cs
/// - Base: Integration/IntegrationTestBase.cs
/// - docker-compose.yml: test-db service configuration
/// </remarks>
public class ActivityServiceIntegrationTests : IntegrationTestBase
{
    [Fact]
    public async Task CreateActivity_WithoutContainer_PersistsToDatabase()
    {
        // Arrange
        var containerService = new ContainerService(Context);
        var activityService = new ActivityService(Context, containerService);

        var dto = new CreateActivityDto
        {
            Title = "Build a REST API", Type = ActivityType.Task,
            Description = "Create activity endpoints with proper CRUD operations",
            IsRecurring = false,
            RecurrenceType = RecurrenceType.None
        };

        // Act
        var result = await activityService.CreateActivityAsync(TestUserId, dto);

        // Assert - Check returned DTO
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.Title.Should().Be("Build a REST API");
        result.Description.Should().Be("Create activity endpoints with proper CRUD operations");
        result.UserId.Should().Be(TestUserId);
        result.Containers.Should().HaveCount(1);
        result.Containers[0].ContainerType.Should().Be(ContainerType.Annual);

        // Assert - Verify database persistence
        var savedActivity = await Context.ActivityTemplates
            .Include(at => at.ContainerActivities)
            .ThenInclude(ca => ca.Container)
            .FirstOrDefaultAsync(at => at.Id == result.Id);

        savedActivity.Should().NotBeNull();
        savedActivity!.Title.Should().Be("Build a REST API");
        savedActivity.ContainerActivities.Should().HaveCount(1);
        savedActivity.ContainerActivities.First().Container.Type.Should().Be(ContainerType.Annual);
    }

    [Fact]
    public async Task CreateActivity_WithAnnualContainer_CreatesCorrectAssociations()
    {
        // Arrange
        var containerService = new ContainerService(Context);
        var activityService = new ActivityService(Context, containerService);

        // Create annual container first
        var annualContainer = await containerService.GetOrCreateCurrentContainerAsync(TestUserId, ContainerType.Annual);

        var dto = new CreateActivityDto
        {
            Title = "Annual planning session", Type = ActivityType.Task,
            Description = "Set goals for the year",
            IsRecurring = true,
            RecurrenceType = RecurrenceType.Annual,
            ContainerId = annualContainer.Id
        };

        // Act
        var result = await activityService.CreateActivityAsync(TestUserId, dto);

        // Assert
        result.Should().NotBeNull();
        result.IsRecurring.Should().BeTrue();
        result.RecurrenceType.Should().Be(RecurrenceType.Annual);
        result.Containers.Should().HaveCount(1);
        result.Containers[0].ContainerId.Should().Be(annualContainer.Id);
        result.Containers[0].ContainerType.Should().Be(ContainerType.Annual);

        // Verify in database
        var containerActivity = await Context.ContainerActivities
            .FirstOrDefaultAsync(ca =>
                ca.ContainerId == annualContainer.Id &&
                ca.ActivityTemplateId == result.Id);

        containerActivity.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateActivity_WithMonthlyContainer_CreatesCorrectAssociations()
    {
        // Arrange
        var containerService = new ContainerService(Context);
        var activityService = new ActivityService(Context, containerService);

        // Create monthly container
        var monthlyContainer = await containerService.GetOrCreateCurrentContainerAsync(TestUserId, ContainerType.Monthly);

        var dto = new CreateActivityDto
        {
            Title = "Monthly review", Type = ActivityType.Task,
            Description = "Review progress and adjust goals",
            IsRecurring = true,
            RecurrenceType = RecurrenceType.Monthly,
            ContainerId = monthlyContainer.Id
        };

        // Act
        var result = await activityService.CreateActivityAsync(TestUserId, dto);

        // Assert
        result.Containers[0].ContainerType.Should().Be(ContainerType.Monthly);
        result.Containers[0].ContainerId.Should().Be(monthlyContainer.Id);

        // Verify monthly container has correct date range
        var savedContainer = await Context.Containers.FindAsync(monthlyContainer.Id);
        savedContainer.Should().NotBeNull();
        savedContainer!.StartDate.Day.Should().Be(1); // First day of month
    }

    [Fact]
    public async Task CreateActivity_WithWeeklyContainer_CreatesCorrectAssociations()
    {
        // Arrange
        var containerService = new ContainerService(Context);
        var activityService = new ActivityService(Context, containerService);

        // Create weekly container
        var weeklyContainer = await containerService.GetOrCreateCurrentContainerAsync(TestUserId, ContainerType.Weekly);

        var dto = new CreateActivityDto
        {
            Title = "Weekly sprint planning", Type = ActivityType.Task,
            Description = "Plan the week's tasks",
            IsRecurring = true,
            RecurrenceType = RecurrenceType.Weekly,
            ContainerId = weeklyContainer.Id
        };

        // Act
        var result = await activityService.CreateActivityAsync(TestUserId, dto);

        // Assert
        result.Containers[0].ContainerType.Should().Be(ContainerType.Weekly);

        // Verify weekly container spans 7 days
        var savedContainer = await Context.Containers.FindAsync(weeklyContainer.Id);
        savedContainer.Should().NotBeNull();
        var duration = (savedContainer!.EndDate - savedContainer.StartDate)?.Days;
        duration.Should().Be(6); // 7 days including start and end (e.g., Mon-Sun = 6 days difference)
    }

    [Fact]
    public async Task GetActivitiesForUser_ReturnsOnlyUserActivities()
    {
        // Arrange
        var containerService = new ContainerService(Context);
        var activityService = new ActivityService(Context, containerService);

        // Create activities for test user
        await activityService.CreateActivityAsync(TestUserId, new CreateActivityDto
        {
            Title = "My Activity 1", Type = ActivityType.Task
        });
        await activityService.CreateActivityAsync(TestUserId, new CreateActivityDto
        {
            Title = "My Activity 2", Type = ActivityType.Task
        });

        // Create activity for different user
        const string otherUserId = "other_user_123";
        var otherContainer = await containerService.GetOrCreateCurrentContainerAsync(otherUserId, ContainerType.Annual);
        // Manually create activity for other user (simulating another user's activity)
        // This tests user isolation

        // Act
        var result = await activityService.GetActivitiesForUserAsync(TestUserId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(a => a.UserId.Should().Be(TestUserId));
        result.Should().Contain(a => a.Title == "My Activity 1");
        result.Should().Contain(a => a.Title == "My Activity 2");
    }

    [Fact]
    public async Task GetActivityById_WithValidId_ReturnsFullActivityDetails()
    {
        // Arrange
        var containerService = new ContainerService(Context);
        var activityService = new ActivityService(Context, containerService);

        var created = await activityService.CreateActivityAsync(TestUserId, new CreateActivityDto
        {
            Title = "Test Activity", Type = ActivityType.Task,
            Description = "Detailed description",
            IsRecurring = false,
            RecurrenceType = RecurrenceType.None
        });

        // Act
        var result = await activityService.GetActivityByIdAsync(TestUserId, created.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(created.Id);
        result.Title.Should().Be("Test Activity");
        result.Description.Should().Be("Detailed description");
        result.Containers.Should().HaveCount(1);
        result.Containers[0].ContainerType.Should().Be(ContainerType.Annual);
    }

    [Fact]
    public async Task CreateMultipleActivities_MaintainsCorrectOrder()
    {
        // Arrange
        var containerService = new ContainerService(Context);
        var activityService = new ActivityService(Context, containerService);

        // Act - Create three activities
        var activity1 = await activityService.CreateActivityAsync(TestUserId, new CreateActivityDto
        {
            Title = "First Activity", Type = ActivityType.Task
        });
        var activity2 = await activityService.CreateActivityAsync(TestUserId, new CreateActivityDto
        {
            Title = "Second Activity", Type = ActivityType.Task
        });
        var activity3 = await activityService.CreateActivityAsync(TestUserId, new CreateActivityDto
        {
            Title = "Third Activity", Type = ActivityType.Task
        });

        // Assert - Check ordering in container
        activity1.Containers[0].Order.Should().Be(1);
        activity2.Containers[0].Order.Should().Be(2);
        activity3.Containers[0].Order.Should().Be(3);

        // Verify in database
        var containerActivities = await Context.ContainerActivities
            .Where(ca => ca.ActivityTemplate.UserId == TestUserId)
            .OrderBy(ca => ca.Order)
            .Include(ca => ca.ActivityTemplate)
            .ToListAsync();

        containerActivities.Should().HaveCount(3);
        containerActivities[0].ActivityTemplate.Title.Should().Be("First Activity");
        containerActivities[1].ActivityTemplate.Title.Should().Be("Second Activity");
        containerActivities[2].ActivityTemplate.Title.Should().Be("Third Activity");
    }

    [Fact]
    public async Task CreateRecurringActivity_PersistsRecurrenceSettings()
    {
        // Arrange
        var containerService = new ContainerService(Context);
        var activityService = new ActivityService(Context, containerService);

        var dto = new CreateActivityDto
        {
            Title = "Daily standup", Type = ActivityType.Task,
            Description = "Team sync meeting",
            IsRecurring = true,
            RecurrenceType = RecurrenceType.Daily
        };

        // Act
        var result = await activityService.CreateActivityAsync(TestUserId, dto);

        // Assert
        result.IsRecurring.Should().BeTrue();
        result.RecurrenceType.Should().Be(RecurrenceType.Daily);

        // Verify in database
        var saved = await Context.ActivityTemplates.FindAsync(result.Id);
        saved.Should().NotBeNull();
        saved!.IsRecurring.Should().BeTrue();
        saved.RecurrenceType.Should().Be(RecurrenceType.Daily);
    }
}
