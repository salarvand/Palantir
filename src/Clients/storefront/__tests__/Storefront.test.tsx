import React from 'react';
import { render, screen } from '@testing-library/react';
import { describe, it, expect } from 'vitest';
import StorefrontPage from '../app/page';

describe('Storefront Multi-Zone & A2UI Tests', () => {
  it('renders banner, multi-zone navigation links, and main landmark', () => {
    render(<StorefrontPage />);

    expect(screen.getByRole('banner')).toBeDefined();
    expect(screen.getByRole('navigation', { name: /Primary Multi-Zone Navigation/i })).toBeDefined();
    expect(screen.getByRole('main')).toBeDefined();
    expect(screen.getByRole('link', { name: /MFE Trading Desk/i }).getAttribute('href')).toBe('/desk');
    expect(screen.getByRole('link', { name: /Admin Backoffice/i }).getAttribute('href')).toBe('/backoffice');
  });

  it('renders interactive A2UI trade proposal and risk gauge widgets', () => {
    render(<StorefrontPage />);

    expect(screen.getByRole('region', { name: /AI Copilot Interactive A2UI Surface/i })).toBeDefined();
    expect(screen.getByRole('progressbar', { name: /Projected Margin Utilization/i })).toBeDefined();
  });
});
