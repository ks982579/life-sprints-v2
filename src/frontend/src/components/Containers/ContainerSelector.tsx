import { useEffect, useState } from 'react';
import { containerService } from '../../services/containerService';
import { type Container, ContainerType, ContainerStatus } from '../../types';
import './ContainerSelector.css';

interface ContainerSelectorProps {
  selectedContainerId?: number;
  onSelectContainer: (container: Container) => void;
  containerType: ContainerType;
}

export function ContainerSelector({ selectedContainerId, onSelectContainer, containerType }: ContainerSelectorProps) {
  const [containers, setContainers] = useState<Container[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    loadContainers();
  }, [containerType]);

  const loadContainers = async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await containerService.getContainers(containerType);
      setContainers(data);
    } catch (err) {
      setError('Failed to load containers');
      console.error('Error loading containers:', err);
    } finally {
      setLoading(false);
    }
  };

  const formatDateRange = (container: Container): string => {
    const start = new Date(container.startDate).toLocaleDateString();
    if (container.endDate) {
      const end = new Date(container.endDate).toLocaleDateString();
      return `${start} - ${end}`;
    }
    return start;
  };

  const getStatusLabel = (status: ContainerStatus): string => {
    switch (status) {
      case ContainerStatus.Active:
        return 'Active';
      case ContainerStatus.Completed:
        return 'Completed';
      case ContainerStatus.Archived:
        return 'Archived';
      default:
        return 'Unknown';
    }
  };

  const getStatusClass = (status: ContainerStatus): string => {
    switch (status) {
      case ContainerStatus.Active:
        return 'status-active';
      case ContainerStatus.Completed:
        return 'status-completed';
      case ContainerStatus.Archived:
        return 'status-archived';
      default:
        return '';
    }
  };

  if (loading) {
    return <div className="container-selector-loading">Loading containers...</div>;
  }

  if (error) {
    return <div className="container-selector-error">{error}</div>;
  }

  if (containers.length === 0) {
    return <div className="container-selector-empty">No containers found</div>;
  }

  return (
    <div className="container-selector">
      <div className="container-list">
        {containers.map((container) => (
          <div
            key={container.id}
            className={`container-item ${container.id === selectedContainerId ? 'selected' : ''} ${getStatusClass(container.status)}`}
            onClick={() => onSelectContainer(container)}
          >
            <div className="container-header">
              <span className="container-date">{formatDateRange(container)}</span>
              <span className={`container-status ${getStatusClass(container.status)}`}>
                {getStatusLabel(container.status)}
              </span>
            </div>
            <div className="container-stats">
              <span className="stat">
                <strong>{container.completedActivities}</strong> / {container.totalActivities} completed
              </span>
              {container.totalActivities > 0 && (
                <span className="stat-percentage">
                  ({Math.round((container.completedActivities / container.totalActivities) * 100)}%)
                </span>
              )}
            </div>
            {container.comments && (
              <div className="container-comments">{container.comments}</div>
            )}
          </div>
        ))}
      </div>
    </div>
  );
}
