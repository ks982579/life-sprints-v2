import { useState } from 'react';
import { useBacklog } from '../hooks/useBacklog';
import { ActivityList } from '../components/Activities/ActivityList';
import { ActivityEditor } from '../components/Activities/ActivityEditor';
import { ActivityDetailModal } from '../components/Activities/ActivityDetailModal';
import { DateNavigator } from '../components/Navigation/DateNavigator';
import { ContainerType, RecurrenceType, type Activity, type ActivityType } from '../types';
import styles from './BacklogPage.module.css';

// Note: MoveActivityModal is intentionally omitted for Daily — tasks flow
// down from Weekly into Daily, not out. Users can add items via "New Item".

export function DailyChecklist() {
  const {
    activities,
    loading,
    error,
    containers,
    selectedContainerId,
    setSelectedContainerId,
    handleCreate,
    handleUpdate,
    handleDelete,
    handleToggle,
  } = useBacklog(ContainerType.Daily);

  const [showEditor, setShowEditor] = useState(false);
  const [editingActivity, setEditingActivity] = useState<Activity | undefined>();
  const [detailActivity, setDetailActivity] = useState<Activity | null>(null);

  const handleSave = async (data: {
    title: string;
    description?: string;
    type: ActivityType;
    parentActivityId?: number;
    isRecurring: boolean;
    recurrenceType: RecurrenceType;
  }) => {
    if (editingActivity) {
      await handleUpdate(editingActivity.id, data);
    } else {
      await handleCreate({ ...data, containerId: selectedContainerId });
    }
    setShowEditor(false);
    setEditingActivity(undefined);
  };

  const handleEditClick = (activity: Activity) => {
    setEditingActivity(activity);
    setShowEditor(true);
  };

  const handleCancelEditor = () => {
    setShowEditor(false);
    setEditingActivity(undefined);
  };

  const handleNewItem = () => {
    setEditingActivity(undefined);
    setShowEditor(!showEditor);
  };

  return (
    <div className={styles.page}>
      <div className={styles.pageHeader}>
        <h2 className={styles.pageTitle}>Daily Checklist</h2>
        <button className={styles.newButton} onClick={handleNewItem}>
          {showEditor && !editingActivity ? 'Cancel' : 'New Item'}
        </button>
      </div>

      <DateNavigator
        containers={containers}
        selectedId={selectedContainerId ?? null}
        containerType={ContainerType.Daily}
        onSelect={(id) => setSelectedContainerId(id ?? undefined)}
      />

      {error && <div className={styles.error}>{error}</div>}

      {showEditor && (
        <ActivityEditor
          activities={activities}
          editingActivity={editingActivity}
          onSave={handleSave}
          onCancel={handleCancelEditor}
        />
      )}

      {loading ? (
        <div className={styles.loading}>Loading activities...</div>
      ) : (
        <ActivityList
          activities={activities}
          containerType={ContainerType.Daily}
          onActivityClick={setDetailActivity}
          onEditActivity={handleEditClick}
          onActivityDelete={handleDelete}
          onToggleCompletion={handleToggle}
        />
      )}

      {detailActivity && (
        <ActivityDetailModal
          activity={detailActivity}
          onClose={() => setDetailActivity(null)}
        />
      )}
    </div>
  );
}
