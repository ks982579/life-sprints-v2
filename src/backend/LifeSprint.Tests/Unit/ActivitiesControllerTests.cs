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
            Title = "Learn ASP.NET Core",
            Description = "Build REST APIs",
            IsRecurring = false,
            RecurrenceType = RecurrenceType.None
        };

        var expectedResponse = new ActivityResponseDto
        {
            Id = 1,
            UserId = TestUserId,
            Title = "Learn ASP.NET Core",
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
            Title = "", // Invalid - empty title
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
            Title = "Test Activity",
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
            Title = "Test Activity"
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
            Title = "Weekly Review",
            Description = "Review progress and plan ahead",
            IsRecurring = true,
            RecurrenceType = RecurrenceType.Weekly
        };

        var expectedResponse = new ActivityResponseDto
        {
            Id = 2,
            UserId = TestUserId,
            Title = "Weekly Review",
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
                Title = "Activity 1",
                IsRecurring = false,
                RecurrenceType = RecurrenceType.None,
                CreatedAt = DateTime.UtcNow,
                Containers = new List<ContainerAssociationDto>()
            },
            new()
            {
                Id = 2,
                UserId = TestUserId,
                Title = "Activity 2",
                IsRecurring = true,
                RecurrenceType = RecurrenceType.Weekly,
                CreatedAt = DateTime.UtcNow,
                Containers = new List<ContainerAssociationDto>()
            }
        };

        _mockActivityService
            .Setup(x => x.GetActivitiesForUserAsync(TestUserId))
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
            x => x.GetActivitiesForUserAsync(TestUserId),
            Times.Once);
    }

    [Fact]
    public async Task GetActivities_WithNoActivities_ReturnsEmptyList()
    {
        // Arrange
        _mockActivityService
            .Setup(x => x.GetActivitiesForUserAsync(TestUserId))
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
            .Setup(x => x.GetActivitiesForUserAsync(TestUserId))
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
            Title = "Test Activity",
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
}
