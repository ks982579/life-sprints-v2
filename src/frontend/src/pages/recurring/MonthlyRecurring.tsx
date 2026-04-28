import { useState } from 'react';
import { useRecurringItems } from '../../hooks/useRecurringItems';
import { ActivityList } from '../../components/Activities/ActivityList';
import { ActivityEditor } from '../../components/Activities/ActivityEditor';
import { ActivityDetailModal } from '../../components/Activities/ActivityDetailModal';
import { RecurrenceType, type Activity, type ActivityType } from '../../types';
import styles from '../BacklogPage.module.css';

export function MonthlyRecurring() {
  const { activities, loading, error, handleCreate, handleUpdate, handleDelete } = useRecurringItems(RecurrenceType.Monthly);
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
      await handleCreate({ ...data, isRecurring: true, recurrenceType: RecurrenceType.Monthly });
    }
    setShowEditor(false);
    setEditingActivity(undefined);
  };

  return (
    <div className={styles.page}>
      <div className={styles.pageHeader}>
        <h2 className={styles.pageTitle}>Monthly Recurring Items</h2>
        <button className={styles.newButton} onClick={() => { setEditingActivity(undefined); setShowEditor(!showEditor); }}>
          {showEditor && !editingActivity ? 'Cancel' : 'New Recurring Item'}
        </button>
      </div>

      {error && <div className={styles.error}>{error}</div>}

      {showEditor && (
        <ActivityEditor
          activities={activities}
          editingActivity={editingActivity}
          onSave={handleSave}
          onCancel={() => { setShowEditor(false); setEditingActivity(undefined); }}
          fixedIsRecurring={true}
          fixedRecurrenceType={RecurrenceType.Monthly}
        />
      )}

      {loading ? (
        <div className={styles.loading}>Loading recurring items...</div>
      ) : (
        <ActivityList
          activities={activities}
          onActivityClick={setDetailActivity}
          onEditActivity={(activity) => { setEditingActivity(activity); setShowEditor(true); }}
          onActivityDelete={handleDelete}
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
