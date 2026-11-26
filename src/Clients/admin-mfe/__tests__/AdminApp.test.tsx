import React from 'react';
import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect } from 'vitest';
import AdminApp from '../src/AdminApp';

describe('Admin MFE Component Tests', () => {
  it('toggles PBAC permission buttons with aria-pressed state', () => {
    render(<AdminApp />);

    const buttons = screen.getAllByRole('button');
    expect(buttons.length).toBeGreaterThan(0);
    const initialPressed = buttons[0].getAttribute('aria-pressed');
    fireEvent.click(buttons[0]);
    expect(buttons[0].getAttribute('aria-pressed')).not.toBe(initialPressed);
  });
});
