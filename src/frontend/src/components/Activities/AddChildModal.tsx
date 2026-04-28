import { useEffect, useState } from 'react';
import { ActivityType, type Activity, type CreateActivityDto } from '../../types';
import styles from './AddChildModal.module.css';

const childTypeFor: Partial<Record<number, ActivityType>> = {
  [ActivityType.Project]: ActivityType.Epic,
  [ActivityType.Epic]: ActivityType.Story,
  [ActivityType.Story]: ActivityType.Task,
};

const activityTypeLabels: Record<number, string> = {
  [ActivityType.Project]: 'Project',
  [ActivityType.Epic]: 'Epic',
  [ActivityType.Story]: 'Story',
  [ActivityType.Task]: 'Task',
};

interface AddChildModalProps {
  parent: Activity;
  onSave: (data: CreateActivityDto) => Promise<void>;
  onClose: () => void;
}

export function AddChildModal({ parent, onSave, onClose }: AddChildModalProps) {
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [submitting, setSubmitting] = useState(false);

  const childType = childTypeFor[parent.type];

  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onClose();
    };
    document.addEventListener('keydown', handleKeyDown);
    return () => document.removeEventListener('keydown', handleKeyDown);
  }, [onClose]);

  if (!childType) return null;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!title.trim()) return;
    setSubmitting(true);
    try {
      await onSave({
        title: title.trim(),
        description: description.trim() || undefined,
        type: childType,
        parentActivityId: parent.id,
      });
      onClose();
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className={styles.overlay} onClick={onClose} role="dialog" aria-modal="true">
      <div className={styles.modal} onClick={(e) => e.stopPropagation()}>
        <button className={styles.closeButton} onClick={onClose} aria-label="Close">
          &times;
        </button>

        <h3 className={styles.title}>
          Add {activityTypeLabels[childType]} to {parent.title}
        </h3>

        <form onSubmit={handleSubmit}>
          <div className={styles.field}>
            <span className={styles.readonlyLabel}>Type</span>
            <span className={styles.readonlyValue}>{activityTypeLabels[childType]}</span>
          </div>

          <div className={styles.field}>
            <span className={styles.readonlyLabel}>Parent</span>
            <span className={styles.readonlyValue}>{parent.title}</span>
          </div>

          <div className={styles.field}>
            <label htmlFor="child-title" className={styles.inputLabel}>Title *</label>
            <input
              id="child-title"
              type="text"
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              placeholder={`Enter ${activityTypeLabels[childType].toLowerCase()} title`}
              required
              autoFocus
              className={styles.input}
            />
          </div>

          <div className={styles.field}>
            <label htmlFor="child-description" className={styles.inputLabel}>Description</label>
            <textarea
              id="child-description"
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              placeholder="Optional description"
              rows={2}
              className={styles.textarea}
            />
          </div>

          <div className={styles.actions}>
            <button type="button" className={styles.cancelButton} onClick={onClose} disabled={submitting}>
              Cancel
            </button>
            <button type="submit" className={styles.createButton} disabled={!title.trim() || submitting}>
              {submitting ? 'Saving…' : `Add ${activityTypeLabels[childType]}`}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
