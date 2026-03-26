import { api } from './api';
import { type Container, type ContainerType, type UpdateContainerStatusDto } from '../types';

export interface CreateNewContainerDto {
  type: ContainerType;
  rolloverIncomplete: boolean;
}

export const containerService = {
  // Get all containers for the current user
  getContainers: async (type?: ContainerType): Promise<Container[]> => {
    const queryParam = type !== undefined ? `?type=${type}` : '';
    return api.get<Container[]>(`/containers${queryParam}`);
  },

  // Get a single container by ID
  getContainer: async (id: number): Promise<Container> => {
    return api.get<Container>(`/containers/${id}`);
  },

  // Update container status
  updateContainerStatus: async (id: number, dto: UpdateContainerStatusDto): Promise<Container> => {
    return api.patch<Container>(`/containers/${id}/status`, dto);
  },

  // Create a new container for the current period.
  // Throws { conflict: true, message: string } if a container already exists for this period.
  createNewContainer: async (dto: CreateNewContainerDto): Promise<Container> => {
    const API_URL = import.meta.env.VITE_API_URL || 'http://localhost/api';
    const response = await fetch(`${API_URL}/containers/new`, {
      method: 'POST',
      credentials: 'include',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(dto),
    });
    if (response.status === 409) {
      const body = await response.json().catch(() => ({}));
      throw Object.assign(new Error(body.message ?? 'Container already exists for this period'), { conflict: true });
    }
    if (!response.ok) {
      throw new Error(`HTTP error! status: ${response.status}`);
    }
    return response.json();
  },
};
