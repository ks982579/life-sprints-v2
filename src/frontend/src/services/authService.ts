import { api } from './api';
import { type User } from '../types/auth';

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost/api';

export const authService = {
    login: () => {
        // Redirect to backend OAuth flow
        window.location.href = `${API_URL}/auth/github/login`;
    },

    logout: async (): Promise<void> => {
        await api.post('/auth/logout');
    },

    getCurrentUser: async (): Promise<User | null> => {
        try {
            const user = await api.get<User>('/auth/me');
            return user;
        } catch (error) {
            // User is not authenticated
            return null;
        }
    },
};
