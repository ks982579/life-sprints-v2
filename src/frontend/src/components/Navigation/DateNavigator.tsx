import { ContainerType, type Container } from '../../types';
import styles from './DateNavigator.module.css';

interface DateNavigatorProps {
  containers: Container[];
  selectedId: number | null;
  containerType: ContainerType;
  onSelect: (id: number | null) => void;
}

/**
 * Formats a container's date range into a human-readable label.
 * - Annual: "2026"
 * - Monthly: "March 2026"
 * - Weekly: "Week of 2026-03-29" (Sunday start; startDate is Monday, so display Sunday = startDate - 1 day)
 * - Daily: "2026-03-25"
 */
function formatLabel(container: Container, containerType: ContainerType): string {
  const startDate = new Date(container.startDate);

  switch (containerType) {
    case ContainerType.Annual:
      return String(startDate.getUTCFullYear());

    case ContainerType.Monthly: {
      const month = startDate.toLocaleString('en-GB', { month: 'long', timeZone: 'UTC' });
      return `${month} ${startDate.getUTCFullYear()}`;
    }

    case ContainerType.Weekly: {
      // Backend uses ISO 8601 weeks (Mon–Sun). Display the Sunday before the Monday start.
      const sunday = new Date(startDate);
      sunday.setUTCDate(startDate.getUTCDate() - 1);
      const y = sunday.getUTCFullYear();
      const m = String(sunday.getUTCMonth() + 1).padStart(2, '0');
      const d = String(sunday.getUTCDate()).padStart(2, '0');
      return `Week of ${y}-${m}-${d}`;
    }

    case ContainerType.Daily: {
      const y = startDate.getUTCFullYear();
      const m = String(startDate.getUTCMonth() + 1).padStart(2, '0');
      const d = String(startDate.getUTCDate()).padStart(2, '0');
      return `${y}-${m}-${d}`;
    }

    default:
      return container.startDate;
  }
}

/**
 * DateNavigator allows navigating between historical containers (backlogs/sprints).
 * Containers are ordered newest-first from the API.
 * "Previous" = older container (higher index). "Next" = newer container (lower index).
 * When selectedId is null, the most recent container (index 0) is shown.
 */
export function DateNavigator({ containers, selectedId, containerType, onSelect }: DateNavigatorProps) {
  if (containers.length === 0) {
    return null;
  }

  const currentIndex = selectedId !== null
    ? containers.findIndex((c) => c.id === selectedId)
    : 0;

  const effectiveIndex = currentIndex === -1 ? 0 : currentIndex;
  const currentContainer = containers[effectiveIndex];

  const hasPrevious = effectiveIndex < containers.length - 1;
  const hasNext = effectiveIndex > 0;

  const handlePrevious = () => {
    if (hasPrevious) {
      onSelect(containers[effectiveIndex + 1].id);
    }
  };

  const handleNext = () => {
    if (hasNext) {
      onSelect(containers[effectiveIndex - 1].id);
    }
  };

  const handleCurrent = () => {
    onSelect(null);
  };

  const label = formatLabel(currentContainer, containerType);
  const isViewingCurrent = effectiveIndex === 0;

  return (
    <div className={styles.navigator}>
      <button
        className={styles.arrowButton}
        onClick={handlePrevious}
        disabled={!hasPrevious}
        title="Previous period"
        aria-label="Previous period"
      >
        &#8249;
      </button>

      <div className={styles.labelWrapper}>
        <span className={styles.label}>{label}</span>
        {!isViewingCurrent && (
          <button className={styles.currentButton} onClick={handleCurrent}>
            Jump to current
          </button>
        )}
      </div>

      <button
        className={styles.arrowButton}
        onClick={handleNext}
        disabled={!hasNext}
        title="Next period"
        aria-label="Next period"
      >
        &#8250;
      </button>
    </div>
  );
}
