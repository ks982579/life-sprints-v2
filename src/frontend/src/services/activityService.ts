import { api } from './api';
import { type Activity, type CreateActivityDto, type UpdateActivityDto } from '../types';

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

  // Update an existing activity
  updateActivity: async (id: number, dto: UpdateActivityDto): Promise<Activity> => {
    return api.put<Activity>(`/activities/${id}`, dto);
  },

  // Delete (archive) an activity
  deleteActivity: async (id: number): Promise<void> => {
    return api.delete(`/activities/${id}`);
  },
};
