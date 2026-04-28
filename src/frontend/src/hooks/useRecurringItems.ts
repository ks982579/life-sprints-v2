import { useState, useEffect, useCallback } from 'react';
import {
  type Activity,
  type CreateActivityDto,
  type UpdateActivityDto,
  type RecurrenceType,
} from '../types';
import { activityService } from '../services/activityService';

export interface UseRecurringItemsResult {
  activities: Activity[];
  loading: boolean;
  error: string | null;
  handleCreate: (data: CreateActivityDto) => Promise<void>;
  handleUpdate: (id: number, data: UpdateActivityDto) => Promise<void>;
  handleDelete: (id: number) => Promise<void>;
  reload: () => Promise<void>;
}

export function useRecurringItems(recurrenceType: RecurrenceType): UseRecurringItemsResult {
  const [activities, setActivities] = useState<Activity[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const loadActivities = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await activityService.getActivities({ isRecurring: true, recurrenceType });
      setActivities(data);
    } catch (err) {
      setError('Failed to load recurring items. Please try again.');
      console.error('Error loading recurring items:', err);
    } finally {
      setLoading(false);
    }
  }, [recurrenceType]);

  useEffect(() => {
    loadActivities();
  }, [loadActivities]);

  const handleCreate = useCallback(
    async (data: CreateActivityDto) => {
      await activityService.createActivity({ ...data, skipContainerLink: true });
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

  return {
    activities,
    loading,
    error,
    handleCreate,
    handleUpdate,
    handleDelete,
    reload: loadActivities,
  };
}
