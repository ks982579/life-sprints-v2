import { useEffect } from 'react';
import { ActivityType, type Activity } from '../../types';
import styles from './ActivityDetailModal.module.css';

interface ActivityDetailModalProps {
  activity: Activity | null;
  onClose: () => void;
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

export function ActivityDetailModal({ activity, onClose }: ActivityDetailModalProps) {
  // Close on Escape key
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') {
        onClose();
      }
    };
    document.addEventListener('keydown', handleKeyDown);
    return () => document.removeEventListener('keydown', handleKeyDown);
  }, [onClose]);

  if (!activity) {
    return null;
  }

  return (
    <div className={styles.overlay} onClick={onClose} role="dialog" aria-modal="true">
      <div className={styles.modal} onClick={(e) => e.stopPropagation()}>
        <button className={styles.closeButton} onClick={onClose} aria-label="Close">
          &times;
        </button>

        <div className={styles.header}>
          <span
            className={styles.typeBadge}
            style={{ backgroundColor: activityTypeColors[activity.type] }}
          >
            {activityTypeLabels[activity.type]}
          </span>
          <h2 className={styles.title}>{activity.title}</h2>
        </div>

        {activity.description && (
          <p className={styles.description}>{activity.description}</p>
        )}

        {activity.parentActivityTitle && (
          <div className={styles.section}>
            <span className={styles.sectionLabel}>Parent</span>
            <span className={styles.parentTitle}>{activity.parentActivityTitle}</span>
          </div>
        )}

        {activity.isRecurring && (
          <div className={styles.section}>
            <span className={styles.recurringBadge}>Recurring</span>
          </div>
        )}

        {activity.children.length > 0 && (
          <div className={styles.section}>
            <span className={styles.sectionLabel}>
              Children ({activity.children.length})
            </span>
            <ul className={styles.childList}>
              {activity.children.map((child) => (
                <li key={child.id} className={styles.childItem}>
                  <span
                    className={styles.childTypeBadge}
                    style={{ backgroundColor: activityTypeColors[child.type] }}
                  >
                    {activityTypeLabels[child.type]}
                  </span>
                  <span className={styles.childTitle}>{child.title}</span>
                </li>
              ))}
            </ul>
          </div>
        )}

        <div className={styles.footer}>
          <span className={styles.metaText}>
            Created {new Date(activity.createdAt).toLocaleDateString('en-GB')}
          </span>
        </div>
      </div>
    </div>
  );
}
