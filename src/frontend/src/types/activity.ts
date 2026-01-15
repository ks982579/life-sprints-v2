export type ActivityType = 0 | 1 | 2 | 3;
export const ActivityType = {
  Project: 0 as ActivityType,
  Epic: 1 as ActivityType,
  Story: 2 as ActivityType,
  Task: 3 as ActivityType,
} as const;

export type RecurrenceType = 0 | 1 | 2 | 3 | 4;
export const RecurrenceType = {
  None: 0 as RecurrenceType,
  Daily: 1 as RecurrenceType,
  Weekly: 2 as RecurrenceType,
  Monthly: 3 as RecurrenceType,
  Annual: 4 as RecurrenceType,
} as const;

export type ContainerType = 0 | 1 | 2 | 3;
export const ContainerType = {
  Annual: 0 as ContainerType,
  Monthly: 1 as ContainerType,
  Weekly: 2 as ContainerType,
  Daily: 3 as ContainerType,
} as const;

export type ContainerStatus = 0 | 1 | 2;
export const ContainerStatus = {
  Active: 0 as ContainerStatus,
  Completed: 1 as ContainerStatus,
  Archived: 2 as ContainerStatus,
} as const;

export interface ContainerAssociation {
  containerId: number;
  containerType: ContainerType;
  addedAt: string;
  completedAt?: string;
  order: number;
  isRolledOver: boolean;
}

export interface Container {
  id: number;
  userId: string;
  type: ContainerType;
  startDate: string;
  endDate?: string;
  status: ContainerStatus;
  comments?: string;
  createdAt: string;
  archivedAt?: string;
  totalActivities: number;
  completedActivities: number;
}

export interface UpdateContainerStatusDto {
  status: ContainerStatus;
}

export interface ActivityChild {
  id: number;
  title: string;
  type: ActivityType;
}

export interface Activity {
  id: number;
  userId: string;
  title: string;
  description?: string;
  type: ActivityType;
  parentActivityId?: number;
  parentActivityTitle?: string;
  isRecurring: boolean;
  recurrenceType: RecurrenceType;
  createdAt: string;
  archivedAt?: string;
  containers: ContainerAssociation[];
  children: ActivityChild[];
}

export interface CreateActivityDto {
  title: string;
  description?: string;
  type: ActivityType;
  parentActivityId?: number;
  isRecurring?: boolean;
  recurrenceType?: RecurrenceType;
  containerId?: number;
}

export interface UpdateActivityDto {
  title?: string;
  description?: string;
  type?: ActivityType;
  parentActivityId?: number;
  isRecurring?: boolean;
  recurrenceType?: RecurrenceType;
}
