import { test, expect } from '@playwright/test';

test.describe('Authentication', () => {
  test('should login with test user and access protected dashboard', async ({ page, context }) => {
    // Step 1: Visit the application (should show login page since not authenticated)
    await page.goto('/');

    // Step 2: Verify we're on the login page
    await expect(page.getByRole('heading', { name: 'Life Sprint' })).toBeVisible();
    await expect(page.getByRole('button', { name: /sign in with github/i })).toBeVisible();

    // Step 3: Use test-login endpoint to authenticate (bypass real GitHub OAuth)
    const response = await context.request.post('/api/auth/test-login', {
      data: {
        username: 'e2e-test-user',
        email: 'e2e@test.local',
        avatarUrl: 'https://api.dicebear.com/7.x/avataaars/svg?seed=e2e-test'
      }
    });

    expect(response.ok()).toBeTruthy();
    const userData = await response.json();
    expect(userData.gitHubUsername).toBe('e2e-test-user');

    // Step 4: Reload page - should now show dashboard (authenticated)
    await page.reload();

    // Step 5: Verify we're on the dashboard with user info
    await expect(page.getByText(/welcome, e2e-test-user/i)).toBeVisible();
    await expect(page.getByRole('button', { name: /logout/i })).toBeVisible();

    // Verify backlog cards are visible
    await expect(page.getByText(/annual backlog/i)).toBeVisible();
    await expect(page.getByText(/monthly backlog/i)).toBeVisible();
    await expect(page.getByText(/weekly sprint/i)).toBeVisible();
    await expect(page.getByText(/daily checklist/i)).toBeVisible();

    // Step 6: Test logout
    await page.getByRole('button', { name: /logout/i }).click();

    // Step 7: Verify we're back on login page
    await expect(page.getByRole('heading', { name: 'Life Sprint' })).toBeVisible();
    await expect(page.getByRole('button', { name: /sign in with github/i })).toBeVisible();
  });

  test('should show login page when not authenticated', async ({ page }) => {
    await page.goto('/');

    await expect(page.getByRole('heading', { name: 'Life Sprint' })).toBeVisible();
    await expect(page.getByText(/plan your life, one sprint at a time/i)).toBeVisible();
    await expect(page.getByRole('button', { name: /sign in with github/i })).toBeVisible();
  });

  test('should return user data from /api/auth/me when authenticated', async ({ context }) => {
    // Login with test endpoint
    const loginResponse = await context.request.post('/api/auth/test-login', {
      data: {
        username: 'api-test-user'
      }
    });

    expect(loginResponse.ok()).toBeTruthy();

    // Verify /api/auth/me returns user data (using context.request to share cookies)
    const meResponse = await context.request.get('/api/auth/me');
    expect(meResponse.ok()).toBeTruthy();

    const userData = await meResponse.json();
    expect(userData.gitHubUsername).toBe('api-test-user');
    expect(userData.id).toBe('test_api-test-user');
  });

  test('should return 401 from /api/auth/me when not authenticated', async ({ request }) => {
    const response = await request.get('/api/auth/me');
    expect(response.status()).toBe(401);
  });
});
