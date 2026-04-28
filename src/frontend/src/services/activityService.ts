import { api } from './api';
import { type Activity, type CreateActivityDto, type UpdateActivityDto, type ContainerType, type RecurrenceType } from '../types';

interface GetActivitiesOptions {
  containerType?: ContainerType;
  containerId?: number;
  isRecurring?: boolean;
  recurrenceType?: RecurrenceType;
}

export const activityService = {
  // Get all activities for the current user, optionally filtered by container type, container ID, or recurring flags.
  // When containerId is provided it takes precedence over containerType.
  getActivities: async (containerTypeOrOptions?: ContainerType | GetActivitiesOptions, containerId?: number): Promise<Activity[]> => {
    const params = new URLSearchParams();
    let opts: GetActivitiesOptions = {};

    if (typeof containerTypeOrOptions === 'object' && containerTypeOrOptions !== null) {
      opts = containerTypeOrOptions;
    } else if (containerTypeOrOptions !== undefined) {
      opts = { containerType: containerTypeOrOptions, containerId };
    }

    if (opts.containerId !== undefined) {
      params.set('containerId', String(opts.containerId));
    } else if (opts.containerType !== undefined) {
      params.set('containerType', String(opts.containerType));
    }
    if (opts.isRecurring !== undefined) {
      params.set('isRecurring', String(opts.isRecurring));
    }
    if (opts.recurrenceType !== undefined) {
      params.set('recurrenceType', String(opts.recurrenceType));
    }
    const qs = params.toString();
    return api.get<Activity[]>(qs ? `/activities?${qs}` : '/activities');
  },

  // Get a single activity by ID
  getActivity: async (id: number): Promise<Activity> => {
    return api.get<Activity>(`/activities/${id}`);
  },

  // Create a new activity
  createActivity: async (dto: CreateActivityDto): Promise<Activity> => {
    return api.post<Activity>('/activities', dto);
  },

  // Update an existing activity
  updateActivity: async (id: number, dto: UpdateActivityDto): Promise<Activity> => {
    return api.put<Activity>(`/activities/${id}`, dto);
  },

  // Toggle completion status of an activity in a container
  toggleCompletion: async (id: number, containerId: number, isCompleted: boolean): Promise<Activity> => {
    return api.patch<Activity>(`/activities/${id}/complete`, { containerId, isCompleted });
  },

  // Delete (archive) an activity
  deleteActivity: async (id: number): Promise<void> => {
    return api.delete(`/activities/${id}`);
  },

  // Reorder an activity within a container (direction: 'up' | 'down')
  reorderActivity: async (activityId: number, containerId: number, direction: 'up' | 'down'): Promise<void> => {
    return api.patch<void>(`/activities/${activityId}/reorder`, { containerId, direction });
  },

  // Add an activity to an additional container (move/copy workflow)
  addToContainer: async (activityId: number, containerId: number): Promise<void> => {
    return api.post(`/activities/${activityId}/containers/${containerId}`, {});
  },

  // Remove an activity from a container
  removeFromContainer: async (activityId: number, containerId: number): Promise<void> => {
    return api.delete(`/activities/${activityId}/containers/${containerId}`);
  },
};
