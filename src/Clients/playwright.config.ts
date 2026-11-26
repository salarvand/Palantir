import { defineConfig } from '@playwright/test';
import { defineBddConfig } from 'playwright-bdd';

const testDir = defineBddConfig({
  features: '../../tests/bdd/*.feature',
  steps: 'e2e/steps/*.ts',
});

export default defineConfig({
  testDir,
  reporter: [['list'], ['html', { open: 'never' }]],
  use: {
    baseURL: process.env.STOREFRONT_URL || 'http://localhost:3100',
    trace: 'on-first-retry',
  },
});
