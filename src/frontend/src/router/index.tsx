import { createBrowserRouter, Navigate } from 'react-router-dom';
import { MainLayout } from '../components/Layout';
import { ProtectedRoute } from '../components/Auth/ProtectedRoute';
import { LoginPage } from '../components/Auth/LoginPage';
import { AnnualBacklog, MonthlyBacklog, WeeklySprint, DailyChecklist } from '../pages';

/**
 * Application router configuration.
 *
 * Route structure:
 * /login          → LoginPage (public)
 * /               → ProtectedRoute (auth guard using Outlet)
 *   /             → MainLayout (sidebar + header + Outlet)
 *     /           → redirect to /annual
 *     /annual     → AnnualBacklog
 *     /monthly    → MonthlyBacklog
 *     /weekly     → WeeklySprint
 *     /daily      → DailyChecklist
 */
export const router = createBrowserRouter([
  {
    path: '/login',
    element: <LoginPage />,
  },
  {
    path: '/',
    element: <ProtectedRoute />,
    children: [
      {
        element: <MainLayout />,
        children: [
          { index: true, element: <Navigate to="annual" replace /> },
          { path: 'annual', element: <AnnualBacklog /> },
          { path: 'monthly', element: <MonthlyBacklog /> },
          { path: 'weekly', element: <WeeklySprint /> },
          { path: 'daily', element: <DailyChecklist /> },
        ],
      },
    ],
  },
]);
