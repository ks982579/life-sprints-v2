import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { ActivityList } from '../ActivityList';
import { ActivityType, ContainerType, RecurrenceType, type Activity } from '../../../types';

// Helper to build a minimal Activity fixture
function makeActivity(overrides: Partial<Activity> & { id: number; title: string }): Activity {
  return {
    userId: 'user1',
    description: undefined,
    type: ActivityType.Task,
    parentActivityId: undefined,
    parentActivityTitle: undefined,
    isRecurring: false,
    recurrenceType: RecurrenceType.None,
    createdAt: new Date().toISOString(),
    archivedAt: undefined,
    containers: [
      {
        containerId: 1,
        containerType: ContainerType.Annual,
        addedAt: new Date().toISOString(),
        completedAt: undefined,
        order: 1,
        isRolledOver: false,
      },
    ],
    children: [],
    ...overrides,
  };
}

describe('ActivityList', () => {
  it('shows empty state when no activities match the container type', () => {
    const activities = [makeActivity({ id: 1, title: 'Annual Task' })];

    render(
      <ActivityList
        activities={activities}
        containerType={ContainerType.Weekly}
      />
    );

    expect(screen.getByText('No activities in this backlog yet.')).toBeInTheDocument();
  });

  it('renders activities that match the container type', () => {
    const activities = [
      makeActivity({ id: 1, title: 'Annual Goal' }),
      makeActivity({
        id: 2,
        title: 'Weekly Task',
        containers: [
          {
            containerId: 2,
            containerType: ContainerType.Weekly,
            addedAt: new Date().toISOString(),
            completedAt: undefined,
            order: 1,
            isRolledOver: false,
          },
        ],
      }),
    ];

    render(<ActivityList activities={activities} containerType={ContainerType.Annual} />);

    expect(screen.getByText('Annual Goal')).toBeInTheDocument();
    expect(screen.queryByText('Weekly Task')).not.toBeInTheDocument();
  });

  it('displays the correct activity type badge', () => {
    const activities = [makeActivity({ id: 1, title: 'My Project', type: ActivityType.Project })];

    render(<ActivityList activities={activities} containerType={ContainerType.Annual} />);

    expect(screen.getByText('Project')).toBeInTheDocument();
  });

  it('shows the recurring badge for recurring activities', () => {
    const activities = [
      makeActivity({ id: 1, title: 'Weekly Review', isRecurring: true, recurrenceType: RecurrenceType.Weekly }),
    ];

    render(<ActivityList activities={activities} containerType={ContainerType.Annual} />);

    expect(screen.getByText('Recurring')).toBeInTheDocument();
  });

  it('does not show the recurring badge for non-recurring activities', () => {
    const activities = [makeActivity({ id: 1, title: 'One-time Task' })];

    render(<ActivityList activities={activities} containerType={ContainerType.Annual} />);

    expect(screen.queryByText('Recurring')).not.toBeInTheDocument();
  });

  it('shows the description when provided', () => {
    const activities = [
      makeActivity({ id: 1, title: 'Task', description: 'This is the description' }),
    ];

    render(<ActivityList activities={activities} containerType={ContainerType.Annual} />);

    expect(screen.getByText('This is the description')).toBeInTheDocument();
  });

  it('shows the parent title when the activity has a parent', () => {
    const activities = [
      makeActivity({ id: 1, title: 'Child Task', parentActivityTitle: 'Parent Epic' }),
    ];

    render(<ActivityList activities={activities} containerType={ContainerType.Annual} />);

    expect(screen.getByText('Parent Epic')).toBeInTheDocument();
  });

  it('shows child count when the activity has children', () => {
    const activities = [
      makeActivity({
        id: 1,
        title: 'Epic with Children',
        children: [
          { id: 2, title: 'Child 1', type: ActivityType.Task },
          { id: 3, title: 'Child 2', type: ActivityType.Task },
        ],
      }),
    ];

    render(<ActivityList activities={activities} containerType={ContainerType.Annual} />);

    expect(screen.getByText('2')).toBeInTheDocument();
  });

  it('calls onActivityClick when an activity is clicked', async () => {
    const user = userEvent.setup();
    const onActivityClick = vi.fn();
    const activity = makeActivity({ id: 1, title: 'Clickable Task' });

    render(
      <ActivityList
        activities={[activity]}
        containerType={ContainerType.Annual}
        onActivityClick={onActivityClick}
      />
    );

    await user.click(screen.getByText('Clickable Task'));

    expect(onActivityClick).toHaveBeenCalledOnce();
    expect(onActivityClick).toHaveBeenCalledWith(activity);
  });

  it('shows the delete button when onActivityDelete is provided', () => {
    const activities = [makeActivity({ id: 1, title: 'Deletable Task' })];

    render(
      <ActivityList
        activities={activities}
        containerType={ContainerType.Annual}
        onActivityDelete={vi.fn()}
      />
    );

    expect(screen.getByText('Delete')).toBeInTheDocument();
  });

  it('does not show the delete button when onActivityDelete is not provided', () => {
    const activities = [makeActivity({ id: 1, title: 'Task' })];

    render(<ActivityList activities={activities} containerType={ContainerType.Annual} />);

    expect(screen.queryByText('Delete')).not.toBeInTheDocument();
  });

  it('renders the completion checkbox when onToggleCompletion is provided', () => {
    const activities = [makeActivity({ id: 1, title: 'Completable Task' })];

    render(
      <ActivityList
        activities={activities}
        containerType={ContainerType.Annual}
        onToggleCompletion={vi.fn()}
      />
    );

    expect(screen.getByRole('checkbox')).toBeInTheDocument();
  });

  it('shows the checkbox as checked when the activity is completed in this container', () => {
    const activities = [
      makeActivity({
        id: 1,
        title: 'Done Task',
        containers: [
          {
            containerId: 1,
            containerType: ContainerType.Annual,
            addedAt: new Date().toISOString(),
            completedAt: new Date().toISOString(), // Completed
            order: 1,
            isRolledOver: false,
          },
        ],
      }),
    ];

    render(
      <ActivityList
        activities={activities}
        containerType={ContainerType.Annual}
        onToggleCompletion={vi.fn()}
      />
    );

    expect(screen.getByRole('checkbox')).toBeChecked();
  });

  it('sorts activities by order within the container', () => {
    const activities = [
      makeActivity({
        id: 1,
        title: 'Second',
        containers: [
          {
            containerId: 1,
            containerType: ContainerType.Annual,
            addedAt: new Date().toISOString(),
            order: 2,
            isRolledOver: false,
          },
        ],
      }),
      makeActivity({
        id: 2,
        title: 'First',
        containers: [
          {
            containerId: 1,
            containerType: ContainerType.Annual,
            addedAt: new Date().toISOString(),
            order: 1,
            isRolledOver: false,
          },
        ],
      }),
    ];

    render(<ActivityList activities={activities} containerType={ContainerType.Annual} />);

    const items = screen.getAllByRole('heading', { level: 3 });
    expect(items[0]).toHaveTextContent('First');
    expect(items[1]).toHaveTextContent('Second');
  });
});
