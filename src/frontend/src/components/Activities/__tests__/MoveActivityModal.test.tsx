import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MoveActivityModal } from '../MoveActivityModal';
import { ActivityType, ContainerType, ContainerStatus, RecurrenceType, type Activity, type Container } from '../../../types';

function makeActivity(overrides: Partial<Activity> = {}): Activity {
  return {
    id: 1,
    userId: 'user1',
    title: 'Test Activity',
    type: ActivityType.Task,
    isRecurring: false,
    recurrenceType: RecurrenceType.None,
    createdAt: '2026-01-01T00:00:00Z',
    containers: [],
    children: [],
    ...overrides,
  };
}

function makeContainer(overrides: Partial<Container> & { id: number }): Container {
  return {
    userId: 'user1',
    type: ContainerType.Annual,
    startDate: '2026-01-01T00:00:00Z',
    status: ContainerStatus.Active,
    createdAt: '2026-01-01T00:00:00Z',
    totalActivities: 0,
    completedActivities: 0,
    ...overrides,
  };
}

const containers: Container[] = [
  makeContainer({ id: 1, type: ContainerType.Annual, startDate: '2026-01-01T00:00:00Z' }),
  makeContainer({ id: 2, type: ContainerType.Monthly, startDate: '2026-03-01T00:00:00Z' }),
];

describe('MoveActivityModal', () => {
  it('renders the activity title', () => {
    render(
      <MoveActivityModal
        activity={makeActivity({ title: 'My Task' })}
        currentContainerId={null}
        availableContainers={containers}
        onMove={vi.fn()}
        onClose={vi.fn()}
      />
    );
    expect(screen.getByText('My Task')).toBeInTheDocument();
  });

  it('renders a button for each available container', () => {
    render(
      <MoveActivityModal
        activity={makeActivity()}
        currentContainerId={null}
        availableContainers={containers}
        onMove={vi.fn()}
        onClose={vi.fn()}
      />
    );
    expect(screen.getByText('Annual')).toBeInTheDocument();
    expect(screen.getByText('Monthly')).toBeInTheDocument();
  });

  it('calls onMove with the correct containerId when a container is clicked', async () => {
    const user = userEvent.setup();
    const onMove = vi.fn().mockResolvedValue(undefined);

    render(
      <MoveActivityModal
        activity={makeActivity()}
        currentContainerId={null}
        availableContainers={[makeContainer({ id: 5, type: ContainerType.Weekly, startDate: '2026-03-23T00:00:00Z' })]}
        onMove={onMove}
        onClose={vi.fn()}
      />
    );

    await user.click(screen.getByText('Weekly'));

    expect(onMove).toHaveBeenCalledWith(5);
  });

  it('disables containers the activity is already in', () => {
    const activity = makeActivity({
      containers: [{ containerId: 1, containerType: ContainerType.Annual, addedAt: '2026-01-01T00:00:00Z', order: 1, isRolledOver: false }],
    });

    render(
      <MoveActivityModal
        activity={activity}
        currentContainerId={null}
        availableContainers={containers}
        onMove={vi.fn()}
        onClose={vi.fn()}
      />
    );

    // Find the Annual container button — it should be disabled
    const buttons = screen.getAllByRole('button');
    const annualButton = buttons.find((b) => b.textContent?.includes('Annual'));
    expect(annualButton).toBeDisabled();
  });

  it('calls onClose when the close button is clicked', async () => {
    const user = userEvent.setup();
    const onClose = vi.fn();

    render(
      <MoveActivityModal
        activity={makeActivity()}
        currentContainerId={null}
        availableContainers={containers}
        onMove={vi.fn()}
        onClose={onClose}
      />
    );

    await user.click(screen.getByLabelText('Close'));

    expect(onClose).toHaveBeenCalledOnce();
  });
});
