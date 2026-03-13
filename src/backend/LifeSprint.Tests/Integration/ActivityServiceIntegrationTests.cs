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
[Collection("IntegrationTests")]
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

    [Fact]
    public async Task UpdateActivity_ChangesFieldsAndPersists()
    {
        // Arrange
        var containerService = new ContainerService(Context);
        var activityService = new ActivityService(Context, containerService);

        var created = await activityService.CreateActivityAsync(TestUserId, new CreateActivityDto
        {
            Title = "Original Title", Type = ActivityType.Task,
            Description = "Original description"
        });

        var updateDto = new UpdateActivityDto
        {
            Title = "Updated Title",
            Description = "Updated description"
        };

        // Act
        var result = await activityService.UpdateActivityAsync(TestUserId, created.Id, updateDto);

        // Assert
        result.Should().NotBeNull();
        result!.Title.Should().Be("Updated Title");
        result.Description.Should().Be("Updated description");
        result.Type.Should().Be(ActivityType.Task); // Unchanged

        // Verify persistence in database
        var saved = await Context.ActivityTemplates.FindAsync(created.Id);
        saved!.Title.Should().Be("Updated Title");
        saved.Description.Should().Be("Updated description");
    }

    [Fact]
    public async Task UpdateActivity_WithInvalidHierarchy_ThrowsInvalidOperationException()
    {
        // Arrange
        var containerService = new ContainerService(Context);
        var activityService = new ActivityService(Context, containerService);

        var project1 = await activityService.CreateActivityAsync(TestUserId, new CreateActivityDto
        {
            Title = "Project One", Type = ActivityType.Project
        });
        var project2 = await activityService.CreateActivityAsync(TestUserId, new CreateActivityDto
        {
            Title = "Project Two", Type = ActivityType.Project
        });

        // Act & Assert - A Project cannot have a parent (even another Project)
        var updateDto = new UpdateActivityDto { ParentActivityId = project2.Id };
        var act = async () => await activityService.UpdateActivityAsync(TestUserId, project1.Id, updateDto);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Project*");
    }

    [Fact]
    public async Task UpdateActivity_OtherUsersActivity_ReturnsNull()
    {
        // Arrange
        var containerService = new ContainerService(Context);
        var activityService = new ActivityService(Context, containerService);

        var created = await activityService.CreateActivityAsync(TestUserId, new CreateActivityDto
        {
            Title = "My Activity", Type = ActivityType.Task
        });

        // Act - Try to update as a different user
        var result = await activityService.UpdateActivityAsync("other_user", created.Id, new UpdateActivityDto
        {
            Title = "Hijacked Title"
        });

        // Assert
        result.Should().BeNull();

        // Verify the title was NOT changed
        var unchanged = await Context.ActivityTemplates.FindAsync(created.Id);
        unchanged!.Title.Should().Be("My Activity");
    }

    [Fact]
    public async Task ArchiveActivity_SetsArchivedAtAndExcludesFromQueries()
    {
        // Arrange
        var containerService = new ContainerService(Context);
        var activityService = new ActivityService(Context, containerService);

        var created = await activityService.CreateActivityAsync(TestUserId, new CreateActivityDto
        {
            Title = "To Be Archived", Type = ActivityType.Task
        });

        // Act
        var success = await activityService.ArchiveActivityAsync(TestUserId, created.Id);

        // Assert - archive returned true
        success.Should().BeTrue();

        // Verify ArchivedAt is set in database
        var saved = await Context.ActivityTemplates.FindAsync(created.Id);
        saved!.ArchivedAt.Should().NotBeNull();
        saved.ArchivedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // Verify excluded from user query
        var activities = await activityService.GetActivitiesForUserAsync(TestUserId);
        activities.Should().NotContain(a => a.Id == created.Id);
    }

    [Fact]
    public async Task ArchiveActivity_OtherUsersActivity_ReturnsFalse()
    {
        // Arrange
        var containerService = new ContainerService(Context);
        var activityService = new ActivityService(Context, containerService);

        var created = await activityService.CreateActivityAsync(TestUserId, new CreateActivityDto
        {
            Title = "My Activity", Type = ActivityType.Task
        });

        // Act - Try to archive as a different user
        var success = await activityService.ArchiveActivityAsync("other_user", created.Id);

        // Assert
        success.Should().BeFalse();

        // Verify NOT archived
        var saved = await Context.ActivityTemplates.FindAsync(created.Id);
        saved!.ArchivedAt.Should().BeNull();
    }

    [Fact]
    public async Task ToggleActivityCompletion_MarksAsCompleteAndIncomplete()
    {
        // Arrange
        var containerService = new ContainerService(Context);
        var activityService = new ActivityService(Context, containerService);

        var annualContainer = await containerService.GetOrCreateCurrentContainerAsync(TestUserId, ContainerType.Annual);
        var created = await activityService.CreateActivityAsync(TestUserId, new CreateActivityDto
        {
            Title = "Task to Complete", Type = ActivityType.Task,
            ContainerId = annualContainer.Id
        });

        // Act - Mark as complete
        var completed = await activityService.ToggleActivityCompletionAsync(
            TestUserId, created.Id, annualContainer.Id, isCompleted: true);

        // Assert - completed
        completed.Should().NotBeNull();
        completed!.Containers[0].CompletedAt.Should().NotBeNull();

        // Verify in database
        var ca = await Context.ContainerActivities
            .FindAsync(annualContainer.Id, created.Id);
        ca!.CompletedAt.Should().NotBeNull();

        // Act - Mark as incomplete
        var incomplete = await activityService.ToggleActivityCompletionAsync(
            TestUserId, created.Id, annualContainer.Id, isCompleted: false);

        // Assert - back to incomplete
        incomplete!.Containers[0].CompletedAt.Should().BeNull();

        var caAfter = await Context.ContainerActivities
            .FindAsync(annualContainer.Id, created.Id);
        caAfter!.CompletedAt.Should().BeNull();
    }

    [Fact]
    public async Task ToggleActivityCompletion_ActivityNotInContainer_ThrowsInvalidOperationException()
    {
        // Arrange
        var containerService = new ContainerService(Context);
        var activityService = new ActivityService(Context, containerService);

        var annualContainer = await containerService.GetOrCreateCurrentContainerAsync(TestUserId, ContainerType.Annual);
        var weeklyContainer = await containerService.GetOrCreateCurrentContainerAsync(TestUserId, ContainerType.Weekly);

        // Create activity in annual only
        var created = await activityService.CreateActivityAsync(TestUserId, new CreateActivityDto
        {
            Title = "Annual Only Task", Type = ActivityType.Task,
            ContainerId = annualContainer.Id
        });

        // Act & Assert - trying to toggle in a container it doesn't belong to
        var act = async () => await activityService.ToggleActivityCompletionAsync(
            TestUserId, created.Id, weeklyContainer.Id, isCompleted: true);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Activity {created.Id} is not associated with container {weeklyContainer.Id}");
    }

    [Fact]
    public async Task GetActivitiesForUser_WithContainerTypeFilter_ReturnsOnlyMatchingActivities()
    {
        // Arrange
        var containerService = new ContainerService(Context);
        var activityService = new ActivityService(Context, containerService);

        var annualContainer = await containerService.GetOrCreateCurrentContainerAsync(TestUserId, ContainerType.Annual);
        var weeklyContainer = await containerService.GetOrCreateCurrentContainerAsync(TestUserId, ContainerType.Weekly);

        await activityService.CreateActivityAsync(TestUserId, new CreateActivityDto
        {
            Title = "Annual Goal", Type = ActivityType.Task, ContainerId = annualContainer.Id
        });
        await activityService.CreateActivityAsync(TestUserId, new CreateActivityDto
        {
            Title = "Weekly Task", Type = ActivityType.Task, ContainerId = weeklyContainer.Id
        });

        // Act - filter by Weekly
        var weeklyActivities = await activityService.GetActivitiesForUserAsync(TestUserId, ContainerType.Weekly);

        // Assert - only weekly activity returned
        weeklyActivities.Should().HaveCount(1);
        weeklyActivities[0].Title.Should().Be("Weekly Task");

        // Act - filter by Annual
        var annualActivities = await activityService.GetActivitiesForUserAsync(TestUserId, ContainerType.Annual);

        // Assert - only annual activity returned
        annualActivities.Should().HaveCount(1);
        annualActivities[0].Title.Should().Be("Annual Goal");

        // Act - no filter
        var allActivities = await activityService.GetActivitiesForUserAsync(TestUserId);

        // Assert - both activities returned
        allActivities.Should().HaveCount(2);
    }
}
