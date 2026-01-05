namespace LifeSprint.Core;

/// <summary>
/// Defines the type/level of an activity in the hierarchy.
/// </summary>
/// <remarks>
/// Hierarchy levels (from highest to lowest):
/// - Project: Large, long-term initiatives (e.g., "Build new mobile app")
/// - Epic: Major features or themes within a project (e.g., "User authentication system")
/// - Story: User-facing functionality (e.g., "User can reset password")
/// - Task: Technical work items (e.g., "Create password reset API endpoint")
/// </remarks>
public enum ActivityType
{
    /// <summary>
    /// A large, long-term initiative that may span multiple quarters or a year.
    /// Projects can contain Epics.
    /// </summary>
    Project = 0,

    /// <summary>
    /// A major feature or theme that breaks down a Project into manageable pieces.
    /// Epics can contain Stories and belong to a Project.
    /// </summary>
    Epic = 1,

    /// <summary>
    /// A user-facing piece of functionality, typically completable in a sprint.
    /// Stories can belong to an Epic and contain Tasks.
    /// </summary>
    Story = 2,

    /// <summary>
    /// A technical work item or subtask, typically completable in hours or days.
    /// Tasks can belong to a Story.
    /// </summary>
    Task = 3
}
