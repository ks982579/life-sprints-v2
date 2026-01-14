using LifeSprint.Core;
using LifeSprint.Core.DTOs;
using LifeSprint.Core.Interfaces;
using LifeSprint.Core.Models;
using LifeSprint.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LifeSprint.Infrastructure.Services;

/// <summary>
/// Service for managing activity templates and their container associations.
/// </summary>
/// <remarks>
/// Related files:
/// - Interface: LifeSprint.Core/Interfaces/IActivityService.cs
/// - DTOs: LifeSprint.Core/DTOs/CreateActivityDto.cs, ActivityResponseDto.cs
/// - Models: LifeSprint.Core/Models/ActivityTemplate.cs, ContainerActivity.cs
/// - Dependencies: IContainerService for managing containers
/// </remarks>
public class ActivityService : IActivityService
{
    private readonly AppDbContext _context;
    private readonly IContainerService _containerService;

    public ActivityService(AppDbContext context, IContainerService containerService)
    {
        _context = context;
        _containerService = containerService;
    }

    /// <summary>
    /// Creates a new activity template and adds it to a container.
    /// If no container specified, adds to current Annual backlog.
    /// </summary>
    public async Task<ActivityResponseDto> CreateActivityAsync(string userId, CreateActivityDto dto)
    {
        // Validate parent relationship if specified
        if (dto.ParentActivityId.HasValue)
        {
            var parent = await _context.ActivityTemplates
                .FirstOrDefaultAsync(at => at.Id == dto.ParentActivityId.Value && at.UserId == userId);

            if (parent == null)
            {
                throw new InvalidOperationException($"Parent activity {dto.ParentActivityId} not found or unauthorized");
            }

            // Validate hierarchy rules (optional but recommended)
            ValidateHierarchy(dto.Type, parent.Type);
        }

        // Create the activity template
        var activityTemplate = new ActivityTemplate
        {
            UserId = userId,
            Title = dto.Title,
            Description = dto.Description,
            Type = dto.Type,
            ParentActivityId = dto.ParentActivityId,
            IsRecurring = dto.IsRecurring,
            RecurrenceType = dto.RecurrenceType,
            CreatedAt = DateTime.UtcNow
        };

        _context.ActivityTemplates.Add(activityTemplate);
        await _context.SaveChangesAsync(); // Save to get the ID

        // Determine which container to add the activity to
        Container container;
        if (dto.ContainerId.HasValue)
        {
            // Use specified container (with authorization check)
            var specifiedContainer = await _containerService.GetContainerAsync(userId, dto.ContainerId.Value);
            if (specifiedContainer == null)
            {
                throw new UnauthorizedAccessException($"Container {dto.ContainerId} not found or unauthorized");
            }
            container = specifiedContainer;
        }
        else
        {
            // Default to current Annual backlog
            container = await _containerService.GetOrCreateCurrentContainerAsync(userId, ContainerType.Annual);
        }

        // Create the container-activity association
        var containerActivity = new ContainerActivity
        {
            ContainerId = container.Id,
            ActivityTemplateId = activityTemplate.Id,
            AddedAt = DateTime.UtcNow,
            Order = await GetNextOrderInContainerAsync(container.Id),
            IsRolledOver = false
        };

        _context.ContainerActivities.Add(containerActivity);
        await _context.SaveChangesAsync();

        // Return the created activity with container associations
        return await GetActivityByIdAsync(userId, activityTemplate.Id)
            ?? throw new InvalidOperationException("Failed to retrieve created activity");
    }

    /// <summary>
    /// Gets all non-archived activities for a user with their container associations.
    /// </summary>
    public async Task<List<ActivityResponseDto>> GetActivitiesForUserAsync(string userId)
    {
        var activities = await _context.ActivityTemplates
            .Where(at => at.UserId == userId && at.ArchivedAt == null)
            .Include(at => at.ParentActivity)
            .Include(at => at.ChildActivities)
            .Include(at => at.ContainerActivities)
                .ThenInclude(ca => ca.Container)
            .OrderByDescending(at => at.CreatedAt)
            .ToListAsync();

        return activities.Select(MapToDto).ToList();
    }

