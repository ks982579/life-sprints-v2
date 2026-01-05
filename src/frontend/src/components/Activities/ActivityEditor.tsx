import { useState, useEffect } from 'react';
import { ActivityType, RecurrenceType, type Activity } from '../../types';
import './ActivityEditor.css';

interface ActivityEditorProps {
  activities: Activity[];
  onSave: (activity: {
    title: string;
    description?: string;
    type: ActivityType;
    parentActivityId?: number;
    isRecurring: boolean;
    recurrenceType: RecurrenceType;
  }) => void;
  onCancel: () => void;
}

const activityTypeOptions = [
  { value: ActivityType.Project, label: 'Project' },
  { value: ActivityType.Epic, label: 'Epic' },
  { value: ActivityType.Story, label: 'Story' },
  { value: ActivityType.Task, label: 'Task' },
];

const recurrenceOptions = [
  { value: RecurrenceType.None, label: 'None' },
  { value: RecurrenceType.Daily, label: 'Daily' },
  { value: RecurrenceType.Weekly, label: 'Weekly' },
  { value: RecurrenceType.Monthly, label: 'Monthly' },
  { value: RecurrenceType.Annual, label: 'Annual' },
];

// Define valid parent types for each child type
const validParentTypes: Record<number, ActivityType[]> = {
  [ActivityType.Project]: [], // Projects have no parents
  [ActivityType.Epic]: [ActivityType.Project],
  [ActivityType.Story]: [ActivityType.Epic, ActivityType.Project],
  [ActivityType.Task]: [ActivityType.Story, ActivityType.Epic],
};

export function ActivityEditor({ activities, onSave, onCancel }: ActivityEditorProps) {
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [type, setType] = useState<ActivityType>(ActivityType.Task);
  const [parentActivityId, setParentActivityId] = useState<number | undefined>();
  const [isRecurring, setIsRecurring] = useState(false);
  const [recurrenceType, setRecurrenceType] = useState<RecurrenceType>(RecurrenceType.None);

  // Get potential parent activities based on selected type
  const potentialParents = activities.filter((activity) =>
    validParentTypes[type].includes(activity.type)
  );

  // Reset parent selection when type changes
  useEffect(() => {
    if (!validParentTypes[type].length) {
      setParentActivityId(undefined);
    } else if (parentActivityId) {
      const parent = activities.find((a) => a.id === parentActivityId);
      if (parent && !validParentTypes[type].includes(parent.type)) {
        setParentActivityId(undefined);
      }
    }
  }, [type, parentActivityId, activities]);

  // Update recurrence type when isRecurring changes
  useEffect(() => {
    if (!isRecurring) {
      setRecurrenceType(RecurrenceType.None);
    }
  }, [isRecurring]);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();

    if (!title.trim()) {
      return;
    }

    onSave({
      title: title.trim(),
      description: description.trim() || undefined,
      type,
      parentActivityId,
      isRecurring,
      recurrenceType: isRecurring ? recurrenceType : RecurrenceType.None,
    });

    // Reset form
    setTitle('');
    setDescription('');
    setType(ActivityType.Task);
    setParentActivityId(undefined);
    setIsRecurring(false);
    setRecurrenceType(RecurrenceType.None);
  };

  return (
    <div className="activity-editor">
      <form onSubmit={handleSubmit}>
        <div className="editor-row">
          <div className="form-group">
            <label htmlFor="activity-title">Title *</label>
            <input
              id="activity-title"
              type="text"
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              placeholder="Enter activity title"
              required
              autoFocus
            />
          </div>

          <div className="form-group">
            <label htmlFor="activity-type">Type *</label>
            <select
              id="activity-type"
              value={type}
              onChange={(e) => setType(Number(e.target.value) as ActivityType)}
              required
            >
              {activityTypeOptions.map((option) => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>
          </div>
        </div>

        <div className="form-group">
          <label htmlFor="activity-description">Description</label>
          <textarea
            id="activity-description"
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            placeholder="Optional description"
            rows={2}
          />
        </div>

        {potentialParents.length > 0 && (
          <div className="form-group">
            <label htmlFor="activity-parent">Parent Activity</label>
            <select
              id="activity-parent"
              value={parentActivityId || ''}
              onChange={(e) =>
                setParentActivityId(e.target.value ? Number(e.target.value) : undefined)
              }
            >
              <option value="">-- None --</option>
              {potentialParents.map((parent) => (
                <option key={parent.id} value={parent.id}>
                  {parent.title}
                </option>
              ))}
            </select>
          </div>
        )}

        <div className="form-group checkbox-group">
          <label>
            <input
              type="checkbox"
              checked={isRecurring}
              onChange={(e) => setIsRecurring(e.target.checked)}
            />
            <span>Recurring Activity</span>
          </label>
        </div>

        {isRecurring && (
          <div className="form-group">
            <label htmlFor="recurrence-type">Recurrence Type</label>
            <select
              id="recurrence-type"
              value={recurrenceType}
              onChange={(e) => setRecurrenceType(Number(e.target.value) as RecurrenceType)}
            >
              {recurrenceOptions
                .filter((option) => option.value !== RecurrenceType.None)
                .map((option) => (
                  <option key={option.value} value={option.value}>
                    {option.label}
                  </option>
                ))}
            </select>
          </div>
        )}

        <div className="editor-actions">
          <button type="button" onClick={onCancel} className="cancel-button">
            Cancel
          </button>
          <button type="submit" className="save-button" disabled={!title.trim()}>
            Create Activity
          </button>
        </div>
      </form>
    </div>
  );
}
