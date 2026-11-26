import React from 'react';
import type { Metadata } from 'next';

export const metadata: Metadata = {
  title: 'Palantir Admin Backoffice',
  description: 'Next.js Multi-Zone Admin, Risk Governance & Compliance Dashboard',
};

export default function BackofficeLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="en">
      <body style={{ margin: 0, background: '#090d16', color: '#e2e8f0', fontFamily: 'system-ui, sans-serif' }}>
        {children}
      </body>
    </html>
  );
}
