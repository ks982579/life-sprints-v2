import { describe, it, expect, vi, beforeEach } from 'vitest';
import { activityService } from '../activityService';
import { ActivityType, ContainerType, RecurrenceType, type Activity } from '../../types';

// Mock the api module
vi.mock('../api', () => ({
  api: {
    get: vi.fn(),
    post: vi.fn(),
    put: vi.fn(),
    patch: vi.fn(),
    delete: vi.fn(),
  },
}));

import { api } from '../api';

function makeActivity(id: number, title: string): Activity {
  return {
    id,
    userId: 'user1',
    title,
    type: ActivityType.Task,
    isRecurring: false,
    recurrenceType: RecurrenceType.None,
    createdAt: new Date().toISOString(),
    containers: [],
    children: [],
  };
}

describe('activityService', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('getActivities', () => {
    it('calls GET /activities without a query param when no containerType provided', async () => {
      const expected = [makeActivity(1, 'Task A')];
      vi.mocked(api.get).mockResolvedValue(expected);

      const result = await activityService.getActivities();

      expect(api.get).toHaveBeenCalledWith('/activities');
      expect(result).toEqual(expected);
    });

    it('calls GET /activities with containerType query param when provided', async () => {
      const expected = [makeActivity(2, 'Weekly Task')];
      vi.mocked(api.get).mockResolvedValue(expected);

      const result = await activityService.getActivities(ContainerType.Weekly);

      expect(api.get).toHaveBeenCalledWith('/activities?containerType=2');
      expect(result).toEqual(expected);
    });

    it('calls GET /activities with Annual (0) containerType', async () => {
      vi.mocked(api.get).mockResolvedValue([]);

      await activityService.getActivities(ContainerType.Annual);

      expect(api.get).toHaveBeenCalledWith('/activities?containerType=0');
    });
  });

  describe('getActivity', () => {
    it('calls GET /activities/:id', async () => {
      const expected = makeActivity(1, 'Single Task');
      vi.mocked(api.get).mockResolvedValue(expected);

      const result = await activityService.getActivity(1);

      expect(api.get).toHaveBeenCalledWith('/activities/1');
      expect(result).toEqual(expected);
    });
  });

  describe('createActivity', () => {
    it('calls POST /activities with the dto', async () => {
      const dto = { title: 'New Task', type: ActivityType.Task };
      const expected = makeActivity(1, 'New Task');
      vi.mocked(api.post).mockResolvedValue(expected);

      const result = await activityService.createActivity(dto);

      expect(api.post).toHaveBeenCalledWith('/activities', dto);
      expect(result).toEqual(expected);
    });
  });

  describe('updateActivity', () => {
    it('calls PUT /activities/:id with the dto', async () => {
      const dto = { title: 'Updated Title' };
      const expected = makeActivity(1, 'Updated Title');
      vi.mocked(api.put).mockResolvedValue(expected);

      const result = await activityService.updateActivity(1, dto);

      expect(api.put).toHaveBeenCalledWith('/activities/1', dto);
      expect(result).toEqual(expected);
    });
  });

  describe('toggleCompletion', () => {
    it('calls PATCH /activities/:id/complete with containerId and isCompleted', async () => {
      const expected = makeActivity(1, 'Task');
      vi.mocked(api.patch).mockResolvedValue(expected);

      const result = await activityService.toggleCompletion(1, 5, true);

      expect(api.patch).toHaveBeenCalledWith('/activities/1/complete', {
        containerId: 5,
        isCompleted: true,
      });
      expect(result).toEqual(expected);
    });
  });

  describe('deleteActivity', () => {
    it('calls DELETE /activities/:id', async () => {
      vi.mocked(api.delete).mockResolvedValue(undefined);

      await activityService.deleteActivity(1);

      expect(api.delete).toHaveBeenCalledWith('/activities/1');
    });
  });
});
