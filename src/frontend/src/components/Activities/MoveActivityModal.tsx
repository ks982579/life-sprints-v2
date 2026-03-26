import { useEffect, useState } from 'react';
import { ContainerType, ContainerStatus, type Activity, type Container } from '../../types';
import styles from './MoveActivityModal.module.css';

interface MoveActivityModalProps {
  activity: Activity;
  currentContainerId: number | null;
  availableContainers: Container[];
  onMove: (targetContainerId: number) => Promise<void>;
  onClose: () => void;
}

const containerTypeLabels: Record<number, string> = {
  [ContainerType.Annual]: 'Annual',
  [ContainerType.Monthly]: 'Monthly',
  [ContainerType.Weekly]: 'Weekly',
  [ContainerType.Daily]: 'Daily',
};

const containerTypeOrder = [
  ContainerType.Annual,
  ContainerType.Monthly,
  ContainerType.Weekly,
  ContainerType.Daily,
];

export function MoveActivityModal({
  activity,
  currentContainerId,
  availableContainers,
  onMove,
  onClose,
}: MoveActivityModalProps) {
  const [moving, setMoving] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

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

  // Determine which containers the activity already belongs to
  const activityContainerIds = new Set(activity.containers.map((c) => c.containerId));

  // Show all non-archived containers grouped by type
  const activeContainers = availableContainers.filter(
    (c) => c.status !== ContainerStatus.Archived
  );

  // Group by type, in hierarchy order
  const grouped = containerTypeOrder
    .map((type) => ({
      type,
      label: containerTypeLabels[type],
      containers: activeContainers.filter((c) => c.type === type),
    }))
    .filter((group) => group.containers.length > 0);

  const handleMove = async (targetId: number) => {
    if (activityContainerIds.has(targetId)) {
      setErrorMessage('Activity is already in that backlog.');
      return;
    }
    try {
      setMoving(true);
      setErrorMessage(null);
      await onMove(targetId);
    } catch {
      setErrorMessage('Failed to add activity. Please try again.');
    } finally {
      setMoving(false);
    }
  };

  return (
    <div className={styles.overlay} onClick={onClose} role="dialog" aria-modal="true">
      <div className={styles.modal} onClick={(e) => e.stopPropagation()}>
        <button className={styles.closeButton} onClick={onClose} aria-label="Close">
          &times;
        </button>

        <h3 className={styles.title}>Add to Backlog</h3>
        <p className={styles.activityName}>{activity.title}</p>

        {errorMessage && (
          <div className={styles.error}>{errorMessage}</div>
        )}

        {grouped.length === 0 ? (
          <p className={styles.empty}>No available backlogs.</p>
        ) : (
          grouped.map((group) => (
            <div key={group.type}>
              <p className={styles.groupLabel}>{group.label}</p>
              <ul className={styles.containerList}>
                {group.containers.map((container) => {
                  const alreadyIn = activityContainerIds.has(container.id);
                  const isCurrent = container.id === currentContainerId;

                  return (
                    <li key={container.id} className={styles.containerItem}>
                      <button
                        className={`${styles.containerButton} ${alreadyIn ? styles.alreadyIn : ''}`}
                        onClick={() => handleMove(container.id)}
                        disabled={moving || alreadyIn}
                        title={alreadyIn ? 'Already in this backlog' : undefined}
                      >
                        <span className={styles.containerDate}>
                          {new Date(container.startDate).toLocaleDateString('en-GB')}
                        </span>
                        {isCurrent && (
                          <span className={styles.currentTag}>current</span>
                        )}
                        {alreadyIn && (
                          <span className={styles.checkmark}>&#10003;</span>
                        )}
                      </button>
                    </li>
                  );
                })}
              </ul>
            </div>
          ))
        )}
      </div>
    </div>
  );
}
