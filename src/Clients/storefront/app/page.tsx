import React from 'react';
import { A2UIRenderer } from '../components/a2ui-renderer';

export default function StorefrontPage() {
  return (
    <div style={{ minHeight: '100vh', display: 'flex', flexDirection: 'column' }}>
      <header
        role="banner"
        style={{
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          padding: '16px 24px',
          background: '#111827',
          borderBottom: '1px solid #1f2937',
        }}
      >
        <strong style={{ fontSize: '20px', color: '#38bdf8' }}>PALANTIR STOREFRONT</strong>
        <nav role="navigation" aria-label="Primary Multi-Zone Navigation" style={{ display: 'flex', gap: '16px' }}>
          <a href="/" style={{ color: '#f8fafc', textDecoration: 'none' }}>Markets</a>
          <a href="/desk" style={{ color: '#38bdf8', textDecoration: 'none' }}>MFE Trading Desk</a>
          <a href="/backoffice" style={{ color: '#38bdf8', textDecoration: 'none' }}>Admin Backoffice</a>
          <a href="/accessibility" style={{ color: '#94a3b8', textDecoration: 'none' }}>Accessibility</a>
        </nav>
      </header>

      <main id="main-content" role="main" style={{ padding: '24px', maxWidth: '1100px', margin: '0 auto', width: '100%' }}>
        <h1 style={{ marginTop: 0 }}>Institutional Trading Portal</h1>
        <p style={{ color: '#94a3b8' }}>
          Next.js Multi-Zone storefront integrating Keycloak OIDC PKCE authentication, Module Federation microfrontends,
          and live AG-UI/A2UI AI Trade Copilot workflows.
        </p>

        <A2UIRenderer
          proposal={{
            ProposalId: '00000000-0000-0000-0000-000000000001',
            Symbol: 'PAL/USD',
            Side: 'BUY',
            Quantity: 100,
            Price: 150.25,
            ExecutionStatus: 'READY_TO_EXECUTE',
            Confidence: 0.88,
            ReasoningNote: 'Order book bid depth imbalance +18% with pre-trade margin within limits.',
          }}
          riskGauge={{
            CurrentMarginPercent: 24.0,
            ProjectedMarginPercent: 27.0,
            RiskTier: 'LOW',
            WithinRiskTolerance: true,
          }}
        />
      </main>
    </div>
  );
}
