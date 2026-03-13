using Xunit;
using FluentAssertions;
using LifeSprint.Api.Controllers;
using LifeSprint.Infrastructure.Services;
using LifeSprint.Core.DTOs;
using LifeSprint.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace LifeSprint.Tests.Integration;

/// <summary>
/// Integration tests for ActivitiesController using a real PostgreSQL database.
/// Tests the full request/response flow through the controller.
/// </summary>
/// <remarks>
/// Related files:
/// - Controller: LifeSprint.Api/Controllers/ActivitiesController.cs
/// - Base: Integration/IntegrationTestBase.cs
/// </remarks>
[Collection("IntegrationTests")]
public class ActivitiesControllerIntegrationTests : IntegrationTestBase
{
    private ActivitiesController CreateController()
    {
        var containerService = new ContainerService(Context);
        var activityService = new ActivityService(Context, containerService);
        var controller = new ActivitiesController(activityService);

        // Setup authenticated user context
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, TestUserId)
        }, "TestAuth"));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        return controller;
    }

    [Fact]
    public async Task POST_CreateActivity_ReturnsCreatedWithActivity()
    {
        // Arrange
        var controller = CreateController();
        var dto = new CreateActivityDto
        {
            Title = "Learn integration testing", Type = ActivityType.Task,
            Description = "Master the art of testing with real databases",
            IsRecurring = false,
            RecurrenceType = RecurrenceType.None
        };

        // Act
        var result = await controller.CreateActivity(dto);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(201);
        createdResult.ActionName.Should().Be(nameof(ActivitiesController.GetActivity));

        var activity = createdResult.Value.Should().BeOfType<ActivityResponseDto>().Subject;
        activity.Id.Should().BeGreaterThan(0);
        activity.Title.Should().Be("Learn integration testing");
        activity.Description.Should().Be("Master the art of testing with real databases");
        activity.UserId.Should().Be(TestUserId);
        activity.Containers.Should().HaveCount(1);
        activity.Containers[0].ContainerType.Should().Be(ContainerType.Annual);
    }

    [Fact]
    public async Task POST_CreateRecurringActivity_StoresRecurrenceSettings()
    {
        // Arrange
        var controller = CreateController();
        var dto = new CreateActivityDto
        {
            Title = "Weekly retrospective", Type = ActivityType.Task,
            Description = "Team reflection and improvement planning",
            IsRecurring = true,
            RecurrenceType = RecurrenceType.Weekly
        };

        // Act
        var result = await controller.CreateActivity(dto);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var activity = createdResult.Value.Should().BeOfType<ActivityResponseDto>().Subject;

        activity.IsRecurring.Should().BeTrue();
        activity.RecurrenceType.Should().Be(RecurrenceType.Weekly);
    }

    [Fact]
    public async Task POST_CreateActivity_WithSpecificContainer_AddsToThatContainer()
    {
        // Arrange
        var controller = CreateController();

        // First create a monthly container
        var containerService = new ContainerService(Context);
        var monthlyContainer = await containerService.GetOrCreateCurrentContainerAsync(TestUserId, ContainerType.Monthly);

        var dto = new CreateActivityDto
        {
            Title = "Monthly budget review", Type = ActivityType.Task,
            ContainerId = monthlyContainer.Id
        };

        // Act
        var result = await controller.CreateActivity(dto);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var activity = createdResult.Value.Should().BeOfType<ActivityResponseDto>().Subject;

        activity.Containers.Should().HaveCount(1);
        activity.Containers[0].ContainerId.Should().Be(monthlyContainer.Id);
        activity.Containers[0].ContainerType.Should().Be(ContainerType.Monthly);
    }

    [Fact]
    public async Task GET_GetActivities_ReturnsAllUserActivities()
    {
        // Arrange
        var controller = CreateController();

        // Create some activities first
        await controller.CreateActivity(new CreateActivityDto { Title = "Activity 1", Type = ActivityType.Task });
        await controller.CreateActivity(new CreateActivityDto { Title = "Activity 2", Type = ActivityType.Task });
        await controller.CreateActivity(new CreateActivityDto { Title = "Activity 3", Type = ActivityType.Task });

        // Act
        var result = await controller.GetActivities();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var activities = okResult.Value.Should().BeAssignableTo<List<ActivityResponseDto>>().Subject;

        activities.Should().HaveCount(3);
        activities.Should().Contain(a => a.Title == "Activity 1");
        activities.Should().Contain(a => a.Title == "Activity 2");
        activities.Should().Contain(a => a.Title == "Activity 3");
        activities.Should().AllSatisfy(a => a.UserId.Should().Be(TestUserId));
    }

    [Fact]
    public async Task GET_GetActivity_WithValidId_ReturnsActivity()
    {
        // Arrange
        var controller = CreateController();

        // Create an activity
        var createResult = await controller.CreateActivity(new CreateActivityDto
        {
            Title = "Find me", Type = ActivityType.Task,
            Description = "I should be retrievable"
        });

        var createdResult = createResult.Should().BeOfType<CreatedAtActionResult>().Subject;
        var createdActivity = createdResult.Value.Should().BeOfType<ActivityResponseDto>().Subject;

        // Act
        var result = await controller.GetActivity(createdActivity.Id);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var activity = okResult.Value.Should().BeOfType<ActivityResponseDto>().Subject;

        activity.Id.Should().Be(createdActivity.Id);
        activity.Title.Should().Be("Find me");
        activity.Description.Should().Be("I should be retrievable");
    }

    [Fact]
    public async Task GET_GetActivity_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var controller = CreateController();

        // Act
        var result = await controller.GetActivity(99999);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task FullWorkflow_CreateMultipleActivitiesAndRetrieve()
    {
        // Arrange
        var controller = CreateController();

        // Act - Create multiple activities
        var activity1 = await controller.CreateActivity(new CreateActivityDto
        {
            Title = "Design database schema", Type = ActivityType.Task,
            Description = "Plan the data model",
            IsRecurring = false
        });

        var activity2 = await controller.CreateActivity(new CreateActivityDto
        {
            Title = "Implement API endpoints", Type = ActivityType.Task,
            Description = "Build RESTful services",
            IsRecurring = false
        });

        var activity3 = await controller.CreateActivity(new CreateActivityDto
        {
            Title = "Write tests", Type = ActivityType.Task,
            Description = "Ensure code quality",
            IsRecurring = false
        });

        // Act - Retrieve all activities
        var getAllResult = await controller.GetActivities();

        // Assert - Verify all activities are returned
        var okResult = getAllResult.Should().BeOfType<OkObjectResult>().Subject;
        var activities = okResult.Value.Should().BeAssignableTo<List<ActivityResponseDto>>().Subject;

        activities.Should().HaveCount(3);

        // Extract created activity IDs
        var id1 = ((activity1 as CreatedAtActionResult)?.Value as ActivityResponseDto)?.Id ?? 0;
        var id2 = ((activity2 as CreatedAtActionResult)?.Value as ActivityResponseDto)?.Id ?? 0;
        var id3 = ((activity3 as CreatedAtActionResult)?.Value as ActivityResponseDto)?.Id ?? 0;

        // Act - Retrieve each individual activity
        var get1 = await controller.GetActivity(id1);
        var get2 = await controller.GetActivity(id2);
        var get3 = await controller.GetActivity(id3);

        // Assert - Verify each activity can be retrieved individually
        get1.Should().BeOfType<OkObjectResult>();
        get2.Should().BeOfType<OkObjectResult>();
        get3.Should().BeOfType<OkObjectResult>();

        var retrieved1 = (get1 as OkObjectResult)?.Value as ActivityResponseDto;
        var retrieved2 = (get2 as OkObjectResult)?.Value as ActivityResponseDto;
        var retrieved3 = (get3 as OkObjectResult)?.Value as ActivityResponseDto;

        retrieved1?.Title.Should().Be("Design database schema");
        retrieved2?.Title.Should().Be("Implement API endpoints");
        retrieved3?.Title.Should().Be("Write tests");
    }

    [Fact]
    public async Task CreateActivities_InDifferentContainers_MaintainsCorrectAssociations()
    {
        // Arrange
        var controller = CreateController();
        var containerService = new ContainerService(Context);

        // Create containers for different time periods
        var annualContainer = await containerService.GetOrCreateCurrentContainerAsync(TestUserId, ContainerType.Annual);
        var monthlyContainer = await containerService.GetOrCreateCurrentContainerAsync(TestUserId, ContainerType.Monthly);
        var weeklyContainer = await containerService.GetOrCreateCurrentContainerAsync(TestUserId, ContainerType.Weekly);

        // Act - Create activities in different containers
        await controller.CreateActivity(new CreateActivityDto
        {
            Title = "Annual goal", Type = ActivityType.Task,
            ContainerId = annualContainer.Id
        });

        await controller.CreateActivity(new CreateActivityDto
        {
            Title = "Monthly objective", Type = ActivityType.Task,
            ContainerId = monthlyContainer.Id
        });

        await controller.CreateActivity(new CreateActivityDto
        {
            Title = "Weekly task", Type = ActivityType.Task,
            ContainerId = weeklyContainer.Id
        });

        // Act - Retrieve all activities
        var result = await controller.GetActivities();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var activities = okResult.Value.Should().BeAssignableTo<List<ActivityResponseDto>>().Subject;

        activities.Should().HaveCount(3);

        var annualActivity = activities.First(a => a.Title == "Annual goal");
        var monthlyActivity = activities.First(a => a.Title == "Monthly objective");
        var weeklyActivity = activities.First(a => a.Title == "Weekly task");

        annualActivity.Containers[0].ContainerType.Should().Be(ContainerType.Annual);
        monthlyActivity.Containers[0].ContainerType.Should().Be(ContainerType.Monthly);
        weeklyActivity.Containers[0].ContainerType.Should().Be(ContainerType.Weekly);
    }

    [Fact]
    public async Task GET_GetActivities_WithContainerTypeFilter_ReturnsOnlyMatchingActivities()
    {
        // Arrange
        var controller = CreateController();
        var containerService = new ContainerService(Context);

        var annualContainer = await containerService.GetOrCreateCurrentContainerAsync(TestUserId, ContainerType.Annual);
        var weeklyContainer = await containerService.GetOrCreateCurrentContainerAsync(TestUserId, ContainerType.Weekly);

        await controller.CreateActivity(new CreateActivityDto
        {
            Title = "Annual Goal", Type = ActivityType.Task, ContainerId = annualContainer.Id
        });
        await controller.CreateActivity(new CreateActivityDto
        {
            Title = "Weekly Task", Type = ActivityType.Task, ContainerId = weeklyContainer.Id
        });

        // Act - filter by Weekly
        var result = await controller.GetActivities(ContainerType.Weekly);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var activities = okResult.Value.Should().BeAssignableTo<List<ActivityResponseDto>>().Subject;

        activities.Should().HaveCount(1);
        activities[0].Title.Should().Be("Weekly Task");
        activities[0].Containers[0].ContainerType.Should().Be(ContainerType.Weekly);
    }

    [Fact]
    public async Task PUT_UpdateActivity_UpdatesTitleAndDescription()
    {
        // Arrange
        var controller = CreateController();

        var createResult = await controller.CreateActivity(new CreateActivityDto
        {
            Title = "Original Title", Type = ActivityType.Task,
            Description = "Original description"
        });
        var created = ((createResult as CreatedAtActionResult)!.Value as ActivityResponseDto)!;

        var updateDto = new UpdateActivityDto
        {
            Title = "Updated Title",
            Description = "Updated description"
        };

        // Act
        var result = await controller.UpdateActivity(created.Id, updateDto);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var updated = okResult.Value.Should().BeOfType<ActivityResponseDto>().Subject;

        updated.Title.Should().Be("Updated Title");
        updated.Description.Should().Be("Updated description");
        updated.Type.Should().Be(ActivityType.Task); // Unchanged
        updated.Containers.Should().HaveCount(1);    // Container association preserved
    }

    [Fact]
    public async Task PUT_UpdateActivity_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var controller = CreateController();
        var updateDto = new UpdateActivityDto { Title = "New Title" };

        // Act
        var result = await controller.UpdateActivity(99999, updateDto);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task PUT_UpdateActivity_WithInvalidHierarchy_ReturnsBadRequest()
    {
        // Arrange
        var controller = CreateController();

        var projectResult = await controller.CreateActivity(new CreateActivityDto
        {
            Title = "My Project", Type = ActivityType.Project
        });
        var project = ((projectResult as CreatedAtActionResult)!.Value as ActivityResponseDto)!;

        // Act - Project cannot have a parent
        var result = await controller.UpdateActivity(project.Id, new UpdateActivityDto
        {
            ParentActivityId = project.Id // Self-reference as parent (also invalid)
        });

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task PATCH_ToggleCompletion_MarksActivityAsComplete()
    {
        // Arrange
        var controller = CreateController();
        var containerService = new ContainerService(Context);
        var annualContainer = await containerService.GetOrCreateCurrentContainerAsync(TestUserId, ContainerType.Annual);

        var createResult = await controller.CreateActivity(new CreateActivityDto
        {
            Title = "Task to Complete", Type = ActivityType.Task,
            ContainerId = annualContainer.Id
        });
        var created = ((createResult as CreatedAtActionResult)!.Value as ActivityResponseDto)!;

        // Act - Mark as complete
        var result = await controller.ToggleCompletion(created.Id, new ToggleCompletionDto
        {
            ContainerId = annualContainer.Id,
            IsCompleted = true
        });

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var activity = okResult.Value.Should().BeOfType<ActivityResponseDto>().Subject;
        activity.Containers[0].CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task PATCH_ToggleCompletion_MarksActivityAsIncomplete()
    {
        // Arrange
        var controller = CreateController();
        var containerService = new ContainerService(Context);
        var annualContainer = await containerService.GetOrCreateCurrentContainerAsync(TestUserId, ContainerType.Annual);

        var createResult = await controller.CreateActivity(new CreateActivityDto
        {
            Title = "Task to Uncomplete", Type = ActivityType.Task,
            ContainerId = annualContainer.Id
        });
        var created = ((createResult as CreatedAtActionResult)!.Value as ActivityResponseDto)!;

        // Mark as complete first
        await controller.ToggleCompletion(created.Id, new ToggleCompletionDto
        {
            ContainerId = annualContainer.Id, IsCompleted = true
        });

        // Act - Mark as incomplete
        var result = await controller.ToggleCompletion(created.Id, new ToggleCompletionDto
        {
            ContainerId = annualContainer.Id,
            IsCompleted = false
        });

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var activity = okResult.Value.Should().BeOfType<ActivityResponseDto>().Subject;
        activity.Containers[0].CompletedAt.Should().BeNull();
    }

    [Fact]
    public async Task PATCH_ToggleCompletion_ActivityNotInContainer_ReturnsBadRequest()
    {
        // Arrange
        var controller = CreateController();
        var containerService = new ContainerService(Context);
        var annualContainer = await containerService.GetOrCreateCurrentContainerAsync(TestUserId, ContainerType.Annual);
        var weeklyContainer = await containerService.GetOrCreateCurrentContainerAsync(TestUserId, ContainerType.Weekly);

        var createResult = await controller.CreateActivity(new CreateActivityDto
        {
            Title = "Annual Only", Type = ActivityType.Task,
            ContainerId = annualContainer.Id
        });
        var created = ((createResult as CreatedAtActionResult)!.Value as ActivityResponseDto)!;

        // Act - Try to toggle in a container the activity doesn't belong to
        var result = await controller.ToggleCompletion(created.Id, new ToggleCompletionDto
        {
            ContainerId = weeklyContainer.Id,
            IsCompleted = true
        });

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task DELETE_ArchiveActivity_ReturnsNoContentAndExcludesFromList()
    {
        // Arrange
        var controller = CreateController();

        var createResult = await controller.CreateActivity(new CreateActivityDto
        {
            Title = "To Be Deleted", Type = ActivityType.Task
        });
        var created = ((createResult as CreatedAtActionResult)!.Value as ActivityResponseDto)!;

        // Act
        var deleteResult = await controller.DeleteActivity(created.Id);

        // Assert - returns 204
        deleteResult.Should().BeOfType<NoContentResult>();

        // Assert - activity no longer appears in list
        var listResult = await controller.GetActivities();
        var activities = ((listResult as OkObjectResult)!.Value as List<ActivityResponseDto>)!;
        activities.Should().NotContain(a => a.Id == created.Id);

        // Assert - individual lookup also returns 404
        var getResult = await controller.GetActivity(created.Id);
        getResult.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task DELETE_ArchiveActivity_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var controller = CreateController();

        // Act
        var result = await controller.DeleteActivity(99999);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
