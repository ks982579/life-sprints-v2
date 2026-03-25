import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { ActivityDetailModal } from '../ActivityDetailModal';
import { ActivityType, RecurrenceType, type Activity } from '../../../types';

function makeActivity(overrides: Partial<Activity> = {}): Activity {
  return {
    id: 1,
    userId: 'user1',
    title: 'Test Activity',
    description: undefined,
    type: ActivityType.Task,
    parentActivityId: undefined,
    parentActivityTitle: undefined,
    isRecurring: false,
    recurrenceType: RecurrenceType.None,
    createdAt: '2026-01-01T00:00:00Z',
    archivedAt: undefined,
    containers: [],
    children: [],
    ...overrides,
  };
}

describe('ActivityDetailModal', () => {
  it('renders nothing when activity is null', () => {
    const { container } = render(
      <ActivityDetailModal activity={null} onClose={vi.fn()} />
    );
    expect(container.firstChild).toBeNull();
  });

  it('renders the activity title', () => {
    render(<ActivityDetailModal activity={makeActivity({ title: 'My Epic Goal' })} onClose={vi.fn()} />);
    expect(screen.getByText('My Epic Goal')).toBeInTheDocument();
  });

  it('renders the activity description when provided', () => {
    render(
      <ActivityDetailModal
        activity={makeActivity({ description: 'This is a detailed description' })}
        onClose={vi.fn()}
      />
    );
    expect(screen.getByText('This is a detailed description')).toBeInTheDocument();
  });

  it('renders child activities', () => {
    const activity = makeActivity({
      children: [
        { id: 2, title: 'Child Task A', type: ActivityType.Task },
        { id: 3, title: 'Child Task B', type: ActivityType.Task },
      ],
    });
    render(<ActivityDetailModal activity={activity} onClose={vi.fn()} />);
    expect(screen.getByText('Child Task A')).toBeInTheDocument();
    expect(screen.getByText('Child Task B')).toBeInTheDocument();
  });

  it('calls onClose when the close button is clicked', async () => {
    const user = userEvent.setup();
    const onClose = vi.fn();
    render(<ActivityDetailModal activity={makeActivity()} onClose={onClose} />);

    await user.click(screen.getByLabelText('Close'));

    expect(onClose).toHaveBeenCalledOnce();
  });

  it('calls onClose when the backdrop is clicked', async () => {
    const user = userEvent.setup();
    const onClose = vi.fn();
    render(<ActivityDetailModal activity={makeActivity()} onClose={onClose} />);

    await user.click(screen.getByRole('dialog'));

    expect(onClose).toHaveBeenCalledOnce();
  });

  it('calls onClose when Escape key is pressed', () => {
    const onClose = vi.fn();
    render(<ActivityDetailModal activity={makeActivity()} onClose={onClose} />);

    fireEvent.keyDown(document, { key: 'Escape' });

    expect(onClose).toHaveBeenCalledOnce();
  });
});
