import React from 'react';
import { render, screen } from '@testing-library/react';
import { describe, it, expect } from 'vitest';
import App from '../src/App';
import TradingDeskApp from '../../trading-desk/src/TradingDeskApp';

describe('WCAG 2.1 AA Accessibility Checks', () => {
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

  it('TradingDeskApp renders order form with associated labels and polite live regions', () => {
    render(<TradingDeskApp />);

    const statusRegion = screen.getByRole('status');
    expect(statusRegion).toBeDefined();
    expect(statusRegion.getAttribute('aria-live')).toBe('polite');

    const symbolInput = screen.getByLabelText(/Symbol/i);
    expect(symbolInput).toBeDefined();

    const qtyInput = screen.getByLabelText(/Quantity/i);
    expect(qtyInput).toBeDefined();

    const priceInput = screen.getByLabelText(/Limit Price/i);
    expect(priceInput).toBeDefined();

    const submitBtn = screen.getByRole('button', { name: /Submit Order/i });
    expect(submitBtn).toBeDefined();
  });
});
