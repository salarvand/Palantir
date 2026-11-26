import React from 'react';
import { render, screen } from '@testing-library/react';
import { describe, it, expect } from 'vitest';
import BackofficeDashboardPage from '../app/page';

describe('Admin Backoffice Multi-Zone Tests', () => {
  it('renders banner, navigation, and governance control cards', () => {
    render(<BackofficeDashboardPage />);

    expect(screen.getByRole('banner')).toBeDefined();
    expect(screen.getByRole('navigation', { name: /Backoffice Navigation/i })).toBeDefined();
    expect(screen.getByRole('heading', { name: /Operations, Risk & IAM Backoffice/i })).toBeDefined();
    expect(screen.getByRole('region', { name: /Governance Controls/i })).toBeDefined();
  });
});
