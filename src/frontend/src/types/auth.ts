export interface User {
  id: string;
  gitHubUsername: string;
  email?: string;
  avatarUrl?: string;
}

export interface AuthContextType {
  user: User | null;
  loading: boolean;
  login: () => void;
  logout: () => Promise<void>;
}
