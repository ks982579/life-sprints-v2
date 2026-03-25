import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { DateNavigator } from '../DateNavigator';
import { ContainerType, ContainerStatus, type Container } from '../../../types';

function makeContainer(overrides: Partial<Container> & { id: number; startDate: string }): Container {
  return {
    userId: 'user1',
    type: ContainerType.Annual,
    endDate: undefined,
    status: ContainerStatus.Active,
    comments: undefined,
    createdAt: new Date().toISOString(),
    archivedAt: undefined,
    totalActivities: 0,
    completedActivities: 0,
    ...overrides,
  };
}

const annualContainers: Container[] = [
  makeContainer({ id: 3, startDate: '2026-01-01T00:00:00Z', type: ContainerType.Annual }), // most recent
  makeContainer({ id: 2, startDate: '2025-01-01T00:00:00Z', type: ContainerType.Annual }),
  makeContainer({ id: 1, startDate: '2024-01-01T00:00:00Z', type: ContainerType.Annual }), // oldest
];

describe('DateNavigator', () => {
  it('renders the date label for annual container', () => {
    render(
      <DateNavigator
        containers={annualContainers}
        selectedId={null}
        containerType={ContainerType.Annual}
        onSelect={vi.fn()}
      />
    );
    expect(screen.getByText('2026')).toBeInTheDocument();
  });

  it('disables previous button when at oldest container', () => {
    render(
      <DateNavigator
        containers={annualContainers}
        selectedId={1} // oldest (index 2)
        containerType={ContainerType.Annual}
        onSelect={vi.fn()}
      />
    );
    expect(screen.getByLabelText('Previous period')).toBeDisabled();
  });

  it('disables next button when at most recent container', () => {
    render(
      <DateNavigator
        containers={annualContainers}
        selectedId={null} // most recent (index 0)
        containerType={ContainerType.Annual}
        onSelect={vi.fn()}
      />
    );
    expect(screen.getByLabelText('Next period')).toBeDisabled();
  });

  it('calls onSelect with previous container id when previous is clicked', async () => {
    const user = userEvent.setup();
    const onSelect = vi.fn();

    render(
      <DateNavigator
        containers={annualContainers}
        selectedId={3} // most recent, index 0 → previous is index 1 (id=2)
        containerType={ContainerType.Annual}
        onSelect={onSelect}
      />
    );

    await user.click(screen.getByLabelText('Previous period'));

    expect(onSelect).toHaveBeenCalledWith(2);
  });

  it('calls onSelect with next container id when next is clicked', async () => {
    const user = userEvent.setup();
    const onSelect = vi.fn();

    render(
      <DateNavigator
        containers={annualContainers}
        selectedId={2} // index 1 → next is index 0 (id=3)
        containerType={ContainerType.Annual}
        onSelect={onSelect}
      />
    );

    await user.click(screen.getByLabelText('Next period'));

    expect(onSelect).toHaveBeenCalledWith(3);
  });

  it('formats weekly container as "Week of YYYY-MM-DD"', () => {
    // Backend weekly startDate is Monday; display Sunday = Monday - 1 day
    // startDate = 2026-03-23 (Monday) → display 2026-03-22 (Sunday)
    const weeklyContainer = makeContainer({
      id: 10,
      startDate: '2026-03-23T00:00:00Z',
      type: ContainerType.Weekly,
    });

    render(
      <DateNavigator
        containers={[weeklyContainer]}
        selectedId={null}
        containerType={ContainerType.Weekly}
        onSelect={vi.fn()}
      />
    );

    expect(screen.getByText('Week of 2026-03-22')).toBeInTheDocument();
  });

  it('renders nothing when containers list is empty', () => {
    const { container } = render(
      <DateNavigator
        containers={[]}
        selectedId={null}
        containerType={ContainerType.Annual}
        onSelect={vi.fn()}
      />
    );
    expect(container.firstChild).toBeNull();
  });
});
