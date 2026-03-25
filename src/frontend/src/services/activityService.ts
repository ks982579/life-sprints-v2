import { api } from './api';
import { type Activity, type CreateActivityDto, type UpdateActivityDto, type ContainerType } from '../types';

export const activityService = {
  // Get all activities for the current user, optionally filtered by container type or specific container ID.
  // When containerId is provided it takes precedence over containerType.
  getActivities: async (containerType?: ContainerType, containerId?: number): Promise<Activity[]> => {
    const params = new URLSearchParams();
    if (containerId !== undefined) {
      params.set('containerId', String(containerId));
    } else if (containerType !== undefined) {
      params.set('containerType', String(containerType));
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

  // Add an activity to an additional container (move/copy workflow)
  addToContainer: async (activityId: number, containerId: number): Promise<void> => {
    return api.post(`/activities/${activityId}/containers/${containerId}`, {});
  },

  // Remove an activity from a container
  removeFromContainer: async (activityId: number, containerId: number): Promise<void> => {
    return api.delete(`/activities/${activityId}/containers/${containerId}`);
  },
};
