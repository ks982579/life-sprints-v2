import { api } from './api';
import { type Container, type ContainerType, type UpdateContainerStatusDto } from '../types';

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
};
