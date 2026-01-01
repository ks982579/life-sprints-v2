import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './e2e',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : undefined,
  reporter: 'html',

  use: {
    baseURL: 'http://localhost',
    trace: 'on-first-retry',
  },

  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],

  // Note: For local development, start docker-compose manually before running tests
  // In CI, the webServer config below will start it automatically
  ...(process.env.CI && {
    webServer: {
      command: 'docker-compose up',
      url: 'http://localhost',
      reuseExistingServer: false,
      cwd: '../..',
    },
  }),
});
