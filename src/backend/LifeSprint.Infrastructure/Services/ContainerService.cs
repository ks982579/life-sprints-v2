using LifeSprint.Core;
using LifeSprint.Core.DTOs;
using LifeSprint.Core.Interfaces;
using LifeSprint.Core.Models;
using LifeSprint.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace LifeSprint.Infrastructure.Services;

/// <summary>
/// Service for managing containers (backlogs and sprints).
/// Handles container lifecycle and date range calculations.
/// </summary>
/// <remarks>
/// Related files:
/// - Interface: LifeSprint.Core/Interfaces/IContainerService.cs
/// - Model: LifeSprint.Core/Models/Container.cs
/// - Used by: LifeSprint.Infrastructure/Services/ActivityService.cs
/// </remarks>
public class ContainerService : IContainerService
{
    private readonly AppDbContext _context;

    public ContainerService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Gets or creates the current active container for a user and type.
    /// Determines date range based on container type.
    /// </summary>
    public async Task<Container> GetOrCreateCurrentContainerAsync(string userId, ContainerType type)
    {
        var (startDate, endDate) = GetDateRangeForType(type);

        // Try to find existing active container for this period
        var existingContainer = await _context.Containers
            .FirstOrDefaultAsync(c =>
                c.UserId == userId &&
                c.Type == type &&
                c.Status == ContainerStatus.Active &&
                c.StartDate == startDate);

        if (existingContainer != null)
        {
            return existingContainer;
        }

        // Create new container
        var newContainer = new Container
        {
            UserId = userId,
            Type = type,
            StartDate = startDate,
            EndDate = endDate,
            Status = ContainerStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        _context.Containers.Add(newContainer);
        await _context.SaveChangesAsync();

        await InstantiateRecurringItemsAsync(userId, newContainer);

        return newContainer;
    }

    /// <summary>
    /// Gets a container by ID with authorization check.
    /// </summary>
    public async Task<ContainerResponseDto?> GetContainerAsync(string userId, int containerId)
    {
        var container = await _context.Containers
            .Include(c => c.ContainerActivities)
            .FirstOrDefaultAsync(c => c.Id == containerId && c.UserId == userId);

        return container == null ? null : MapToDto(container);
    }

    /// <summary>
    /// Gets all containers for a user, optionally filtered by type.
    /// Returns containers ordered by start date descending (most recent first).
    /// </summary>
    public async Task<List<ContainerResponseDto>> GetContainersForUserAsync(string userId, ContainerType? type = null)
    {
        var query = _context.Containers
            .Include(c => c.ContainerActivities)
            .Where(c => c.UserId == userId);

        if (type.HasValue)
        {
            query = query.Where(c => c.Type == type.Value);
        }

        var containers = await query
            .OrderByDescending(c => c.StartDate)
            .ToListAsync();

        return containers.Select(MapToDto).ToList();
    }

    /// <summary>
    /// Updates the status of a container.
    /// </summary>
    public async Task<ContainerResponseDto?> UpdateContainerStatusAsync(string userId, int containerId, ContainerStatus status)
    {
        var container = await _context.Containers
            .Include(c => c.ContainerActivities)
            .FirstOrDefaultAsync(c => c.Id == containerId && c.UserId == userId);

        if (container == null)
        {
            return null; // Not found or unauthorized
        }

        container.Status = status;
        await _context.SaveChangesAsync();

        return MapToDto(container);
    }

    /// <summary>
    /// Creates a new container for the current period if one does not already exist.
    /// Returns null if a container already exists for this period (caller returns 409).
    /// Optionally rolls over incomplete items from the most recent previous container.
    /// </summary>
    public async Task<ContainerResponseDto?> CreateNewContainerAsync(string userId, ContainerType type, bool rolloverIncomplete)
    {
        var (startDate, endDate) = GetDateRangeForType(type);

        // Return null (conflict) if an active container already exists for this period
        var existing = await _context.Containers
            .FirstOrDefaultAsync(c =>
                c.UserId == userId &&
                c.Type == type &&
                c.Status == ContainerStatus.Active &&
                c.StartDate == startDate);

        if (existing != null)
        {
            return null;
        }

        // Find the most recent previous container for potential rollover
        Container? previousContainer = null;
        if (rolloverIncomplete)
        {
            previousContainer = await _context.Containers
                .Where(c => c.UserId == userId && c.Type == type && c.StartDate < startDate)
                .OrderByDescending(c => c.StartDate)
                .FirstOrDefaultAsync();
        }

        // Create the new container
        var newContainer = new Container
        {
            UserId = userId,
            Type = type,
            StartDate = startDate,
            EndDate = endDate,
            Status = ContainerStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        _context.Containers.Add(newContainer);
        await _context.SaveChangesAsync();

        // Roll over incomplete items from the previous container
        if (rolloverIncomplete && previousContainer != null)
        {
            var incompleteActivities = await _context.ContainerActivities
                .Where(ca => ca.ContainerId == previousContainer.Id && ca.CompletedAt == null)
                .ToListAsync();

            foreach (var ca in incompleteActivities)
            {
                var alreadyLinked = await _context.ContainerActivities
                    .AnyAsync(x => x.ActivityTemplateId == ca.ActivityTemplateId && x.ContainerId == newContainer.Id);

                if (!alreadyLinked)
                {
                    _context.ContainerActivities.Add(new ContainerActivity
                    {
                        ContainerId = newContainer.Id,
                        ActivityTemplateId = ca.ActivityTemplateId,
                        AddedAt = DateTime.UtcNow,
                        Order = ca.Order,
                        IsRolledOver = true
                    });
                }
            }

            await _context.SaveChangesAsync();
        }

        await InstantiateRecurringItemsAsync(userId, newContainer);

        return await GetContainerAsync(userId, newContainer.Id);
    }

    /// <summary>
    /// Calculates the start and end dates for a container based on its type.
    /// </summary>
    /// <remarks>
    /// Date logic:
    /// - Annual: Jan 1 - Dec 31 of current year
    /// - Monthly: 1st day - last day of current month
    /// - Weekly: Monday - Sunday of current week (ISO 8601)
    /// - Daily: Today only (same start/end date)
    /// </remarks>
    private static (DateTime startDate, DateTime endDate) GetDateRangeForType(ContainerType type)
    {
        var today = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);

        return type switch
        {
            ContainerType.Annual => GetAnnualRange(today),
            ContainerType.Monthly => GetMonthlyRange(today),
            ContainerType.Weekly => GetWeeklyRange(today),
            ContainerType.Daily => (today, today),
            _ => throw new ArgumentException($"Unknown container type: {type}")
        };
    }

    /// <summary>
    /// Gets the annual date range (Jan 1 - Dec 31 of current year).
    /// </summary>
    private static (DateTime startDate, DateTime endDate) GetAnnualRange(DateTime date)
    {
        var startDate = DateTime.SpecifyKind(new DateTime(date.Year, 1, 1), DateTimeKind.Utc);
        var endDate = DateTime.SpecifyKind(new DateTime(date.Year, 12, 31), DateTimeKind.Utc);
        return (startDate, endDate);
    }

    /// <summary>
    /// Gets the monthly date range (1st - last day of current month).
    /// </summary>
    private static (DateTime startDate, DateTime endDate) GetMonthlyRange(DateTime date)
    {
        var startDate = DateTime.SpecifyKind(new DateTime(date.Year, date.Month, 1), DateTimeKind.Utc);
        var endDate = startDate.AddMonths(1).AddDays(-1); // First day of next month minus 1 = last day of current month
        return (startDate, endDate);
    }

    /// <summary>
    /// Gets the weekly date range (Monday - Sunday of current week, ISO 8601).
    /// </summary>
    private static (DateTime startDate, DateTime endDate) GetWeeklyRange(DateTime date)
    {
        // Calculate days since Monday (ISO 8601: Monday = 1, Sunday = 7)
        var dayOfWeek = (int)date.DayOfWeek;
        var daysSinceMonday = dayOfWeek == 0 ? 6 : dayOfWeek - 1; // Sunday = 0 in C#, should be 6

        var startDate = date.AddDays(-daysSinceMonday); // AddDays preserves Kind
        var endDate = startDate.AddDays(6);

        return (startDate, endDate);
    }

    /// <summary>
    /// Builds the stamped title for a concrete recurring instance (e.g., "Pay Bills | April 2026").
    /// Weekly containers use StartDate - 1 day to display the Sunday of the week.
    /// </summary>
    private static string BuildStampedTitle(string baseTitle, Container container)
    {
        return container.Type switch
        {
            ContainerType.Annual => $"{baseTitle} | {container.StartDate.Year}",
            ContainerType.Monthly => $"{baseTitle} | {container.StartDate.ToString("MMMM yyyy", CultureInfo.CurrentCulture)}",
            ContainerType.Weekly => $"{baseTitle} | Week of {container.StartDate.AddDays(-1):yyyy-MM-dd}",
            ContainerType.Daily => $"{baseTitle} | {container.StartDate:yyyy-MM-dd}",
            _ => baseTitle
        };
    }

    /// <summary>
    /// Instantiates recurring templates matching the container's type into concrete stamped copies.
    /// Skips templates that already have a concrete instance in this container (idempotent).
    /// Handles parent-child recurring relationships via topological ordering.
    /// </summary>
    private async Task InstantiateRecurringItemsAsync(string userId, Container newContainer)
    {
        var matchingRecurrenceType = newContainer.Type switch
        {
            ContainerType.Annual => RecurrenceType.Annual,
            ContainerType.Monthly => RecurrenceType.Monthly,
            ContainerType.Weekly => RecurrenceType.Weekly,
            ContainerType.Daily => RecurrenceType.Daily,
            _ => (RecurrenceType?)null
        };

        if (matchingRecurrenceType == null) return;

        var templates = await _context.ActivityTemplates
            .Where(at => at.UserId == userId && at.IsRecurring && at.RecurrenceType == matchingRecurrenceType && at.ArchivedAt == null)
            .OrderBy(at => at.Id)
            .ToListAsync();

        if (!templates.Any()) return;

        // Topological ordering: parents before children
        var ordered = TopologicalSort(templates);

        // Map from template ID → concrete instance ID (for child parent resolution)
        var templateToConcrete = new Dictionary<int, int>();

        foreach (var template in ordered)
        {
            var stampedTitle = BuildStampedTitle(template.Title, newContainer);

            // Skip if already instantiated in this container
            var alreadyExists = await _context.ActivityTemplates
                .AnyAsync(at => at.UserId == userId && at.Title == stampedTitle
                    && at.ContainerActivities.Any(ca => ca.ContainerId == newContainer.Id));
            if (alreadyExists) continue;

            // Resolve concrete parent ID if the template has a parent
            int? concreteParentId = null;
            if (template.ParentActivityId.HasValue && templateToConcrete.TryGetValue(template.ParentActivityId.Value, out var mappedParentId))
            {
                concreteParentId = mappedParentId;
            }

            var concreteInstance = new ActivityTemplate
            {
                UserId = userId,
                Title = stampedTitle,
                Description = template.Description,
                Type = template.Type,
                ParentActivityId = concreteParentId,
                IsRecurring = false,
                RecurrenceType = RecurrenceType.None,
                CreatedAt = DateTime.UtcNow
            };

            _context.ActivityTemplates.Add(concreteInstance);
            await _context.SaveChangesAsync();

            templateToConcrete[template.Id] = concreteInstance.Id;

            var order = await GetNextOrderInContainerAsync(newContainer.Id);
            _context.ContainerActivities.Add(new ContainerActivity
            {
                ContainerId = newContainer.Id,
                ActivityTemplateId = concreteInstance.Id,
                AddedAt = DateTime.UtcNow,
                Order = order,
                IsRolledOver = false
            });
            await _context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Returns a topologically sorted list of templates (parents before children).
    /// Templates without a parent in the list come first.
    /// </summary>
    private static List<ActivityTemplate> TopologicalSort(List<ActivityTemplate> templates)
    {
        var idSet = templates.ToDictionary(t => t.Id);
        var result = new List<ActivityTemplate>();
        var visited = new HashSet<int>();

        void Visit(ActivityTemplate t)
        {
            if (visited.Contains(t.Id)) return;
            visited.Add(t.Id);
            if (t.ParentActivityId.HasValue && idSet.TryGetValue(t.ParentActivityId.Value, out var parent))
                Visit(parent);
            result.Add(t);
        }

        foreach (var t in templates) Visit(t);
        return result;
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
    /// Maps a Container entity to ContainerResponseDto with activity counts.
    /// </summary>
    private static ContainerResponseDto MapToDto(Container container)
    {
        var totalActivities = container.ContainerActivities.Count;
        var completedActivities = container.ContainerActivities.Count(ca => ca.CompletedAt != null);

        return new ContainerResponseDto
        {
            Id = container.Id,
            UserId = container.UserId,
            Type = container.Type,
            StartDate = container.StartDate,
            EndDate = container.EndDate,
            Status = container.Status,
            Comments = container.Comments,
            CreatedAt = container.CreatedAt,
            ArchivedAt = container.ArchivedAt,
            TotalActivities = totalActivities,
            CompletedActivities = completedActivities
        };
    }
}
