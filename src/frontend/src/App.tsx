import { useState, useEffect } from 'react';
import { AuthProvider, useAuth } from './context/AuthContext';
import { ProtectedRoute } from './components/Auth/ProtectedRoute';
import { BacklogTabs, ActivityList, ActivityEditor } from './components/Activities';
import { activityService } from './services/activityService';
import { ContainerType, ActivityType, RecurrenceType, type Activity } from './types';
import './App.css';

function Dashboard() {
  const { user, logout } = useAuth();
  const [activeTab, setActiveTab] = useState<ContainerType>(ContainerType.Annual);
  const [activities, setActivities] = useState<Activity[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showEditor, setShowEditor] = useState(false);
  const [editingActivity, setEditingActivity] = useState<Activity | undefined>();

  // Load activities on mount
  useEffect(() => {
    loadActivities();
  }, []);

  const loadActivities = async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await activityService.getActivities();
      setActivities(data);
    } catch (err) {
      setError('Failed to load activities. Please try again.');
      console.error('Error loading activities:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleSaveActivity = async (activityData: {
    title: string;
    description?: string;
    type: ActivityType;
    parentActivityId?: number;
    isRecurring: boolean;
    recurrenceType: RecurrenceType;
  }) => {
    try {
      setError(null);
      if (editingActivity) {
        // Update existing activity
        await activityService.updateActivity(editingActivity.id, activityData);
      } else {
        // Create new activity
        await activityService.createActivity(activityData);
      }
      await loadActivities();
      setShowEditor(false);
      setEditingActivity(undefined);
    } catch (err) {
      setError(editingActivity ? 'Failed to update activity. Please try again.' : 'Failed to create activity. Please try again.');
      console.error('Error saving activity:', err);
    }
  };

  const handleEditActivity = (activity: Activity) => {
    setEditingActivity(activity);
    setShowEditor(true);
  };

  const handleCancelEditor = () => {
    setShowEditor(false);
    setEditingActivity(undefined);
  };

  const handleDeleteActivity = async (activityId: number) => {
    try {
      setError(null);
      await activityService.deleteActivity(activityId);
      await loadActivities();
    } catch (err) {
      setError('Failed to delete activity. Please try again.');
      console.error('Error deleting activity:', err);
    }
  };

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
        <div className="content-header">
          <h2>Your Backlogs</h2>
          <button
            onClick={() => setShowEditor(!showEditor)}
            className="create-button"
          >
            {showEditor ? 'Cancel' : 'Create New Item'}
          </button>
        </div>

        {error && (
          <div className="error-message">
            {error}
            <button onClick={() => setError(null)} className="dismiss-button">
              ×
            </button>
          </div>
        )}

        <BacklogTabs activeTab={activeTab} onTabChange={setActiveTab} />

        {showEditor && (
          <ActivityEditor
            activities={activities}
            editingActivity={editingActivity}
            onSave={handleSaveActivity}
            onCancel={handleCancelEditor}
          />
        )}

        {loading ? (
          <div className="loading">Loading activities...</div>
        ) : (
          <ActivityList
            activities={activities}
            containerType={activeTab}
            onActivityClick={handleEditActivity}
            onActivityDelete={handleDeleteActivity}
          />
        )}
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
