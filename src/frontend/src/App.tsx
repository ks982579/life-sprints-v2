import { AuthProvider, useAuth } from './context/AuthContext';
import { ProtectedRoute } from './components/Auth/ProtectedRoute';
import './App.css';

function Dashboard() {
  const { user, logout } = useAuth();

  return (
    <div className="dashboard">
      <header className="dashboard-header">
        <h1>Life Sprint</h1>
        <div className="user-info">
          {user?.avatarUrl && (
            <img
              src={user.avatarUrl}
              alt={user.gitHubUsername}
              className="avatar"
            />
          )}
          <span>Welcome, {user?.gitHubUsername}!</span>
          <button onClick={logout} className="logout-button">
            Logout
          </button>
        </div>
      </header>

      <main className="dashboard-content">
        <h2>Your Backlogs</h2>
        <div className="backlog-grid">
          <div className="backlog-card">
            <h3>Annual Backlog</h3>
            <p>Long-term goals and projects</p>
          </div>
          <div className="backlog-card">
            <h3>Monthly Backlog</h3>
            <p>This month's objectives</p>
          </div>
          <div className="backlog-card">
            <h3>Weekly Sprint</h3>
            <p>Current week's focus</p>
          </div>
          <div className="backlog-card">
            <h3>Daily Checklist</h3>
            <p>Today's tasks</p>
          </div>
        </div>
      </main>
    </div>
  );
}

function App() {
  return (
    <AuthProvider>
      <ProtectedRoute>
        <Dashboard />
      </ProtectedRoute>
    </AuthProvider>
  );
}

export default App;
