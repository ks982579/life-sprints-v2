using Xunit;
using Moq;
using FluentAssertions;
using LifeSprint.Api.Controllers;
using LifeSprint.Core.DTOs;
using LifeSprint.Core.Interfaces;
using LifeSprint.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace LifeSprint.Tests.Unit;

/// <summary>
/// Unit tests for ActivitiesController.
/// Tests API endpoints for creating and retrieving activities.
/// </summary>
/// <remarks>
/// Related files:
/// - Controller: LifeSprint.Api/Controllers/ActivitiesController.cs
/// - Service: LifeSprint.Infrastructure/Services/ActivityService.cs
/// - DTOs: LifeSprint.Core/DTOs/CreateActivityDto.cs, ActivityResponseDto.cs
/// </remarks>
[Trait("Category", "Unit")]
public class ActivitiesControllerTests
{
    private readonly Mock<IActivityService> _mockActivityService;
    private readonly ActivitiesController _controller;
    private const string TestUserId = "test_user_123";

    public ActivitiesControllerTests()
    {
        _mockActivityService = new Mock<IActivityService>();
        _controller = new ActivitiesController(_mockActivityService.Object);

        // Setup authenticated user context
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, TestUserId)
        }, "mock"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    #region POST /api/activities Tests

    [Fact]
    public async Task CreateActivity_WithValidDto_ReturnsCreatedResult()
    {
        // Arrange
        var createDto = new CreateActivityDto
        {
            Title = "Learn ASP.NET Core", Type = ActivityType.Task,
            Description = "Build REST APIs",
            IsRecurring = false,
            RecurrenceType = RecurrenceType.None
        };

        var expectedResponse = new ActivityResponseDto
        {
            Id = 1,
            UserId = TestUserId,
            Title = "Learn ASP.NET Core", Type = ActivityType.Task,
            Description = "Build REST APIs",
            IsRecurring = false,
            RecurrenceType = RecurrenceType.None,
            CreatedAt = DateTime.UtcNow,
            ArchivedAt = null,
            Containers = new List<ContainerAssociationDto>
            {
                new()
                {
                    ContainerId = 1,
                    ContainerType = ContainerType.Annual,
                    AddedAt = DateTime.UtcNow,
                    CompletedAt = null,
                    Order = 1,
                    IsRolledOver = false
                }
            }
        };

        _mockActivityService
            .Setup(x => x.CreateActivityAsync(TestUserId, createDto))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.CreateActivity(createDto);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(201);
        createdResult.ActionName.Should().Be(nameof(ActivitiesController.GetActivity));
        createdResult.RouteValues.Should().ContainKey("id");
        createdResult.RouteValues!["id"].Should().Be(1);

        var returnedActivity = createdResult.Value.Should().BeOfType<ActivityResponseDto>().Subject;
        returnedActivity.Id.Should().Be(1);
        returnedActivity.Title.Should().Be("Learn ASP.NET Core");
        returnedActivity.UserId.Should().Be(TestUserId);

        _mockActivityService.Verify(
            x => x.CreateActivityAsync(TestUserId, createDto),
            Times.Once);
    }

    [Fact]
    public async Task CreateActivity_WithInvalidModel_ReturnsBadRequest()
    {
        // Arrange
        var createDto = new CreateActivityDto
        {
            Title = "", Type = ActivityType.Task, // Invalid - empty title
            Description = "Test"
        };

        _controller.ModelState.AddModelError("Title", "Title is required");

        // Act
        var result = await _controller.CreateActivity(createDto);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();

        _mockActivityService.Verify(
            x => x.CreateActivityAsync(It.IsAny<string>(), It.IsAny<CreateActivityDto>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateActivity_ServiceThrowsUnauthorizedException_ReturnsUnauthorized()
    {
        // Arrange
        var createDto = new CreateActivityDto
        {
            Title = "Test Activity", Type = ActivityType.Task,
            ContainerId = 999 // Invalid container
        };

        _mockActivityService
            .Setup(x => x.CreateActivityAsync(TestUserId, createDto))
            .ThrowsAsync(new UnauthorizedAccessException("Container not found"));

        // Act
        var result = await _controller.CreateActivity(createDto);

        // Assert
        var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        unauthorizedResult.StatusCode.Should().Be(401);
        unauthorizedResult.Value.Should().BeEquivalentTo(new { message = "Container not found" });
    }

    [Fact]
    public async Task CreateActivity_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var createDto = new CreateActivityDto
        {
            Title = "Test Activity", Type = ActivityType.Task
        };

        _mockActivityService
            .Setup(x => x.CreateActivityAsync(TestUserId, createDto))
            .ThrowsAsync(new Exception("Database connection failed"));

        // Act
        var result = await _controller.CreateActivity(createDto);

        // Assert
        var errorResult = result.Should().BeOfType<ObjectResult>().Subject;
        errorResult.StatusCode.Should().Be(500);
        errorResult.Value.Should().BeEquivalentTo(new { message = "An error occurred while creating the activity" });
    }

    [Fact]
    public async Task CreateActivity_RecurringActivity_CreatesSuccessfully()
    {
        // Arrange
        var createDto = new CreateActivityDto
        {
            Title = "Weekly Review", Type = ActivityType.Task,
            Description = "Review progress and plan ahead",
            IsRecurring = true,
            RecurrenceType = RecurrenceType.Weekly
        };

        var expectedResponse = new ActivityResponseDto
        {
            Id = 2,
            UserId = TestUserId,
            Title = "Weekly Review", Type = ActivityType.Task,
            Description = "Review progress and plan ahead",
            IsRecurring = true,
            RecurrenceType = RecurrenceType.Weekly,
            CreatedAt = DateTime.UtcNow,
            Containers = new List<ContainerAssociationDto>()
        };

        _mockActivityService
            .Setup(x => x.CreateActivityAsync(TestUserId, createDto))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.CreateActivity(createDto);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var returnedActivity = createdResult.Value.Should().BeOfType<ActivityResponseDto>().Subject;
        returnedActivity.IsRecurring.Should().BeTrue();
        returnedActivity.RecurrenceType.Should().Be(RecurrenceType.Weekly);
    }

    #endregion

    #region GET /api/activities Tests

    [Fact]
    public async Task GetActivities_ReturnsAllUserActivities()
    {
        // Arrange
        var expectedActivities = new List<ActivityResponseDto>
        {
            new()
            {
                Id = 1,
                UserId = TestUserId,
                Title = "Activity 1", Type = ActivityType.Task,
                IsRecurring = false,
                RecurrenceType = RecurrenceType.None,
                CreatedAt = DateTime.UtcNow,
                Containers = new List<ContainerAssociationDto>()
            },
            new()
            {
                Id = 2,
                UserId = TestUserId,
                Title = "Activity 2", Type = ActivityType.Task,
                IsRecurring = true,
                RecurrenceType = RecurrenceType.Weekly,
                CreatedAt = DateTime.UtcNow,
                Containers = new List<ContainerAssociationDto>()
            }
        };

        _mockActivityService
            .Setup(x => x.GetActivitiesForUserAsync(TestUserId, It.IsAny<ContainerType?>()))
            .ReturnsAsync(expectedActivities);

        // Act
        var result = await _controller.GetActivities();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var activities = okResult.Value.Should().BeAssignableTo<List<ActivityResponseDto>>().Subject;
        activities.Should().HaveCount(2);
        activities[0].Title.Should().Be("Activity 1");
        activities[1].Title.Should().Be("Activity 2");

        _mockActivityService.Verify(
            x => x.GetActivitiesForUserAsync(TestUserId, null),
            Times.Once);
    }

    [Fact]
    public async Task GetActivities_WithContainerTypeFilter_PassesFilterToService()
    {
        // Arrange
        var weeklyActivities = new List<ActivityResponseDto>
        {
            new()
            {
                Id = 1, UserId = TestUserId, Title = "Weekly Task", Type = ActivityType.Task,
                IsRecurring = false, RecurrenceType = RecurrenceType.None,
                CreatedAt = DateTime.UtcNow,
                Containers = new List<ContainerAssociationDto>
                {
                    new() { ContainerId = 10, ContainerType = ContainerType.Weekly,
                            AddedAt = DateTime.UtcNow, Order = 1, IsRolledOver = false }
                }
            }
        };

        _mockActivityService
            .Setup(x => x.GetActivitiesForUserAsync(TestUserId, ContainerType.Weekly))
            .ReturnsAsync(weeklyActivities);

        // Act
        var result = await _controller.GetActivities(ContainerType.Weekly);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var activities = okResult.Value.Should().BeAssignableTo<List<ActivityResponseDto>>().Subject;
        activities.Should().HaveCount(1);
        activities[0].Title.Should().Be("Weekly Task");

        _mockActivityService.Verify(
            x => x.GetActivitiesForUserAsync(TestUserId, ContainerType.Weekly),
            Times.Once);
    }

    [Fact]
    public async Task GetActivities_WithNoActivities_ReturnsEmptyList()
    {
        // Arrange
        _mockActivityService
            .Setup(x => x.GetActivitiesForUserAsync(TestUserId, It.IsAny<ContainerType?>()))
            .ReturnsAsync(new List<ActivityResponseDto>());

        // Act
        var result = await _controller.GetActivities();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var activities = okResult.Value.Should().BeAssignableTo<List<ActivityResponseDto>>().Subject;
        activities.Should().BeEmpty();
    }

    [Fact]
    public async Task GetActivities_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        _mockActivityService
            .Setup(x => x.GetActivitiesForUserAsync(TestUserId, It.IsAny<ContainerType?>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetActivities();

        // Assert
        var errorResult = result.Should().BeOfType<ObjectResult>().Subject;
        errorResult.StatusCode.Should().Be(500);
        errorResult.Value.Should().BeEquivalentTo(new { message = "An error occurred while retrieving activities" });
    }

    #endregion

    #region GET /api/activities/{id} Tests

    [Fact]
    public async Task GetActivity_WithValidId_ReturnsActivity()
    {
        // Arrange
        var expectedActivity = new ActivityResponseDto
        {
            Id = 1,
            UserId = TestUserId,
            Title = "Test Activity", Type = ActivityType.Task,
            Description = "Test Description",
            IsRecurring = false,
            RecurrenceType = RecurrenceType.None,
            CreatedAt = DateTime.UtcNow,
            Containers = new List<ContainerAssociationDto>
            {
                new()
                {
                    ContainerId = 1,
                    ContainerType = ContainerType.Monthly,
                    AddedAt = DateTime.UtcNow,
                    Order = 1,
                    IsRolledOver = false
                }
            }
        };

        _mockActivityService
            .Setup(x => x.GetActivityByIdAsync(TestUserId, 1))
            .ReturnsAsync(expectedActivity);

        // Act
        var result = await _controller.GetActivity(1);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var activity = okResult.Value.Should().BeOfType<ActivityResponseDto>().Subject;
        activity.Id.Should().Be(1);
        activity.Title.Should().Be("Test Activity");
        activity.Containers.Should().HaveCount(1);

        _mockActivityService.Verify(
            x => x.GetActivityByIdAsync(TestUserId, 1),
            Times.Once);
    }

    [Fact]
    public async Task GetActivity_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        _mockActivityService
            .Setup(x => x.GetActivityByIdAsync(TestUserId, 999))
            .ReturnsAsync((ActivityResponseDto?)null);

        // Act
        var result = await _controller.GetActivity(999);

        // Assert
        result.Should().BeOfType<NotFoundResult>();

        _mockActivityService.Verify(
            x => x.GetActivityByIdAsync(TestUserId, 999),
            Times.Once);
    }

    [Fact]
    public async Task GetActivity_OtherUsersActivity_ReturnsNotFound()
    {
        // Arrange - Service returns null for unauthorized access
        _mockActivityService
            .Setup(x => x.GetActivityByIdAsync(TestUserId, 5))
            .ReturnsAsync((ActivityResponseDto?)null);

        // Act
        var result = await _controller.GetActivity(5);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetActivity_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        _mockActivityService
            .Setup(x => x.GetActivityByIdAsync(TestUserId, 1))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetActivity(1);

        // Assert
        var errorResult = result.Should().BeOfType<ObjectResult>().Subject;
        errorResult.StatusCode.Should().Be(500);
        errorResult.Value.Should().BeEquivalentTo(new { message = "An error occurred while retrieving the activity" });
    }

    #endregion

    #region PUT /api/activities/{id} Tests

    [Fact]
    public async Task UpdateActivity_WithValidDto_ReturnsUpdatedActivity()
    {
        // Arrange
        var updateDto = new UpdateActivityDto { Title = "Updated Title" };
        var expectedResponse = new ActivityResponseDto
        {
            Id = 1, UserId = TestUserId, Title = "Updated Title", Type = ActivityType.Task,
            IsRecurring = false, RecurrenceType = RecurrenceType.None,
            CreatedAt = DateTime.UtcNow, Containers = new List<ContainerAssociationDto>()
        };

        _mockActivityService
            .Setup(x => x.UpdateActivityAsync(TestUserId, 1, updateDto))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.UpdateActivity(1, updateDto);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var activity = okResult.Value.Should().BeOfType<ActivityResponseDto>().Subject;
        activity.Title.Should().Be("Updated Title");

        _mockActivityService.Verify(x => x.UpdateActivityAsync(TestUserId, 1, updateDto), Times.Once);
    }

    [Fact]
    public async Task UpdateActivity_WithInvalidModel_ReturnsBadRequest()
    {
        // Arrange
        var updateDto = new UpdateActivityDto { Title = new string('x', 501) }; // Exceeds 500-char limit
        _controller.ModelState.AddModelError("Title", "Title cannot exceed 500 characters");

        // Act
        var result = await _controller.UpdateActivity(1, updateDto);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        _mockActivityService.Verify(
            x => x.UpdateActivityAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<UpdateActivityDto>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateActivity_ActivityNotFound_ReturnsNotFound()
    {
        // Arrange
        var updateDto = new UpdateActivityDto { Title = "New Title" };

        _mockActivityService
            .Setup(x => x.UpdateActivityAsync(TestUserId, 999, updateDto))
            .ReturnsAsync((ActivityResponseDto?)null);

        // Act
        var result = await _controller.UpdateActivity(999, updateDto);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.Value.Should().BeEquivalentTo(new { message = "Activity not found or access denied" });
    }

    [Fact]
    public async Task UpdateActivity_HierarchyViolation_ReturnsBadRequest()
    {
        // Arrange
        var updateDto = new UpdateActivityDto { Type = ActivityType.Project, ParentActivityId = 2 };

        _mockActivityService
            .Setup(x => x.UpdateActivityAsync(TestUserId, 1, updateDto))
            .ThrowsAsync(new InvalidOperationException("Project cannot have a parent"));

        // Act
        var result = await _controller.UpdateActivity(1, updateDto);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().BeEquivalentTo(new { message = "Project cannot have a parent" });
    }

    [Fact]
    public async Task UpdateActivity_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var updateDto = new UpdateActivityDto { Title = "New Title" };

        _mockActivityService
            .Setup(x => x.UpdateActivityAsync(TestUserId, 1, updateDto))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.UpdateActivity(1, updateDto);

        // Assert
        var errorResult = result.Should().BeOfType<ObjectResult>().Subject;
        errorResult.StatusCode.Should().Be(500);
        errorResult.Value.Should().BeEquivalentTo(new { message = "An error occurred while updating the activity" });
    }

    #endregion

    #region PATCH /api/activities/{id}/complete Tests

    [Fact]
    public async Task ToggleCompletion_MarkAsComplete_ReturnsUpdatedActivity()
    {
        // Arrange
        var toggleDto = new ToggleCompletionDto { ContainerId = 5, IsCompleted = true };
        var expectedResponse = new ActivityResponseDto
        {
            Id = 1, UserId = TestUserId, Title = "Test Activity", Type = ActivityType.Task,
            IsRecurring = false, RecurrenceType = RecurrenceType.None,
            CreatedAt = DateTime.UtcNow,
            Containers = new List<ContainerAssociationDto>
            {
                new() { ContainerId = 5, ContainerType = ContainerType.Weekly,
                        AddedAt = DateTime.UtcNow, CompletedAt = DateTime.UtcNow,
                        Order = 1, IsRolledOver = false }
            }
        };

        _mockActivityService
            .Setup(x => x.ToggleActivityCompletionAsync(TestUserId, 1, 5, true))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.ToggleCompletion(1, toggleDto);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var activity = okResult.Value.Should().BeOfType<ActivityResponseDto>().Subject;
        activity.Containers[0].CompletedAt.Should().NotBeNull();

        _mockActivityService.Verify(x => x.ToggleActivityCompletionAsync(TestUserId, 1, 5, true), Times.Once);
    }

    [Fact]
    public async Task ToggleCompletion_MarkAsIncomplete_ReturnsUpdatedActivity()
    {
        // Arrange
        var toggleDto = new ToggleCompletionDto { ContainerId = 5, IsCompleted = false };
        var expectedResponse = new ActivityResponseDto
        {
            Id = 1, UserId = TestUserId, Title = "Test Activity", Type = ActivityType.Task,
            IsRecurring = false, RecurrenceType = RecurrenceType.None,
            CreatedAt = DateTime.UtcNow,
            Containers = new List<ContainerAssociationDto>
            {
                new() { ContainerId = 5, ContainerType = ContainerType.Weekly,
                        AddedAt = DateTime.UtcNow, CompletedAt = null,
                        Order = 1, IsRolledOver = false }
            }
        };

        _mockActivityService
            .Setup(x => x.ToggleActivityCompletionAsync(TestUserId, 1, 5, false))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.ToggleCompletion(1, toggleDto);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var activity = okResult.Value.Should().BeOfType<ActivityResponseDto>().Subject;
        activity.Containers[0].CompletedAt.Should().BeNull();
    }

    [Fact]
    public async Task ToggleCompletion_ActivityNotFound_ReturnsNotFound()
    {
        // Arrange
        var toggleDto = new ToggleCompletionDto { ContainerId = 5, IsCompleted = true };

        _mockActivityService
            .Setup(x => x.ToggleActivityCompletionAsync(TestUserId, 999, 5, true))
            .ReturnsAsync((ActivityResponseDto?)null);

        // Act
        var result = await _controller.ToggleCompletion(999, toggleDto);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.Value.Should().BeEquivalentTo(new { message = "Activity not found or access denied" });
    }

    [Fact]
    public async Task ToggleCompletion_ActivityNotInContainer_ReturnsBadRequest()
    {
        // Arrange
        var toggleDto = new ToggleCompletionDto { ContainerId = 99, IsCompleted = true };

        _mockActivityService
            .Setup(x => x.ToggleActivityCompletionAsync(TestUserId, 1, 99, true))
            .ThrowsAsync(new InvalidOperationException("Activity 1 is not in container 99"));

        // Act
        var result = await _controller.ToggleCompletion(1, toggleDto);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().BeEquivalentTo(new { message = "Activity 1 is not in container 99" });
    }

    [Fact]
    public async Task ToggleCompletion_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var toggleDto = new ToggleCompletionDto { ContainerId = 5, IsCompleted = true };

        _mockActivityService
            .Setup(x => x.ToggleActivityCompletionAsync(TestUserId, 1, 5, true))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.ToggleCompletion(1, toggleDto);

        // Assert
        var errorResult = result.Should().BeOfType<ObjectResult>().Subject;
        errorResult.StatusCode.Should().Be(500);
        errorResult.Value.Should().BeEquivalentTo(new { message = "An error occurred while toggling activity completion" });
    }

    #endregion

    #region DELETE /api/activities/{id} Tests

    [Fact]
    public async Task DeleteActivity_WithValidId_ReturnsNoContent()
    {
        // Arrange
        _mockActivityService
            .Setup(x => x.ArchiveActivityAsync(TestUserId, 1))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteActivity(1);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        _mockActivityService.Verify(x => x.ArchiveActivityAsync(TestUserId, 1), Times.Once);
    }

    [Fact]
    public async Task DeleteActivity_ActivityNotFound_ReturnsNotFound()
    {
        // Arrange
        _mockActivityService
            .Setup(x => x.ArchiveActivityAsync(TestUserId, 999))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteActivity(999);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.Value.Should().BeEquivalentTo(new { message = "Activity not found or access denied" });
    }

    [Fact]
    public async Task DeleteActivity_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        _mockActivityService
            .Setup(x => x.ArchiveActivityAsync(TestUserId, 1))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.DeleteActivity(1);

        // Assert
        var errorResult = result.Should().BeOfType<ObjectResult>().Subject;
        errorResult.StatusCode.Should().Be(500);
        errorResult.Value.Should().BeEquivalentTo(new { message = "An error occurred while deleting the activity" });
    }

    #endregion

    #region AddToContainer Tests

    [Fact]
    public async Task AddToContainer_ServiceReturnsTrue_ReturnsNoContent()
    {
        _mockActivityService
            .Setup(x => x.AddActivityToContainerAsync(TestUserId, 1, 10))
            .ReturnsAsync(true);

        var result = await _controller.AddToContainer(1, 10);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task AddToContainer_ServiceReturnsNull_ReturnsConflict()
    {
        _mockActivityService
            .Setup(x => x.AddActivityToContainerAsync(TestUserId, 1, 10))
            .ReturnsAsync((bool?)null);

        var result = await _controller.AddToContainer(1, 10);

        result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task AddToContainer_ServiceReturnsFalse_ReturnsNotFound()
    {
        _mockActivityService
            .Setup(x => x.AddActivityToContainerAsync(TestUserId, 1, 10))
            .ReturnsAsync(false);

        var result = await _controller.AddToContainer(1, 10);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region RemoveFromContainer Tests

    [Fact]
    public async Task RemoveFromContainer_ServiceReturnsTrue_ReturnsNoContent()
    {
        _mockActivityService
            .Setup(x => x.RemoveActivityFromContainerAsync(TestUserId, 1, 10))
            .ReturnsAsync(true);

        var result = await _controller.RemoveFromContainer(1, 10);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task RemoveFromContainer_ServiceReturnsFalse_ReturnsNotFound()
    {
        _mockActivityService
            .Setup(x => x.RemoveActivityFromContainerAsync(TestUserId, 1, 10))
            .ReturnsAsync(false);

        var result = await _controller.RemoveFromContainer(1, 10);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion
}
