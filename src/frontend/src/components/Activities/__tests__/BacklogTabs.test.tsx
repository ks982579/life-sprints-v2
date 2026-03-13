import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { BacklogTabs } from '../BacklogTabs';
import { ContainerType } from '../../../types';

describe('BacklogTabs', () => {
  it('renders all three visible tabs', () => {
    render(<BacklogTabs activeTab={ContainerType.Annual} onTabChange={vi.fn()} />);

    expect(screen.getByText('Annual Backlog')).toBeInTheDocument();
    expect(screen.getByText('Monthly Backlog')).toBeInTheDocument();
    expect(screen.getByText('Weekly Sprint')).toBeInTheDocument();
  });

  it('marks the active tab with the active class', () => {
    render(<BacklogTabs activeTab={ContainerType.Monthly} onTabChange={vi.fn()} />);

    const monthlyButton = screen.getByText('Monthly Backlog');
    expect(monthlyButton).toHaveClass('active');

    const annualButton = screen.getByText('Annual Backlog');
    expect(annualButton).not.toHaveClass('active');
  });

  it('calls onTabChange with the correct container type when a tab is clicked', async () => {
    const user = userEvent.setup();
    const onTabChange = vi.fn();
    render(<BacklogTabs activeTab={ContainerType.Annual} onTabChange={onTabChange} />);

    await user.click(screen.getByText('Weekly Sprint'));

    expect(onTabChange).toHaveBeenCalledOnce();
    expect(onTabChange).toHaveBeenCalledWith(ContainerType.Weekly);
  });

  it('calls onTabChange with Annual when Annual Backlog tab is clicked', async () => {
    const user = userEvent.setup();
    const onTabChange = vi.fn();
    render(<BacklogTabs activeTab={ContainerType.Weekly} onTabChange={onTabChange} />);

    await user.click(screen.getByText('Annual Backlog'));

    expect(onTabChange).toHaveBeenCalledWith(ContainerType.Annual);
  });

  it('does not render the Daily Checklist tab', () => {
    render(<BacklogTabs activeTab={ContainerType.Annual} onTabChange={vi.fn()} />);

    expect(screen.queryByText('Daily Checklist')).not.toBeInTheDocument();
  });
});
