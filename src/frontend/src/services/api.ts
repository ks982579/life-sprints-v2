const API_URL = import.meta.env.VITE_API_URL || 'http://localhost/api';

interface FetchOptions extends RequestInit {
  headers?: HeadersInit;
}

async function fetchWithCredentials(url: string, options: FetchOptions = {}) {
  const response = await fetch(`${API_URL}${url}`, {
    ...options,
    credentials: 'include', // Important for cookies
    headers: {
      'Content-Type': 'application/json',
      ...options.headers,
    },
  });

  if (!response.ok) {
    throw new Error(`HTTP error! status: ${response.status}`);
  }

  return response;
}

export const api = {
  get: async <T>(url: string): Promise<T> => {
    const response = await fetchWithCredentials(url);
    return response.json();
  },

  post: async <T>(url: string, data?: unknown): Promise<T> => {
    const response = await fetchWithCredentials(url, {
      method: 'POST',
      body: data ? JSON.stringify(data) : undefined,
    });
    return response.json();
  },

  put: async <T>(url: string, data: unknown): Promise<T> => {
    const response = await fetchWithCredentials(url, {
      method: 'PUT',
      body: JSON.stringify(data),
    });
    return response.json();
  },

  patch: async <T>(url: string, data: unknown): Promise<T> => {
    const response = await fetchWithCredentials(url, {
      method: 'PATCH',
      body: JSON.stringify(data),
    });
    return response.json();
  },

  delete: async (url: string): Promise<void> => {
    await fetchWithCredentials(url, {
      method: 'DELETE',
    });
  },
};
