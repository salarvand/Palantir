import React from 'react';
import { render, screen, cleanup } from '@testing-library/react';
import { describe, it, expect, afterEach, vi } from 'vitest';

vi.mock('tradingDesk/TradingDeskApp', () => ({
  default: () => <div>Mocked Trading Desk</div>,
}));
vi.mock('portfolio/PortfolioApp', () => ({
  default: () => <div>Mocked Portfolio</div>,
}));
vi.mock('admin/AdminApp', () => ({
  default: () => <div>Mocked Admin</div>,
}));

import App from '../src/App';

afterEach(() => {
  cleanup();
});

describe('WCAG 2.1 AA Shell Accessibility Checks', () => {
  it('App Shell renders banner, navigation landmarks and accessible buttons', () => {
    render(<App />);

    const banner = screen.getByRole('banner');
    expect(banner).toBeDefined();

    const navigation = screen.getByRole('navigation', { name: /Main Navigation/i });
    expect(navigation).toBeDefined();

    const tradingBtn = screen.getByRole('button', { name: /Trading Desk/i });
    expect(tradingBtn).toBeDefined();
    expect(tradingBtn.getAttribute('aria-current')).toBe('page');
  });
});
