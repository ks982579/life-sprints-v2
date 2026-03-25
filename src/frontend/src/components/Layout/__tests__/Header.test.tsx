import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Header } from '../Header';
import { type AuthContextType } from '../../../types/auth';
import { AuthContext } from '../../../context/AuthContext';

function renderHeader(contextValue: Partial<AuthContextType> = {}) {
  const defaultContext: AuthContextType = {
    user: null,
    loading: false,
    login: vi.fn(),
    logout: vi.fn(),
    ...contextValue,
  };

  return render(
    <AuthContext.Provider value={defaultContext}>
      <Header />
    </AuthContext.Provider>
  );
}

describe('Header', () => {
  it('renders the app title', () => {
    renderHeader();
    expect(screen.getByText('Life Sprint')).toBeInTheDocument();
  });

  it('renders the username when authenticated', () => {
    renderHeader({
      user: {
        id: '1',
        gitHubUsername: 'testuser',
        avatarUrl: undefined,
      },
    });
    expect(screen.getByText('testuser')).toBeInTheDocument();
  });

  it('renders the logout button', () => {
    renderHeader();
    expect(screen.getByText('Logout')).toBeInTheDocument();
  });

  it('calls logout when logout button is clicked', async () => {
    const user = userEvent.setup();
    const logout = vi.fn();
    renderHeader({ logout });

    await user.click(screen.getByText('Logout'));

    expect(logout).toHaveBeenCalledOnce();
  });
});
