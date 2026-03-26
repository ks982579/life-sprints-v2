import { useEffect, useState } from 'react';
import { type ContainerType } from '../../types';
import { containerService } from '../../services/containerService';
import styles from './NewContainerModal.module.css';

interface NewContainerModalProps {
  containerType: ContainerType;
  label: string; // e.g. "Sprint", "Month", "Year"
  onCreated: () => void;
  onClose: () => void;
}

export function NewContainerModal({ containerType, label, onCreated, onClose }: NewContainerModalProps) {
  const [rollover, setRollover] = useState(true);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onClose();
    };
    document.addEventListener('keydown', handleKeyDown);
    return () => document.removeEventListener('keydown', handleKeyDown);
  }, [onClose]);

  const handleCreate = async () => {
    try {
      setLoading(true);
      setError(null);
      await containerService.createNewContainer({ type: containerType, rolloverIncomplete: rollover });
      onCreated();
    } catch (err) {
      const e = err as Error & { conflict?: boolean };
      if (e.conflict) {
        setError(e.message);
      } else {
        setError('Failed to create container. Please try again.');
      }
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className={styles.overlay} onClick={onClose} role="dialog" aria-modal="true">
      <div className={styles.modal} onClick={(e) => e.stopPropagation()}>
        <button className={styles.closeButton} onClick={onClose} aria-label="Close">
          &times;
        </button>

        <h3 className={styles.title}>Start New {label}</h3>

        {error ? (
          <div className={styles.conflict}>
            <span className={styles.conflictIcon}>&#9888;</span>
            {error}
          </div>
        ) : (
          <>
            <p className={styles.description}>
              Choose how to start the new {label.toLowerCase()}:
            </p>

            <div className={styles.options}>
              <label className={styles.option}>
                <input
                  type="radio"
                  name="rollover"
                  checked={rollover}
                  onChange={() => setRollover(true)}
                />
                <div className={styles.optionContent}>
                  <span className={styles.optionTitle}>Roll over incomplete items</span>
                  <span className={styles.optionDesc}>
                    Carry unfinished tasks from the previous {label.toLowerCase()} into this one.
                  </span>
                </div>
              </label>

              <label className={styles.option}>
                <input
                  type="radio"
                  name="rollover"
                  checked={!rollover}
                  onChange={() => setRollover(false)}
                />
                <div className={styles.optionContent}>
                  <span className={styles.optionTitle}>Start fresh</span>
                  <span className={styles.optionDesc}>
                    Begin with an empty {label.toLowerCase()}. Previous items stay in the old {label.toLowerCase()}.
                  </span>
                </div>
              </label>
            </div>

            <div className={styles.actions}>
              <button className={styles.cancelButton} onClick={onClose} disabled={loading}>
                Cancel
              </button>
              <button className={styles.createButton} onClick={handleCreate} disabled={loading}>
                {loading ? 'Creating…' : `Create ${label}`}
              </button>
            </div>
          </>
        )}
      </div>
    </div>
  );
}
