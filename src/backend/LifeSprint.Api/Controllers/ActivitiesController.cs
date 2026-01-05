using Microsoft.AspNetCore.Mvc;
using LifeSprint.Core.Interfaces;
using LifeSprint.Core.DTOs;
using System.Security.Claims;

namespace LifeSprint.Api.Controllers;

/// <summary>
/// API controller for managing activity templates and their container associations.
/// </summary>
/// <remarks>
/// Related files:
/// - Service: LifeSprint.Infrastructure/Services/ActivityService.cs
/// - Interface: LifeSprint.Core/Interfaces/IActivityService.cs
/// - DTOs: LifeSprint.Core/DTOs/CreateActivityDto.cs, ActivityResponseDto.cs
/// - Tests: LifeSprint.Tests/Unit/ActivitiesControllerTests.cs
/// </remarks>
[ApiController]
[Route("api/activities")]
public class ActivitiesController : ControllerBase
{
    private readonly IActivityService _activityService;

    public ActivitiesController(IActivityService activityService)
    {
        _activityService = activityService;
    }

    /// <summary>
    /// Creates a new activity template and adds it to a container.
    /// </summary>
    /// <param name="dto">Activity creation details</param>
    /// <returns>Created activity with container associations</returns>
    /// <response code="201">Activity created successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="401">Unauthorized - container not found or access denied</response>
    /// <response code="500">Internal server error</response>
    [HttpPost]
    public async Task<IActionResult> CreateActivity([FromBody] CreateActivityDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var userId = GetCurrentUserId();
            var activity = await _activityService.CreateActivityAsync(userId, dto);

            return CreatedAtAction(
                nameof(GetActivity),
                new { id = activity.Id },
                activity);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "An error occurred while creating the activity" });
        }
    }

    /// <summary>
    /// Gets all non-archived activities for the current user.
    /// </summary>
    /// <returns>List of user's activities</returns>
    /// <response code="200">Activities retrieved successfully</response>
    /// <response code="500">Internal server error</response>
    [HttpGet]
    public async Task<IActionResult> GetActivities()
    {
        try
        {
            var userId = GetCurrentUserId();
            var activities = await _activityService.GetActivitiesForUserAsync(userId);

            return Ok(activities);
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "An error occurred while retrieving activities" });
        }
    }

    /// <summary>
    /// Gets a specific activity by ID.
    /// </summary>
    /// <param name="id">Activity template ID</param>
    /// <returns>Activity details</returns>
    /// <response code="200">Activity retrieved successfully</response>
    /// <response code="404">Activity not found or access denied</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetActivity(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var activity = await _activityService.GetActivityByIdAsync(userId, id);

            if (activity == null)
            {
                return NotFound();
            }

            return Ok(activity);
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "An error occurred while retrieving the activity" });
        }
    }

    /// <summary>
    /// Gets the current user's ID from the authenticated claims.
    /// </summary>
    /// <returns>User ID</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when user is not authenticated</exception>
    private string GetCurrentUserId()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedAccessException("User is not authenticated");
        }
        return userId;
    }
}
