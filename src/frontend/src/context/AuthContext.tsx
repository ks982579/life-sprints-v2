import { createContext, useContext, useState, useEffect, type ReactNode } from 'react';
import { type User, type AuthContextType } from '../types/auth';
import { authService } from '../services/authService';

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider = ({ children }: { children: ReactNode }) => {
    const [user, setUser] = useState<User | null>(null);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        // Check if user is already authenticated on mount
        authService
            .getCurrentUser()
            .then((currentUser) => {
                setUser(currentUser);
            })
            .catch((error) => {
                console.error('Failed to get current user:', error);
                setUser(null);
            })
            .finally(() => {
                setLoading(false);
            });
    }, []);

    const login = () => {
        authService.login();
    };

    const logout = async () => {
        try {
            await authService.logout();
            setUser(null);
        } catch (error) {
            console.error('Logout failed:', error);
        }
    };

    return (
        <AuthContext.Provider value={{ user, loading, login, logout }}>
            {children}
        </AuthContext.Provider>
    );
};

export const useAuth = (): AuthContextType => {
    const context = useContext(AuthContext);
    if (context === undefined) {
        throw new Error('useAuth must be used within an AuthProvider');
    }
    return context;
};
