using LifeSprint.Core.DTOs;

namespace LifeSprint.Core.Interfaces;

/// <summary>
/// Service for managing activity templates and their container associations.
/// </summary>
/// <remarks>
/// Related files:
/// - Implementation: LifeSprint.Infrastructure/Services/ActivityService.cs
/// - DTOs: LifeSprint.Core/DTOs/CreateActivityDto.cs, ActivityResponseDto.cs
/// - Models: LifeSprint.Core/Models/ActivityTemplate.cs
/// - Tests: LifeSprint.Tests/Unit/ActivityServiceTests.cs
/// </remarks>
public interface IActivityService
{
    /// <summary>
    /// Creates a new activity template and optionally adds it to a container.
    /// If no container is specified, adds to current Annual backlog.
    /// </summary>
    /// <param name="userId">User creating the activity</param>
    /// <param name="dto">Activity details</param>
    /// <returns>Created activity with container associations</returns>
    Task<ActivityResponseDto> CreateActivityAsync(string userId, CreateActivityDto dto);

    /// <summary>
    /// Gets all activities for a user (basic implementation for testing).
    /// Returns all non-archived activities with their container associations.
    /// </summary>
    /// <param name="userId">User ID to filter by</param>
    /// <returns>List of activities</returns>
    Task<List<ActivityResponseDto>> GetActivitiesForUserAsync(string userId);

    /// <summary>
    /// Gets a single activity by ID.
    /// </summary>
    /// <param name="userId">User ID (for authorization)</param>
    /// <param name="activityId">Activity template ID</param>
    /// <returns>Activity or null if not found/unauthorized</returns>
    Task<ActivityResponseDto?> GetActivityByIdAsync(string userId, int activityId);
}
