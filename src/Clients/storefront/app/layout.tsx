import React from 'react';
import type { Metadata } from 'next';

export const metadata: Metadata = {
  title: 'Palantir Trading Storefront',
  description: 'Next.js Multi-Zone Customer-Facing Trading Portal',
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="en">
      <body style={{ margin: 0, background: '#090d16', color: '#e2e8f0', fontFamily: 'system-ui, sans-serif' }}>
        <a
          href="#main-content"
          style={{
            position: 'absolute',
            left: '-9999px',
            padding: '8px 16px',
            background: '#0284c7',
            color: '#ffffff',
          }}
        >
          Skip to main content
        </a>
        {children}
      </body>
    </html>
  );
}