    /// <summary>
    /// Gets a single activity by ID with authorization check.
    /// </summary>
    public async Task<ActivityResponseDto?> GetActivityByIdAsync(string userId, int activityId)
    {
        var activity = await _context.ActivityTemplates
            .Where(at => at.Id == activityId && at.UserId == userId)
            .Include(at => at.ParentActivity)
            .Include(at => at.ChildActivities)
            .Include(at => at.ContainerActivities)
                .ThenInclude(ca => ca.Container)
            .FirstOrDefaultAsync();

        return activity != null ? MapToDto(activity) : null;
    }

    /// <summary>
    /// Updates an existing activity template.
    /// Only updates fields that are provided (not null).
    /// </summary>
    public async Task<ActivityResponseDto?> UpdateActivityAsync(string userId, int activityId, UpdateActivityDto dto)
    {
        // Fetch the activity with authorization check
        var activity = await _context.ActivityTemplates
            .Include(at => at.ChildActivities)
            .FirstOrDefaultAsync(at => at.Id == activityId && at.UserId == userId);

        if (activity == null)
        {
            return null;
        }

        // Track if we need to validate hierarchy
        var typeChanged = dto.Type.HasValue && dto.Type.Value != activity.Type;
        var parentChanged = dto.ParentActivityId != activity.ParentActivityId;

        // Validate new parent relationship if parent is being changed or type is changing
        if (parentChanged || typeChanged)
        {
            var newParentId = parentChanged ? dto.ParentActivityId : activity.ParentActivityId;
            var newType = dto.Type ?? activity.Type;

            if (newParentId.HasValue)
            {
                // Fetch parent to validate it exists and get its type
                var parent = await _context.ActivityTemplates
                    .FirstOrDefaultAsync(at => at.Id == newParentId.Value && at.UserId == userId);

                if (parent == null)
                {
                    throw new InvalidOperationException($"Parent activity {newParentId} not found or unauthorized");
                }

                // Check for circular reference (activity becoming its own descendant)
                if (await WouldCreateCircularReferenceAsync(activityId, newParentId.Value))
                {
                    throw new InvalidOperationException("Cannot set parent: would create circular reference");
                }

                // Validate hierarchy rules
                ValidateHierarchy(newType, parent.Type);
            }

            // If changing type, ensure all existing children are still valid
            if (typeChanged && activity.ChildActivities.Any())
            {
                var updatedType = dto.Type!.Value;
                foreach (var child in activity.ChildActivities)
                {
                    try
                    {
                        ValidateHierarchy(child.Type, updatedType);
                    }
                    catch (InvalidOperationException ex)
                    {
                        throw new InvalidOperationException(
                            $"Cannot change type to {updatedType}: would invalidate child activity '{child.Title}' ({child.Type}). {ex.Message}");
                    }
                }
            }
        }

        // Update fields that were provided
        if (dto.Title != null)
        {
            activity.Title = dto.Title;
        }

        if (dto.Description != null)
        {
            activity.Description = dto.Description;
        }

        if (dto.Type.HasValue)
        {
            activity.Type = dto.Type.Value;
        }

        if (parentChanged)
        {
            activity.ParentActivityId = dto.ParentActivityId;
        }

        if (dto.IsRecurring.HasValue)
        {
            activity.IsRecurring = dto.IsRecurring.Value;
        }

        if (dto.RecurrenceType.HasValue)
        {
            activity.RecurrenceType = dto.RecurrenceType.Value;
        }

        await _context.SaveChangesAsync();

        // Return the updated activity with all associations
        return await GetActivityByIdAsync(userId, activityId);
    }

