import { api } from './api';
import { type Activity, type CreateActivityDto, type UpdateActivityDto, type ContainerType } from '../types';

export const activityService = {
  // Get all activities for the current user, optionally filtered by container type
  getActivities: async (containerType?: ContainerType): Promise<Activity[]> => {
    const url = containerType !== undefined
      ? `/activities?containerType=${containerType}`
      : '/activities';
    return api.get<Activity[]>(url);
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
};
