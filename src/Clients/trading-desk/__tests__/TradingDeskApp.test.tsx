import React from 'react';
import { render, screen, cleanup } from '@testing-library/react';
import { describe, it, expect, afterEach } from 'vitest';
import TradingDeskApp from '../src/TradingDeskApp';

afterEach(() => {
  cleanup();
});

describe('TradingDesk MFE Component & Accessibility Tests', () => {
  it('renders order form with associated labels and polite live regions', () => {
    render(<TradingDeskApp />);

    const statusRegion = screen.getByRole('status');
    expect(statusRegion).toBeDefined();
    expect(statusRegion.getAttribute('aria-live')).toBe('polite');

    expect(screen.getByLabelText(/Symbol/i)).toBeDefined();
    expect(screen.getByLabelText(/Quantity/i)).toBeDefined();
    expect(screen.getByLabelText(/Limit Price/i)).toBeDefined();
    expect(screen.getByRole('button', { name: /Submit Order/i })).toBeDefined();
  });
});
