namespace LifeSprint.Core.Models;

public class Activity
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ActivityType Type { get; set; }
    public ActivityState State { get; set; }

    // Backlog assignments (can be in multiple backlogs)
    public bool InAnnualBacklog { get; set; }
    public bool InMonthlyBacklog { get; set; }
    public bool InWeeklySprint { get; set; }
    public bool InDailyChecklist { get; set; }

    // Hierarchy
    public Guid? ParentId { get; set; }
    public Activity? Parent { get; set; }
    public ICollection<Activity> Children { get; set; } = new List<Activity>();

    // Metadata
    public int SortOrder { get; set; }
    public int Priority { get; set; }
    public decimal? EstimatedHours { get; set; }
    public decimal? ActualHours { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public enum ActivityType
{
    Project,
    Epic,
    Story,
    Task
}

public enum ActivityState
{
    Backlog,
    Todo,
    InProgress,
    Done
}