    /// <summary>
    /// Archives (soft deletes) an activity template by setting ArchivedAt timestamp.
    /// Archived activities are hidden from normal queries but can be recovered.
    /// </summary>
    public async Task<bool> ArchiveActivityAsync(string userId, int activityId)
    {
        // Fetch the activity with authorization check
        var activity = await _context.ActivityTemplates
            .FirstOrDefaultAsync(at => at.Id == activityId && at.UserId == userId);

        if (activity == null)
        {
            return false; // Not found or unauthorized
        }

        // Set the archived timestamp
        activity.ArchivedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return true;
    }

    /// <summary>
    /// Checks if setting a parent would create a circular reference.
    /// </summary>
    private async Task<bool> WouldCreateCircularReferenceAsync(int activityId, int proposedParentId)
    {
        // Walk up the parent chain to see if we encounter the activity itself
        var currentId = proposedParentId;
        var visited = new HashSet<int>();

        while (currentId != 0)
        {
            if (currentId == activityId)
            {
                return true; // Found circular reference
            }

            if (!visited.Add(currentId))
            {
                // Already visited this node, circular reference in parent chain (shouldn't happen but defensive)
                return true;
            }

            var parent = await _context.ActivityTemplates
                .Where(at => at.Id == currentId)
                .Select(at => new { at.ParentActivityId })
                .FirstOrDefaultAsync();

            if (parent?.ParentActivityId == null)
            {
                break;
            }

            currentId = parent.ParentActivityId.Value;
        }

        return false;
    }

    /// <summary>
    /// Gets the next order value for activities in a container.
    /// </summary>
    private async Task<int> GetNextOrderInContainerAsync(int containerId)
    {
        var maxOrder = await _context.ContainerActivities
            .Where(ca => ca.ContainerId == containerId)
            .MaxAsync(ca => (int?)ca.Order);

        return (maxOrder ?? 0) + 1;
    }

    /// <summary>
    /// Validates that the parent-child relationship follows proper hierarchy rules.
    /// </summary>
    private static void ValidateHierarchy(ActivityType childType, ActivityType parentType)
    {
        // Define valid parent-child relationships
        var validRelationships = new Dictionary<ActivityType, List<ActivityType>>
        {
            { ActivityType.Epic, new List<ActivityType> { ActivityType.Project } },
            { ActivityType.Story, new List<ActivityType> { ActivityType.Epic, ActivityType.Project } },
            { ActivityType.Task, new List<ActivityType> { ActivityType.Story, ActivityType.Epic } }
        };

        if (validRelationships.ContainsKey(childType))
        {
            if (!validRelationships[childType].Contains(parentType))
            {
                throw new InvalidOperationException(
                    $"Invalid hierarchy: {childType} cannot be a child of {parentType}. " +
                    $"Valid parents for {childType}: {string.Join(", ", validRelationships[childType])}");
            }
        }
    }

    /// <summary>
    /// Maps an ActivityTemplate entity to ActivityResponseDto.
    /// </summary>
    private static ActivityResponseDto MapToDto(ActivityTemplate activity)
    {
        return new ActivityResponseDto
        {
            Id = activity.Id,
            UserId = activity.UserId,
            Title = activity.Title,
            Description = activity.Description,
            Type = activity.Type,
            ParentActivityId = activity.ParentActivityId,
            ParentActivityTitle = activity.ParentActivity?.Title,
            IsRecurring = activity.IsRecurring,
            RecurrenceType = activity.RecurrenceType,
            CreatedAt = activity.CreatedAt,
            ArchivedAt = activity.ArchivedAt,
            Containers = activity.ContainerActivities.Select(ca => new ContainerAssociationDto
            {
                ContainerId = ca.ContainerId,
                ContainerType = ca.Container.Type,
                AddedAt = ca.AddedAt,
                CompletedAt = ca.CompletedAt,
                Order = ca.Order,
                IsRolledOver = ca.IsRolledOver
            }).ToList(),
            Children = activity.ChildActivities.Select(child => new ActivityChildDto
            {
                Id = child.Id,
                Title = child.Title,
                Type = child.Type
            }).ToList()
        };
    }
}
