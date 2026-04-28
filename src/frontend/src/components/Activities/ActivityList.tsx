import { ActivityType, ContainerType, type Activity } from '../../types';
import './ActivityList.css';

interface ActivityListProps {
  activities: Activity[];
  containerType?: ContainerType;
  onActivityClick?: (activity: Activity) => void;
  onEditActivity?: (activity: Activity) => void;
  onMoveActivity?: (activity: Activity) => void;
  onActivityDelete?: (activityId: number) => void;
  onToggleCompletion?: (activityId: number, containerId: number, isCompleted: boolean) => void;
  onReorder?: (activityId: number, containerId: number, direction: 'up' | 'down') => void;
  onAddChild?: (parent: Activity) => void;
}

const activityTypeLabels: Record<number, string> = {
  [ActivityType.Project]: 'Project',
  [ActivityType.Epic]: 'Epic',
  [ActivityType.Story]: 'Story',
  [ActivityType.Task]: 'Task',
};

const activityTypeColors: Record<number, string> = {
  [ActivityType.Project]: '#9c27b0',
  [ActivityType.Epic]: '#f44336',
  [ActivityType.Story]: '#ff9800',
  [ActivityType.Task]: '#4caf50',
};

export function ActivityList({ activities, containerType, onActivityClick, onEditActivity, onMoveActivity, onActivityDelete, onToggleCompletion, onReorder, onAddChild }: ActivityListProps) {
  const handleDelete = (e: React.MouseEvent, activityId: number) => {
    e.stopPropagation(); // Prevent triggering onClick
    if (window.confirm('Are you sure you want to delete this activity? This cannot be undone.')) {
      onActivityDelete?.(activityId);
    }
  };

  const handleEdit = (e: React.MouseEvent, activity: Activity) => {
    e.stopPropagation();
    onEditActivity?.(activity);
  };

  const handleMove = (e: React.MouseEvent, activity: Activity) => {
    e.stopPropagation();
    onMoveActivity?.(activity);
  };

  const handleReorder = (e: React.MouseEvent, activity: Activity, direction: 'up' | 'down') => {
    e.stopPropagation();
    const container = containerType !== undefined
      ? activity.containers.find(c => c.containerType === containerType)
      : activity.containers[0];
    if (container && onReorder) {
      onReorder(activity.id, container.containerId, direction);
    }
  };

  const handleAddChild = (e: React.MouseEvent, activity: Activity) => {
    e.stopPropagation();
    onAddChild?.(activity);
  };

  const handleToggleCompletion = (e: React.ChangeEvent<HTMLInputElement>, activity: Activity) => {
    e.stopPropagation(); // Prevent triggering onClick
    const container = containerType !== undefined
      ? activity.containers.find(c => c.containerType === containerType)
      : activity.containers[0];
    if (container && onToggleCompletion) {
      const isCompleted = e.target.checked;
      onToggleCompletion(activity.id, container.containerId, isCompleted);
    }
  };
  // Filter activities that belong to the selected container type (skip filter when no containerType)
  const filteredActivities = containerType !== undefined
    ? activities.filter((activity) =>
        activity.containers.some((container) => container.containerType === containerType)
      )
    : activities;

  // Sort by order within the container (or by creation order if no containerType)
  const sortedActivities = [...filteredActivities].sort((a, b) => {
    if (containerType === undefined) return 0;
    const aContainer = a.containers.find((c) => c.containerType === containerType);
    const bContainer = b.containers.find((c) => c.containerType === containerType);
    return (aContainer?.order || 0) - (bContainer?.order || 0);
  });

  if (sortedActivities.length === 0) {
    return (
      <div className="activity-list-empty">
        <p>No activities in this backlog yet.</p>
        <p className="empty-hint">Click "Create New Item" to add your first activity!</p>
      </div>
    );
  }

  return (
    <div className="activity-list">
      {sortedActivities.map((activity, index) => {
        const container = containerType !== undefined
          ? activity.containers.find(c => c.containerType === containerType)
          : activity.containers[0];
        const isCompleted = !!container?.completedAt;

        return (
          <div
            key={activity.id}
            className={`activity-item ${isCompleted ? 'completed' : ''}`}
            onClick={() => onActivityClick?.(activity)}
          >
            <div className="activity-header">
              {onToggleCompletion && (
                <label className="completion-checkbox" onClick={(e) => e.stopPropagation()}>
                  <input
                    type="checkbox"
                    checked={isCompleted}
                    onChange={(e) => handleToggleCompletion(e, activity)}
                  />
                </label>
              )}
              <span
                className="activity-type-badge"
                style={{ backgroundColor: activityTypeColors[activity.type] }}
              >
                {activityTypeLabels[activity.type]}
              </span>
              <h3 className="activity-title">{activity.title}</h3>
            </div>

          {activity.description && (
            <p className="activity-description">{activity.description}</p>
          )}

          {activity.parentActivityTitle && (
            <div className="activity-parent">
              <span className="parent-label">Parent:</span>{' '}
              <span className="parent-title">{activity.parentActivityTitle}</span>
            </div>
          )}

          {activity.children.length > 0 && (
            <div className="activity-children">
              <span className="children-label">Children:</span>{' '}
              <span className="children-count">{activity.children.length}</span>
            </div>
          )}

          {activity.isRecurring && (
            <div className="activity-recurring">
              <span className="recurring-badge">Recurring</span>
            </div>
          )}

            <div className="activity-actions">
              {onReorder && (
                <>
                  <button
                    className="reorder-up-button"
                    onClick={(e) => handleReorder(e, activity, 'up')}
                    disabled={index === 0}
                    title="Move up"
                  >
                    ▲
                  </button>
                  <button
                    className="reorder-down-button"
                    onClick={(e) => handleReorder(e, activity, 'down')}
                    disabled={index === sortedActivities.length - 1}
                    title="Move down"
                  >
                    ▼
                  </button>
                </>
              )}
              {onAddChild && activity.type !== ActivityType.Task && (
                <button
                  className="add-child-button"
                  onClick={(e) => handleAddChild(e, activity)}
                  title="Add child item"
                >
                  Add
                </button>
              )}
              {onEditActivity && (
                <button
                  className="edit-button"
                  onClick={(e) => handleEdit(e, activity)}
                  title="Edit activity"
                >
                  Edit
                </button>
              )}
              {onMoveActivity && (
                <button
                  className="move-button"
                  onClick={(e) => handleMove(e, activity)}
                  title="Add to another backlog"
                >
                  Move
                </button>
              )}
              {onActivityDelete && (
                <button
                  className="delete-button"
                  onClick={(e) => handleDelete(e, activity.id)}
                  title="Delete activity"
                >
                  Delete
                </button>
              )}
            </div>
          </div>
        );
      })}
    </div>
  );
}
