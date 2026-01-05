import { ActivityType, ContainerType, type Activity } from '../../types';
import './ActivityList.css';

interface ActivityListProps {
  activities: Activity[];
  containerType: ContainerType;
  onActivityClick?: (activity: Activity) => void;
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

export function ActivityList({ activities, containerType, onActivityClick }: ActivityListProps) {
  // Filter activities that belong to the selected container type
  const filteredActivities = activities.filter((activity) =>
    activity.containers.some((container) => container.containerType === containerType)
  );

  // Sort by order within the container
  const sortedActivities = filteredActivities.sort((a, b) => {
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
      {sortedActivities.map((activity) => (
        <div
          key={activity.id}
          className="activity-item"
          onClick={() => onActivityClick?.(activity)}
        >
          <div className="activity-header">
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
        </div>
      ))}
    </div>
  );
}
