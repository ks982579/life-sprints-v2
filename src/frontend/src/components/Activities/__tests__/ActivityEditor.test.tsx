import { describe, it, expect, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { ActivityEditor } from '../ActivityEditor';
import { ActivityType, RecurrenceType, type Activity } from '../../../types';
import { ContainerType } from '../../../types';

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
    containers: [{ containerId: 1, containerType: ContainerType.Annual, addedAt: new Date().toISOString(), order: 1, isRolledOver: false }],
    children: [],
    ...overrides,
  };
}

describe('ActivityEditor', () => {
  it('renders the create form with empty fields by default', () => {
    render(<ActivityEditor activities={[]} onSave={vi.fn()} onCancel={vi.fn()} />);

    expect(screen.getByLabelText('Title *')).toHaveValue('');
    expect(screen.getByRole('button', { name: 'Create Activity' })).toBeInTheDocument();
  });

  it('renders the update form pre-populated when editingActivity is provided', () => {
    const activity = makeActivity({ id: 1, title: 'Existing Task', description: 'Existing desc' });

    render(
      <ActivityEditor
        activities={[]}
        editingActivity={activity}
        onSave={vi.fn()}
        onCancel={vi.fn()}
      />
    );

    expect(screen.getByLabelText('Title *')).toHaveValue('Existing Task');
    expect(screen.getByLabelText('Description')).toHaveValue('Existing desc');
    expect(screen.getByRole('button', { name: 'Update Activity' })).toBeInTheDocument();
  });

  it('disables the save button when the title is empty', () => {
    render(<ActivityEditor activities={[]} onSave={vi.fn()} onCancel={vi.fn()} />);

    expect(screen.getByRole('button', { name: 'Create Activity' })).toBeDisabled();
  });

  it('enables the save button when a title is entered', async () => {
    const user = userEvent.setup();
    render(<ActivityEditor activities={[]} onSave={vi.fn()} onCancel={vi.fn()} />);

    await user.type(screen.getByLabelText('Title *'), 'My New Task');

    expect(screen.getByRole('button', { name: 'Create Activity' })).toBeEnabled();
  });

  it('calls onSave with correct data when form is submitted', async () => {
    const user = userEvent.setup();
    const onSave = vi.fn();
    render(<ActivityEditor activities={[]} onSave={onSave} onCancel={vi.fn()} />);

    await user.type(screen.getByLabelText('Title *'), 'New Task');
    await user.type(screen.getByLabelText('Description'), 'Task description');
    await user.click(screen.getByRole('button', { name: 'Create Activity' }));

    expect(onSave).toHaveBeenCalledOnce();
    expect(onSave).toHaveBeenCalledWith(
      expect.objectContaining({
        title: 'New Task',
        description: 'Task description',
        type: ActivityType.Task,
        isRecurring: false,
        recurrenceType: RecurrenceType.None,
      })
    );
  });

  it('trims whitespace from title before calling onSave', async () => {
    const user = userEvent.setup();
    const onSave = vi.fn();
    render(<ActivityEditor activities={[]} onSave={onSave} onCancel={vi.fn()} />);

    await user.type(screen.getByLabelText('Title *'), '  Trimmed Title  ');
    await user.click(screen.getByRole('button', { name: 'Create Activity' }));

    expect(onSave).toHaveBeenCalledWith(
      expect.objectContaining({ title: 'Trimmed Title' })
    );
  });

  it('calls onCancel when the Cancel button is clicked', async () => {
    const user = userEvent.setup();
    const onCancel = vi.fn();
    render(<ActivityEditor activities={[]} onSave={vi.fn()} onCancel={onCancel} />);

    await user.click(screen.getByRole('button', { name: 'Cancel' }));

    expect(onCancel).toHaveBeenCalledOnce();
  });

  it('does not submit when title is whitespace only', async () => {
    const user = userEvent.setup();
    const onSave = vi.fn();
    render(<ActivityEditor activities={[]} onSave={onSave} onCancel={vi.fn()} />);

    await user.type(screen.getByLabelText('Title *'), '   ');
    // Button stays disabled for whitespace-only input
    expect(screen.getByRole('button', { name: 'Create Activity' })).toBeDisabled();
    expect(onSave).not.toHaveBeenCalled();
  });

  it('shows the recurrence type dropdown only when recurring is checked', async () => {
    const user = userEvent.setup();
    render(<ActivityEditor activities={[]} onSave={vi.fn()} onCancel={vi.fn()} />);

    // Recurrence dropdown hidden by default
    expect(screen.queryByLabelText('Recurrence Type')).not.toBeInTheDocument();

    // Check the recurring checkbox
    await user.click(screen.getByRole('checkbox'));

    // Recurrence dropdown now visible
    expect(screen.getByLabelText('Recurrence Type')).toBeInTheDocument();
  });

  it('resets recurrenceType to None when recurring is unchecked', async () => {
    const user = userEvent.setup();
    const onSave = vi.fn();
    render(<ActivityEditor activities={[]} onSave={vi.fn()} onCancel={onSave} />);

    // Enable recurring
    await user.click(screen.getByRole('checkbox'));

    // Select Weekly recurrence
    await user.selectOptions(screen.getByLabelText('Recurrence Type'), String(RecurrenceType.Weekly));

    // Disable recurring
    await user.click(screen.getByRole('checkbox'));

    // Recurrence type select is gone
    expect(screen.queryByLabelText('Recurrence Type')).not.toBeInTheDocument();
  });

  it('shows parent activity dropdown for Epics when Projects exist', async () => {
    const user = userEvent.setup();
    const project = makeActivity({ id: 10, title: 'My Project', type: ActivityType.Project });

    render(<ActivityEditor activities={[project]} onSave={vi.fn()} onCancel={vi.fn()} />);

    // Default type is Task — no parent dropdown since Tasks need Story/Epic
    expect(screen.queryByLabelText('Parent Activity')).not.toBeInTheDocument();

    // Switch to Epic type — should show parent dropdown with project
    await user.selectOptions(screen.getByLabelText('Type *'), String(ActivityType.Epic));

    expect(screen.getByLabelText('Parent Activity')).toBeInTheDocument();
    expect(screen.getByText('My Project')).toBeInTheDocument();
  });

  it('does not show parent dropdown for Projects', async () => {
    const user = userEvent.setup();
    render(<ActivityEditor activities={[]} onSave={vi.fn()} onCancel={vi.fn()} />);

    await user.selectOptions(screen.getByLabelText('Type *'), String(ActivityType.Project));

    expect(screen.queryByLabelText('Parent Activity')).not.toBeInTheDocument();
  });

  it('resets the form fields after a successful save', async () => {
    const user = userEvent.setup();
    const onSave = vi.fn();
    render(<ActivityEditor activities={[]} onSave={onSave} onCancel={vi.fn()} />);

    await user.type(screen.getByLabelText('Title *'), 'My Task');
    await user.click(screen.getByRole('button', { name: 'Create Activity' }));

    await waitFor(() => {
      expect(screen.getByLabelText('Title *')).toHaveValue('');
    });
  });

  it('calls onSave with isRecurring true and selected recurrence type', async () => {
    const user = userEvent.setup();
    const onSave = vi.fn();
    render(<ActivityEditor activities={[]} onSave={onSave} onCancel={vi.fn()} />);

    await user.type(screen.getByLabelText('Title *'), 'Weekly Standup');
    await user.click(screen.getByRole('checkbox')); // Enable recurring
    await user.selectOptions(screen.getByLabelText('Recurrence Type'), String(RecurrenceType.Weekly));
    await user.click(screen.getByRole('button', { name: 'Create Activity' }));

    expect(onSave).toHaveBeenCalledWith(
      expect.objectContaining({
        isRecurring: true,
        recurrenceType: RecurrenceType.Weekly,
      })
    );
  });
});
