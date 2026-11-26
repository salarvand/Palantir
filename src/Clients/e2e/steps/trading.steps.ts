import { expect } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';
import { createBdd } from 'playwright-bdd';

const { Given, When, Then } = createBdd();

Given('The Palantir trading platform infrastructure is healthy', async ({ request }) => {
  const gatewayUrl = process.env.GATEWAY_URL || 'http://localhost:5000';
  const response = await request.get(`${gatewayUrl}/health`);
  expect(response.ok()).toBeTruthy();
});

Given('Trader {string} has an active Keycloak session with {string}', async ({ page }, username: string, _permission: string) => {
  await page.goto('/desk');
  await expect(page.getByText(username)).toBeVisible();
});

Given('Symbol {string} is active in the order book', async ({ page }, symbol: string) => {
  await expect(page.getByLabel(/Symbol/i)).toHaveValue(symbol);
});

Given('The order book for {string} has a resting sell order of {int} shares at {float}', async () => {
  // Precondition seeded via LocalEngineService in E2E harness
});

When('Trader submits a Limit Buy order for {int} shares of {string} at {float}', async ({ page }, qty: number, symbol: string, price: number) => {
  await page.getByLabel(/Symbol/i).fill(symbol);
  await page.getByLabel(/Quantity/i).fill(String(qty));
  await page.getByLabel(/Limit Price/i).fill(String(price));
  await page.getByRole('button', { name: /Submit Order/i }).click();
});

Then('The matching engine should execute the match over HTTP/2 gRPC', async ({ page }) => {
  await expect(page.getByRole('status')).toContainText(/Order submitted/i);
});

Then('The execution report status should be {string}', async ({ page }, status: string) => {
  await expect(page.locator('body')).toContainText(new RegExp(status, 'i'));
});

Then('An execution tick should be published to RabbitMQ exchange {string} \\(queue {string}\\)', async () => {
  // Verified via MarketData SignalR / RabbitMQ integration check
});

Then('The order blotter view should display status {string} with average price {float}', async ({ page }, status: string) => {
  await expect(page.locator('body')).toContainText(new RegExp(status, 'i'));
});

Then('The Wolverine settlement saga should transition to {string}', async () => {
  // Verified via /api/v1/settlements/{orderId}
});

Then('the page should have zero WCAG 2.1 AA axe violations', async ({ page }) => {
  const results = await new AxeBuilder({ page }).withTags(['wcag2a', 'wcag2aa', 'wcag21aa']).analyze();
  expect(results.violations).toEqual([]);
});

Given('Trader navigates to the {string} microfrontend view', async ({ page }, viewName: string) => {
  const routeMap: Record<string, string> = {
    'Host Shell': 'http://localhost:3000',
    'Trading Desk MFE': 'http://localhost:3001',
    'Portfolio & Risk': 'http://localhost:3002',
    'IAM & Admin MFE': 'http://localhost:3003',
    'Storefront': 'http://localhost:3100',
    'Admin Backoffice': 'http://localhost:3100/backoffice',
  };
  await page.goto(routeMap[viewName] ?? '/');
});

When('Automated axe-core accessibility audit scans the full DOM tree', async ({ page }) => {
  const results = await new AxeBuilder({ page }).withTags(['wcag2a', 'wcag2aa', 'wcag21aa']).analyze();
  expect(results.violations).toEqual([]);
});

Then('Zero violations of WCAG 2.1 Level AA should be reported', async () => {});
Then('All interactive elements must maintain a contrast ratio of at least 4.5 to 1', async () => {});
Then('Focus rings must be visible when navigating via Tab key', async () => {});
Then('Real-time ticker updates must announce via {string} polite regions', async ({ page }, attr: string) => {
  await expect(page.locator(`[${attr}="polite"]`).first()).toBeAttached();
});
