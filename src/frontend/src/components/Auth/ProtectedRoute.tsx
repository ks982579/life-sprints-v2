import { Outlet } from 'react-router-dom';
import { useAuth } from '../../context/AuthContext';
import { LoginPage } from './LoginPage';

/**
 * Route-level auth guard. Used as a layout route element in the router.
 * Renders <Outlet /> (child routes) when authenticated, <LoginPage /> otherwise.
 *
 * Related files:
 * - Router: src/frontend/src/router/index.tsx
 * - AuthContext: src/frontend/src/context/AuthContext.tsx
 */
export const ProtectedRoute = () => {
  const { user, loading } = useAuth();

  if (loading) {
    return (
      <div style={{
        display: 'flex',
        justifyContent: 'center',
        alignItems: 'center',
        minHeight: '100vh',
      }}>
        <div>Loading...</div>
      </div>
    );
  }

  if (!user) {
    return <LoginPage />;
  }

  return <Outlet />;
};
