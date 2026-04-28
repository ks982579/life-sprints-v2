import { useState, useEffect } from 'react';
import { useBacklog } from '../hooks/useBacklog';
import { ActivityList } from '../components/Activities/ActivityList';
import { ActivityEditor } from '../components/Activities/ActivityEditor';
import { ActivityDetailModal } from '../components/Activities/ActivityDetailModal';
import { MoveActivityModal } from '../components/Activities/MoveActivityModal';
import { NewContainerModal } from '../components/Activities/NewContainerModal';
import { AddChildModal } from '../components/Activities/AddChildModal';
import { DateNavigator } from '../components/Navigation/DateNavigator';
import { activityService } from '../services/activityService';
import { containerService } from '../services/containerService';
import { ContainerType, RecurrenceType, type Activity, type ActivityType, type Container } from '../types';
import styles from './BacklogPage.module.css';

export function MonthlyBacklog() {
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
    reloadContainers,
  } = useBacklog(ContainerType.Monthly);

  const [showEditor, setShowEditor] = useState(false);
  const [editingActivity, setEditingActivity] = useState<Activity | undefined>();
  const [detailActivity, setDetailActivity] = useState<Activity | null>(null);
  const [moveActivity, setMoveActivity] = useState<Activity | null>(null);
  const [showNewContainer, setShowNewContainer] = useState(false);
  const [allContainers, setAllContainers] = useState<Container[]>([]);

  useEffect(() => {
    containerService.getContainers().then(setAllContainers).catch(console.error);
  }, []);

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
      await handleCreate({ ...data, containerId: selectedContainerId, defaultContainerType: ContainerType.Monthly });
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

  const handleReorder = async (activityId: number, containerId: number, direction: 'up' | 'down') => {
    await activityService.reorderActivity(activityId, containerId, direction);
    await reload();
  };

  const [addChildParent, setAddChildParent] = useState<Activity | null>(null);

  const handleContainerCreated = async (newContainer: Container) => {
    setShowNewContainer(false);
    await reloadContainers();
    await reload();
    setSelectedContainerId(newContainer.id);
    const updated = await containerService.getContainers();
    setAllContainers(updated);
  };

  return (
    <div className={styles.page}>
      <div className={styles.pageHeader}>
        <h2 className={styles.pageTitle}>Monthly Backlog</h2>
        <div className={styles.headerButtons}>
          <button className={styles.secondaryButton} onClick={() => setShowNewContainer(true)}>
            New Month
          </button>
          <button className={styles.newButton} onClick={handleNewItem}>
            {showEditor && !editingActivity ? 'Cancel' : 'New Item'}
          </button>
        </div>
      </div>

      <DateNavigator
        containers={containers}
        selectedId={selectedContainerId ?? null}
        containerType={ContainerType.Monthly}
        onSelect={(id) => setSelectedContainerId(id ?? undefined)}
      />

      {error && <div className={styles.error}>{error}</div>}

      {showEditor && (
        <ActivityEditor
          activities={activities}
          editingActivity={editingActivity}
          onSave={handleSave}
          onCancel={handleCancelEditor}
          hideRecurring
        />
      )}

      {loading ? (
        <div className={styles.loading}>Loading activities...</div>
      ) : (
        <ActivityList
          activities={activities}
          containerType={ContainerType.Monthly}
          onActivityClick={setDetailActivity}
          onEditActivity={handleEditClick}
          onMoveActivity={setMoveActivity}
          onActivityDelete={handleDelete}
          onToggleCompletion={handleToggle}
          onReorder={handleReorder}
          onAddChild={setAddChildParent}
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
          availableContainers={allContainers}
          onMove={async (targetContainerId) => {
            await activityService.addToContainer(moveActivity.id, targetContainerId);
            setMoveActivity(null);
            await reload();
          }}
          onClose={() => setMoveActivity(null)}
        />
      )}

      {showNewContainer && (
        <NewContainerModal
          containerType={ContainerType.Monthly}
          label="Month"
          onCreated={handleContainerCreated}
          onClose={() => setShowNewContainer(false)}
        />
      )}

      {addChildParent && (
        <AddChildModal
          parent={addChildParent}
          onSave={async (data) => {
            await handleCreate({ ...data, containerId: selectedContainerId, defaultContainerType: ContainerType.Monthly });
          }}
          onClose={() => setAddChildParent(null)}
        />
      )}
    </div>
  );
}
