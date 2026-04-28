import { useState, useEffect, useCallback } from 'react';
import {
  type Activity,
  type Container,
  type ContainerType,
  type CreateActivityDto,
  type UpdateActivityDto,
} from '../types';
import { activityService } from '../services/activityService';
import { containerService } from '../services/containerService';

export interface UseBacklogResult {
  activities: Activity[];
  loading: boolean;
  error: string | null;
  containers: Container[];
  selectedContainerId: number | undefined;
  setSelectedContainerId: (id: number | undefined) => void;
  reload: () => Promise<void>;
  reloadContainers: () => Promise<void>;
  handleCreate: (data: CreateActivityDto) => Promise<void>;
  handleUpdate: (id: number, data: UpdateActivityDto) => Promise<void>;
  handleDelete: (id: number) => Promise<void>;
  handleToggle: (activityId: number, containerId: number, isCompleted: boolean) => Promise<void>;
}

/**
 * Custom hook for managing a single backlog's activities and containers.
 *
 * - Loads all containers of the given type for date navigation.
 * - Loads activities filtered by containerId (when selected) or containerType (default).
 * - Exposes CRUD callbacks that reload activities after each mutation.
 *
 * Related files:
 * - Pages: src/frontend/src/pages/AnnualBacklog.tsx (and siblings)
 * - Services: src/frontend/src/services/activityService.ts, containerService.ts
 */
export function useBacklog(containerType: ContainerType): UseBacklogResult {
  const [activities, setActivities] = useState<Activity[]>([]);
  const [containers, setContainers] = useState<Container[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [selectedContainerId, setSelectedContainerId] = useState<number | undefined>();

  const loadContainers = useCallback(async () => {
    try {
      const data = await containerService.getContainers(containerType);
      setContainers(data);
    } catch (err) {
      console.error('Failed to load containers:', err);
    }
  }, [containerType]);

  const loadActivities = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await activityService.getActivities(containerType, selectedContainerId);
      setActivities(data);
    } catch (err) {
      setError('Failed to load activities. Please try again.');
      console.error('Error loading activities:', err);
    } finally {
      setLoading(false);
    }
  }, [containerType, selectedContainerId]);

  useEffect(() => {
    loadContainers();
  }, [loadContainers]);

  useEffect(() => {
    loadActivities();
  }, [loadActivities]);

  const reload = useCallback(async () => {
    await loadActivities();
  }, [loadActivities]);

  const handleCreate = useCallback(
    async (data: CreateActivityDto) => {
      await activityService.createActivity(data);
      await loadActivities();
    },
    [loadActivities]
  );

  const handleUpdate = useCallback(
    async (id: number, data: UpdateActivityDto) => {
      await activityService.updateActivity(id, data);
      await loadActivities();
    },
    [loadActivities]
  );

  const handleDelete = useCallback(
    async (id: number) => {
      await activityService.deleteActivity(id);
      await loadActivities();
    },
    [loadActivities]
  );

  const handleToggle = useCallback(
    async (activityId: number, containerId: number, isCompleted: boolean) => {
      await activityService.toggleCompletion(activityId, containerId, isCompleted);
      await loadActivities();
    },
    [loadActivities]
  );

  return {
    activities,
    loading,
    error,
    containers,
    selectedContainerId,
    setSelectedContainerId,
    reload,
    reloadContainers: loadContainers,
    handleCreate,
    handleUpdate,
    handleDelete,
    handleToggle,
  };
}
