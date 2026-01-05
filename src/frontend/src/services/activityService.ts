import { api } from './api';
import { type Activity, type CreateActivityDto } from '../types';

export const activityService = {
  // Get all activities for the current user
  getActivities: async (): Promise<Activity[]> => {
    return api.get<Activity[]>('/activities');
  },

  // Get a single activity by ID
  getActivity: async (id: number): Promise<Activity> => {
    return api.get<Activity>(`/activities/${id}`);
  },

  // Create a new activity
  createActivity: async (dto: CreateActivityDto): Promise<Activity> => {
    return api.post<Activity>('/activities', dto);
  },

  // Update an existing activity (placeholder for future)
  updateActivity: async (id: number, dto: Partial<CreateActivityDto>): Promise<Activity> => {
    return api.put<Activity>(`/activities/${id}`, dto);
  },

  // Delete an activity (placeholder for future)
  deleteActivity: async (id: number): Promise<void> => {
    return api.delete(`/activities/${id}`);
  },
};
