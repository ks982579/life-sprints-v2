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
