import { useState } from 'react';
import { useBacklog } from '../hooks/useBacklog';
import { ActivityList } from '../components/Activities/ActivityList';
import { ActivityEditor } from '../components/Activities/ActivityEditor';
import { ActivityDetailModal } from '../components/Activities/ActivityDetailModal';
import { MoveActivityModal } from '../components/Activities/MoveActivityModal';
import { DateNavigator } from '../components/Navigation/DateNavigator';
import { activityService } from '../services/activityService';
import { ContainerType, RecurrenceType, type Activity, type ActivityType } from '../types';
import styles from './BacklogPage.module.css';

export function WeeklySprint() {
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
    reload,
  } = useBacklog(ContainerType.Weekly);

  const [showEditor, setShowEditor] = useState(false);
  const [editingActivity, setEditingActivity] = useState<Activity | undefined>();
  const [detailActivity, setDetailActivity] = useState<Activity | null>(null);
  const [moveActivity, setMoveActivity] = useState<Activity | null>(null);

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
        <h2 className={styles.pageTitle}>Weekly Sprint</h2>
        <button className={styles.newButton} onClick={handleNewItem}>
          {showEditor && !editingActivity ? 'Cancel' : 'New Item'}
        </button>
      </div>

      <DateNavigator
        containers={containers}
        selectedId={selectedContainerId ?? null}
        containerType={ContainerType.Weekly}
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
          containerType={ContainerType.Weekly}
          onActivityClick={setDetailActivity}
          onEditActivity={handleEditClick}
          onMoveActivity={setMoveActivity}
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

      {moveActivity && (
        <MoveActivityModal
          activity={moveActivity}
          currentContainerId={selectedContainerId ?? null}
          availableContainers={containers}
          onMove={async (targetContainerId) => {
            await activityService.addToContainer(moveActivity.id, targetContainerId);
            setMoveActivity(null);
            await reload();
          }}
          onClose={() => setMoveActivity(null)}
        />
      )}
    </div>
  );
}
