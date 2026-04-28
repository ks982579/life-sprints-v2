using LifeSprint.Core.DTOs;
using LifeSprint.Core;

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
    /// Gets all activities for a user, optionally filtered by container type, container ID, or recurring flags.
    /// When <paramref name="containerId"/> is provided it takes precedence over <paramref name="containerType"/>.
    /// Returns all non-archived activities with their container associations.
    /// </summary>
    /// <param name="userId">User ID to filter by</param>
    /// <param name="containerType">Optional container type filter (Annual/Monthly/Weekly/Daily)</param>
    /// <param name="containerId">Optional specific container ID filter; overrides containerType when set</param>
    /// <param name="isRecurring">Optional filter for recurring templates</param>
    /// <param name="recurrenceType">Optional recurrence type filter (requires isRecurring=true to be meaningful)</param>
    /// <returns>List of activities matching all provided filters</returns>
    Task<List<ActivityResponseDto>> GetActivitiesForUserAsync(string userId, ContainerType? containerType = null, int? containerId = null, bool? isRecurring = null, RecurrenceType? recurrenceType = null);

    /// <summary>
    /// Gets a single activity by ID.
    /// </summary>
    /// <param name="userId">User ID (for authorization)</param>
    /// <param name="activityId">Activity template ID</param>
    /// <returns>Activity or null if not found/unauthorized</returns>
    Task<ActivityResponseDto?> GetActivityByIdAsync(string userId, int activityId);

    /// <summary>
    /// Updates an existing activity template.
    /// </summary>
    /// <param name="userId">User ID (for authorization)</param>
    /// <param name="activityId">Activity template ID to update</param>
    /// <param name="dto">Updated activity details</param>
    /// <returns>Updated activity or null if not found/unauthorized</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when user doesn't own the activity</exception>
    /// <exception cref="InvalidOperationException">Thrown when hierarchy validation fails</exception>
    Task<ActivityResponseDto?> UpdateActivityAsync(string userId, int activityId, UpdateActivityDto dto);

    /// <summary>
    /// Archives (soft deletes) an activity template by setting ArchivedAt timestamp.
    /// Archived activities are excluded from normal queries but can be recovered.
    /// </summary>
    /// <param name="userId">User ID (for authorization)</param>
    /// <param name="activityId">Activity template ID to archive</param>
    /// <returns>True if archived successfully, false if not found or unauthorized</returns>
    Task<bool> ArchiveActivityAsync(string userId, int activityId);

    /// <summary>
    /// Toggles the completion status of an activity within a specific container.
    /// Sets or clears the CompletedAt timestamp on the ContainerActivity record.
    /// </summary>
    /// <param name="userId">User ID (for authorization)</param>
    /// <param name="activityId">Activity template ID</param>
    /// <param name="containerId">Container ID where the activity should be toggled</param>
    /// <param name="isCompleted">True to mark as completed, false to mark as incomplete</param>
    /// <returns>Updated activity or null if not found/unauthorized</returns>
    Task<ActivityResponseDto?> ToggleActivityCompletionAsync(string userId, int activityId, int containerId, bool isCompleted);

    /// <summary>
    /// Adds an activity to an additional container (enables move/copy across backlogs).
    /// Returns false if the association already exists or the activity/container is not found.
    /// </summary>
    /// <param name="userId">User ID (for authorization)</param>
    /// <param name="activityId">Activity template ID</param>
    /// <param name="containerId">Target container ID</param>
    /// <returns>True on success, false if not found/unauthorized, null if already in container (conflict)</returns>
    Task<bool?> AddActivityToContainerAsync(string userId, int activityId, int containerId);

    /// <summary>
    /// Removes an activity from a specific container (does not archive the activity template).
    /// Returns false if the association is not found or user is unauthorized.
    /// </summary>
    /// <param name="userId">User ID (for authorization)</param>
    /// <param name="activityId">Activity template ID</param>
    /// <param name="containerId">Container ID to remove activity from</param>
    /// <returns>True if removed, false if not found/unauthorized</returns>
    Task<bool> RemoveActivityFromContainerAsync(string userId, int activityId, int containerId);

    /// <summary>
    /// Reorders an activity within a container by swapping its Order with the adjacent item.
    /// </summary>
    /// <param name="userId">User ID (for authorization)</param>
    /// <param name="activityId">Activity template ID</param>
    /// <param name="containerId">Container in which to reorder</param>
    /// <param name="direction">"up" to move toward lower order, "down" to move toward higher order</param>
    /// <returns>True on success, false if not found, unauthorized, already at boundary, or invalid direction</returns>
    Task<bool> ReorderActivityAsync(string userId, int activityId, int containerId, string direction);
}
