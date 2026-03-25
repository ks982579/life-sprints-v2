using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using LifeSprint.Core.Interfaces;
using LifeSprint.Core.DTOs;
using LifeSprint.Core;
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
[Authorize]
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
    /// <param name="containerType">Optional filter: 0=Annual, 1=Monthly, 2=Weekly, 3=Daily</param>
    /// <param name="containerId">Optional filter by specific container ID (overrides containerType)</param>
    /// <returns>List of user's activities</returns>
    /// <response code="200">Activities retrieved successfully</response>
    /// <response code="500">Internal server error</response>
    [HttpGet]
    public async Task<IActionResult> GetActivities([FromQuery] ContainerType? containerType = null, [FromQuery] int? containerId = null)
    {
        try
        {
            var userId = GetCurrentUserId();
            var activities = await _activityService.GetActivitiesForUserAsync(userId, containerType, containerId);

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
    /// Updates an existing activity.
    /// </summary>
    /// <param name="id">Activity template ID</param>
    /// <param name="dto">Updated activity details</param>
    /// <returns>Updated activity</returns>
    /// <response code="200">Activity updated successfully</response>
    /// <response code="400">Invalid request data or validation failed</response>
    /// <response code="404">Activity not found or access denied</response>
    /// <response code="500">Internal server error</response>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateActivity(int id, [FromBody] UpdateActivityDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var userId = GetCurrentUserId();
            var updatedActivity = await _activityService.UpdateActivityAsync(userId, id, dto);

            if (updatedActivity == null)
            {
                return NotFound(new { message = "Activity not found or access denied" });
            }

            return Ok(updatedActivity);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "An error occurred while updating the activity" });
        }
    }

    /// <summary>
    /// Toggles the completion status of an activity in a specific container.
    /// </summary>
    /// <param name="id">Activity template ID</param>
    /// <param name="dto">Completion toggle details</param>
    /// <returns>Updated activity</returns>
    /// <response code="200">Activity completion toggled successfully</response>
    /// <response code="400">Invalid request data or activity not in container</response>
    /// <response code="404">Activity not found or access denied</response>
    /// <response code="500">Internal server error</response>
    [HttpPatch("{id}/complete")]
    public async Task<IActionResult> ToggleCompletion(int id, [FromBody] ToggleCompletionDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var userId = GetCurrentUserId();
            var updatedActivity = await _activityService.ToggleActivityCompletionAsync(userId, id, dto.ContainerId, dto.IsCompleted);

            if (updatedActivity == null)
            {
                return NotFound(new { message = "Activity not found or access denied" });
            }

            return Ok(updatedActivity);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "An error occurred while toggling activity completion" });
        }
    }

    /// <summary>
    /// Archives (soft deletes) an activity.
    /// </summary>
    /// <param name="id">Activity template ID</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Activity archived successfully</response>
    /// <response code="404">Activity not found or access denied</response>
    /// <response code="500">Internal server error</response>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteActivity(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var success = await _activityService.ArchiveActivityAsync(userId, id);

            if (!success)
            {
                return NotFound(new { message = "Activity not found or access denied" });
            }

            return NoContent();
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "An error occurred while deleting the activity" });
        }
    }

    /// <summary>
    /// Adds an activity to an additional container.
    /// </summary>
    /// <param name="id">Activity template ID</param>
    /// <param name="containerId">Target container ID</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Activity added to container</response>
    /// <response code="404">Activity or container not found / access denied</response>
    /// <response code="409">Activity already exists in this container</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("{id}/containers/{containerId}")]
    public async Task<IActionResult> AddToContainer(int id, int containerId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _activityService.AddActivityToContainerAsync(userId, id, containerId);

            return result switch
            {
                true => NoContent(),
                null => Conflict(new { message = "Activity is already in this container" }),
                false => NotFound(new { message = "Activity or container not found or access denied" }),
            };
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "An error occurred while adding the activity to the container" });
        }
    }

    /// <summary>
    /// Removes an activity from a specific container (does not archive the activity).
    /// </summary>
    /// <param name="id">Activity template ID</param>
    /// <param name="containerId">Container ID to remove activity from</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Activity removed from container</response>
    /// <response code="404">Association not found or access denied</response>
    /// <response code="500">Internal server error</response>
    [HttpDelete("{id}/containers/{containerId}")]
    public async Task<IActionResult> RemoveFromContainer(int id, int containerId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var success = await _activityService.RemoveActivityFromContainerAsync(userId, id, containerId);

            if (!success)
            {
                return NotFound(new { message = "Activity is not in this container or access denied" });
            }

            return NoContent();
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "An error occurred while removing the activity from the container" });
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
