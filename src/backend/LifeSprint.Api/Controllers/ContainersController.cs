using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using LifeSprint.Core.Interfaces;
using LifeSprint.Core.DTOs;
using LifeSprint.Core;
using System.Security.Claims;

namespace LifeSprint.Api.Controllers;

/// <summary>
/// API controller for managing containers (backlogs and sprints).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ContainersController : ControllerBase
{
    private readonly IContainerService _containerService;

    public ContainersController(IContainerService containerService)
    {
        _containerService = containerService;
    }

    /// <summary>
    /// Gets all containers for the current user, optionally filtered by type.
    /// </summary>
    /// <param name="type">Optional container type to filter by (0=Annual, 1=Monthly, 2=Weekly, 3=Daily)</param>
    /// <returns>List of containers</returns>
    /// <response code="200">Containers retrieved successfully</response>
    /// <response code="500">Internal server error</response>
    [HttpGet]
    public async Task<IActionResult> GetContainers([FromQuery] ContainerType? type = null)
    {
        try
        {
            var userId = GetCurrentUserId();
            var containers = await _containerService.GetContainersForUserAsync(userId, type);

            return Ok(containers);
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "An error occurred while retrieving containers" });
        }
    }

    /// <summary>
    /// Gets a specific container by ID.
    /// </summary>
    /// <param name="id">Container ID</param>
    /// <returns>Container details</returns>
    /// <response code="200">Container retrieved successfully</response>
    /// <response code="404">Container not found or access denied</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetContainer(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var container = await _containerService.GetContainerAsync(userId, id);

            if (container == null)
            {
                return NotFound(new { message = "Container not found or access denied" });
            }

            return Ok(container);
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "An error occurred while retrieving the container" });
        }
    }

    /// <summary>
    /// Updates the status of a container.
    /// </summary>
    /// <param name="id">Container ID</param>
    /// <param name="dto">Status update details</param>
    /// <returns>Updated container</returns>
    /// <response code="200">Container status updated successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="404">Container not found or access denied</response>
    /// <response code="500">Internal server error</response>
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateContainerStatusDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var userId = GetCurrentUserId();
            var updatedContainer = await _containerService.UpdateContainerStatusAsync(userId, id, dto.Status);

            if (updatedContainer == null)
            {
                return NotFound(new { message = "Container not found or access denied" });
            }

            return Ok(updatedContainer);
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "An error occurred while updating container status" });
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
