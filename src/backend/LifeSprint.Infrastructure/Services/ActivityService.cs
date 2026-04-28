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

        if (!dto.SkipContainerLink)
        {
            // Determine which container to add the activity to
            Container container;
            if (dto.ContainerId.HasValue)
            {
                // Use specified container (with authorization check)
                var specifiedContainer = await _context.Containers
                    .FirstOrDefaultAsync(c => c.Id == dto.ContainerId.Value && c.UserId == userId);
                if (specifiedContainer == null)
                {
                    throw new UnauthorizedAccessException($"Container {dto.ContainerId} not found or unauthorized");
                }
                container = specifiedContainer;
            }
            else
            {
                // Use provided ContainerType, or fall back to Annual
                var fallbackType = dto.DefaultContainerType ?? ContainerType.Annual;
                container = await _containerService.GetOrCreateCurrentContainerAsync(userId, fallbackType);
            }

            // Create the container-activity association for the target container
            var containerActivity = new ContainerActivity
            {
                ContainerId = container.Id,
                ActivityTemplateId = activityTemplate.Id,
                AddedAt = DateTime.UtcNow,
                Order = await GetNextOrderInContainerAsync(container.Id),
                IsRolledOver = false
            };

            _context.ContainerActivities.Add(containerActivity);

            // Auto-propagate to parent containers (Weekly → Monthly → Annual, etc.)
            foreach (var parentType in GetParentContainerTypes(container.Type))
            {
                var parentContainer = await _containerService.GetOrCreateCurrentContainerAsync(userId, parentType);
                var alreadyLinked = await _context.ContainerActivities
                    .AnyAsync(ca => ca.ActivityTemplateId == activityTemplate.Id && ca.ContainerId == parentContainer.Id);
                if (!alreadyLinked)
                {
                    _context.ContainerActivities.Add(new ContainerActivity
                    {
                        ContainerId = parentContainer.Id,
                        ActivityTemplateId = activityTemplate.Id,
                        AddedAt = DateTime.UtcNow,
                        Order = await GetNextOrderInContainerAsync(parentContainer.Id),
                        IsRolledOver = false
                    });
                }
            }

            await _context.SaveChangesAsync();
        }

        // Return the created activity with container associations
        return await GetActivityByIdAsync(userId, activityTemplate.Id)
            ?? throw new InvalidOperationException("Failed to retrieve created activity");
    }

    /// <summary>
    /// Gets all non-archived activities for a user, optionally filtered by container type, container ID, or recurring flags.
    /// When containerId is provided it takes precedence over containerType.
    /// </summary>
    public async Task<List<ActivityResponseDto>> GetActivitiesForUserAsync(string userId, ContainerType? containerType = null, int? containerId = null, bool? isRecurring = null, RecurrenceType? recurrenceType = null)
    {
        var query = _context.ActivityTemplates
            .Where(at => at.UserId == userId && at.ArchivedAt == null)
            .Include(at => at.ParentActivity)
            .Include(at => at.ChildActivities)
            .Include(at => at.ContainerActivities)
                .ThenInclude(ca => ca.Container)
            .AsQueryable();

        if (containerId.HasValue)
        {
            // Filter by specific container ID (overrides containerType)
            query = query.Where(at =>
                at.ContainerActivities.Any(ca => ca.ContainerId == containerId.Value && ca.Container.UserId == userId));
        }
        else if (containerType.HasValue)
        {
            query = query.Where(at =>
                at.ContainerActivities.Any(ca => ca.Container.Type == containerType.Value));
        }

        if (isRecurring.HasValue)
            query = query.Where(at => at.IsRecurring == isRecurring.Value);

        if (recurrenceType.HasValue)
            query = query.Where(at => at.RecurrenceType == recurrenceType.Value);

        var activities = await query
            .OrderByDescending(at => at.CreatedAt)
            .ToListAsync();

        return activities.Select(MapToDto).ToList();
    }

    /// <summary>
    /// Adds an activity to an additional container.
    /// Returns null (conflict) if the association already exists, false if not found/unauthorized, true on success.
    /// </summary>
    public async Task<bool?> AddActivityToContainerAsync(string userId, int activityId, int containerId)
    {
        // Verify activity belongs to user and is not archived
        var activity = await _context.ActivityTemplates
            .FirstOrDefaultAsync(at => at.Id == activityId && at.UserId == userId && at.ArchivedAt == null);

        if (activity == null)
        {
            return false;
        }

        // Verify container belongs to user
        var container = await _context.Containers
            .FirstOrDefaultAsync(c => c.Id == containerId && c.UserId == userId);

        if (container == null)
        {
            return false;
        }

        // Check for existing association
        var existingLink = await _context.ContainerActivities
            .AnyAsync(ca => ca.ActivityTemplateId == activityId && ca.ContainerId == containerId);

        if (existingLink)
        {
            return null; // Conflict: already in this container
        }

        var containerActivity = new ContainerActivity
        {
            ContainerId = containerId,
            ActivityTemplateId = activityId,
            AddedAt = DateTime.UtcNow,
            Order = await GetNextOrderInContainerAsync(containerId),
            IsRolledOver = false
        };

        _context.ContainerActivities.Add(containerActivity);

        // Auto-propagate to parent containers
        foreach (var parentType in GetParentContainerTypes(container.Type))
        {
            var parentContainer = await _containerService.GetOrCreateCurrentContainerAsync(userId, parentType);
            var alreadyLinked = await _context.ContainerActivities
                .AnyAsync(ca => ca.ActivityTemplateId == activityId && ca.ContainerId == parentContainer.Id);
            if (!alreadyLinked)
            {
                _context.ContainerActivities.Add(new ContainerActivity
                {
                    ContainerId = parentContainer.Id,
                    ActivityTemplateId = activityId,
                    AddedAt = DateTime.UtcNow,
                    Order = await GetNextOrderInContainerAsync(parentContainer.Id),
                    IsRolledOver = false
                });
            }
        }

        await _context.SaveChangesAsync();

        return true;
    }

    /// <summary>
    /// Removes an activity from a specific container without archiving the activity template.
    /// </summary>
    public async Task<bool> RemoveActivityFromContainerAsync(string userId, int activityId, int containerId)
    {
        // Find the ContainerActivity with authorization check on both sides
        var containerActivity = await _context.ContainerActivities
            .Include(ca => ca.Container)
            .FirstOrDefaultAsync(ca =>
                ca.ActivityTemplateId == activityId &&
                ca.ContainerId == containerId &&
                ca.Container.UserId == userId);

        if (containerActivity == null)
        {
            return false;
        }

        _context.ContainerActivities.Remove(containerActivity);
        await _context.SaveChangesAsync();

        return true;
    }

    /// <summary>
    /// Gets a single activity by ID with authorization check.
    /// </summary>
    public async Task<ActivityResponseDto?> GetActivityByIdAsync(string userId, int activityId)
    {
        var activity = await _context.ActivityTemplates
            .Where(at => at.Id == activityId && at.UserId == userId && at.ArchivedAt == null)
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
    /// Toggles the completion status of an activity within a specific container.
    /// </summary>
    public async Task<ActivityResponseDto?> ToggleActivityCompletionAsync(string userId, int activityId, int containerId, bool isCompleted)
    {
        // Fetch the activity with authorization check
        var activity = await _context.ActivityTemplates
            .FirstOrDefaultAsync(at => at.Id == activityId && at.UserId == userId);

        if (activity == null)
        {
            return null; // Not found or unauthorized
        }

        // Verify the specific container association exists and belongs to the user
        var targetContainerActivity = await _context.ContainerActivities
            .Include(ca => ca.Container)
            .FirstOrDefaultAsync(ca => ca.ActivityTemplateId == activityId && ca.ContainerId == containerId);

        if (targetContainerActivity == null)
        {
            throw new InvalidOperationException($"Activity {activityId} is not associated with container {containerId}");
        }

        if (targetContainerActivity.Container.UserId != userId)
        {
            return null; // Unauthorized access to container
        }

        // Toggle completion across ALL containers for this activity (shared completion state)
        var allContainerActivities = await _context.ContainerActivities
            .Include(ca => ca.Container)
            .Where(ca => ca.ActivityTemplateId == activityId && ca.Container.UserId == userId)
            .ToListAsync();

        var completedAt = isCompleted ? DateTime.UtcNow : (DateTime?)null;
        foreach (var ca in allContainerActivities)
        {
            ca.CompletedAt = completedAt;
        }

        await _context.SaveChangesAsync();

        // Return the updated activity with all associations
        return await GetActivityByIdAsync(userId, activityId);
    }

    /// <summary>
    /// Reorders an activity within a container by swapping its Order with the adjacent item.
    /// </summary>
    public async Task<bool> ReorderActivityAsync(string userId, int activityId, int containerId, string direction)
    {
        if (direction != "up" && direction != "down")
            return false;

        var targetCa = await _context.ContainerActivities
            .Include(ca => ca.Container)
            .FirstOrDefaultAsync(ca => ca.ActivityTemplateId == activityId && ca.ContainerId == containerId);

        if (targetCa == null || targetCa.Container.UserId != userId)
            return false;

        var allInContainer = await _context.ContainerActivities
            .Where(ca => ca.ContainerId == containerId)
            .OrderBy(ca => ca.Order)
            .ToListAsync();

        var index = allInContainer.FindIndex(ca => ca.ActivityTemplateId == activityId);

        if (direction == "up" && index == 0) return false;
        if (direction == "down" && index == allInContainer.Count - 1) return false;

        var neighborIndex = direction == "up" ? index - 1 : index + 1;
        var neighbor = allInContainer[neighborIndex];

        (allInContainer[index].Order, neighbor.Order) = (neighbor.Order, allInContainer[index].Order);

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
    /// Returns the parent container types for a given type (Daily→Weekly→Monthly→Annual).
    /// Used to auto-propagate activities upward when added to a lower-level container.
    /// </summary>
    private static ContainerType[] GetParentContainerTypes(ContainerType type)
    {
        return type switch
        {
            ContainerType.Daily => [ContainerType.Weekly, ContainerType.Monthly, ContainerType.Annual],
            ContainerType.Weekly => [ContainerType.Monthly, ContainerType.Annual],
            ContainerType.Monthly => [ContainerType.Annual],
            _ => []
        };
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
            { ActivityType.Project, new List<ActivityType>() },
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
