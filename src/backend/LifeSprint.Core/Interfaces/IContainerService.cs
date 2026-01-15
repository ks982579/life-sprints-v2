using LifeSprint.Core.Models;
using LifeSprint.Core.DTOs;

namespace LifeSprint.Core.Interfaces;

/// <summary>
/// Service for managing containers (Annual/Monthly/Weekly/Daily backlogs and sprints).
/// </summary>
/// <remarks>
/// Related files:
/// - Implementation: LifeSprint.Infrastructure/Services/ContainerService.cs
/// - Model: LifeSprint.Core/Models/Container.cs
/// - Enums: LifeSprint.Core/ContainerType.cs, ContainerStatus.cs
/// - Tests: LifeSprint.Tests/Unit/ContainerServiceTests.cs
/// </remarks>
public interface IContainerService
{
    /// <summary>
    /// Gets or creates the current active container for a user and type.
    /// Automatically determines date range based on container type.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="type">Container type (Annual/Monthly/Weekly/Daily)</param>
    /// <returns>Active container (existing or newly created)</returns>
    /// <remarks>
    /// Date ranges by type:
    /// - Annual: Jan 1 - Dec 31 of current year
    /// - Monthly: 1st - last day of current month
    /// - Weekly: Monday - Sunday of current week
    /// - Daily: Today only
    /// </remarks>
    Task<Container> GetOrCreateCurrentContainerAsync(string userId, ContainerType type);

    /// <summary>
    /// Gets a container by ID (with authorization check).
    /// </summary>
    /// <param name="userId">User ID (for authorization)</param>
    /// <param name="containerId">Container ID</param>
    /// <returns>Container with activity counts or null if not found/unauthorized</returns>
    Task<ContainerResponseDto?> GetContainerAsync(string userId, int containerId);

    /// <summary>
    /// Gets all containers for a user, optionally filtered by type.
    /// Returns containers ordered by start date descending (most recent first).
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="type">Optional container type to filter by</param>
    /// <returns>List of containers with activity counts</returns>
    Task<List<ContainerResponseDto>> GetContainersForUserAsync(string userId, ContainerType? type = null);

    /// <summary>
    /// Updates the status of a container (Active -> Completed -> Archived).
    /// </summary>
    /// <param name="userId">User ID (for authorization)</param>
    /// <param name="containerId">Container ID</param>
    /// <param name="status">New status</param>
    /// <returns>Updated container with activity counts or null if not found/unauthorized</returns>
    Task<ContainerResponseDto?> UpdateContainerStatusAsync(string userId, int containerId, ContainerStatus status);
}
