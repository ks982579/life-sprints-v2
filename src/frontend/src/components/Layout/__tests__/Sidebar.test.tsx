import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { Sidebar } from '../Sidebar';

function renderSidebar(initialPath = '/annual') {
  return render(
    <MemoryRouter initialEntries={[initialPath]}>
      <Sidebar />
    </MemoryRouter>
  );
}

describe('Sidebar', () => {
  it('renders all four navigation links', () => {
    renderSidebar();
    expect(screen.getByText('Annual')).toBeInTheDocument();
    expect(screen.getByText('Monthly')).toBeInTheDocument();
    expect(screen.getByText('Weekly Sprint')).toBeInTheDocument();
    expect(screen.getByText('Daily Checklist')).toBeInTheDocument();
  });

  it('renders link to /annual', () => {
    renderSidebar();
    const link = screen.getByText('Annual').closest('a');
    expect(link).toHaveAttribute('href', '/annual');
  });

  it('renders link to /daily', () => {
    renderSidebar();
    const link = screen.getByText('Daily Checklist').closest('a');
    expect(link).toHaveAttribute('href', '/daily');
  });

  it('renders a nav element', () => {
    renderSidebar();
    expect(screen.getByRole('navigation')).toBeInTheDocument();
  });
});
