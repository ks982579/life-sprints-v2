using LifeSprint.Core;
using LifeSprint.Core.Interfaces;
using LifeSprint.Core.Models;
using LifeSprint.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

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

        return newContainer;
    }

    /// <summary>
    /// Gets a container by ID with authorization check.
    /// </summary>
    public async Task<Container?> GetContainerAsync(string userId, int containerId)
    {
        return await _context.Containers
            .FirstOrDefaultAsync(c => c.Id == containerId && c.UserId == userId);
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
        var endDate = startDate.AddDays(-1).AddMonths(1); // AddDays preserves Kind
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
}
