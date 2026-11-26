import React from 'react';
import { render, screen } from '@testing-library/react';
import { describe, it, expect } from 'vitest';
import PortfolioApp from '../src/PortfolioApp';

describe('Portfolio MFE Component Tests', () => {
  it('renders accessible margin utilization progressbar and positions table', () => {
    render(<PortfolioApp />);

    expect(screen.getByRole('progressbar', { name: /Margin Utilization/i })).toBeDefined();
    expect(screen.getByRole('table')).toBeDefined();
  });
});
