import React from 'react';

export default function BackofficeDashboardPage() {
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
        <strong style={{ fontSize: '20px', color: '#38bdf8' }}>PALANTIR ADMIN BACKOFFICE</strong>
        <nav role="navigation" aria-label="Backoffice Navigation" style={{ display: 'flex', gap: '16px' }}>
          <a href={process.env.NEXT_PUBLIC_STOREFRONT_URL || 'http://localhost:3100'} style={{ color: '#94a3b8', textDecoration: 'none' }}>Storefront</a>
          <a href="/desk" style={{ color: '#38bdf8', textDecoration: 'none' }}>MFE Shell</a>
        </nav>
      </header>

      <main id="main-content" role="main" style={{ padding: '24px', maxWidth: '1100px', margin: '0 auto', width: '100%' }}>
        <h1 style={{ marginTop: 0 }}>Operations, Risk &amp; IAM Backoffice</h1>
        <section aria-label="Governance Controls" style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: '16px' }}>
          <article style={{ background: '#111827', padding: '16px', borderRadius: '8px', border: '1px solid #1f2937' }}>
            <h2 style={{ marginTop: 0, fontSize: '16px' }}>Keycloak IAM &amp; PBAC</h2>
            <p style={{ color: '#94a3b8', fontSize: '14px' }}>Manage trader KYC tiers, roles, and PostgreSQL permission tokens.</p>
          </article>
          <article style={{ background: '#111827', padding: '16px', borderRadius: '8px', border: '1px solid #1f2937' }}>
            <h2 style={{ marginTop: 0, fontSize: '16px' }}>Pre-Trade Risk Limits</h2>
            <p style={{ color: '#94a3b8', fontSize: '14px' }}>Configure max single-order notional caps and margin thresholds.</p>
          </article>
          <article style={{ background: '#111827', padding: '16px', borderRadius: '8px', border: '1px solid #1f2937' }}>
            <h2 style={{ marginTop: 0, fontSize: '16px' }}>AI Agent Governance</h2>
            <p style={{ color: '#94a3b8', fontSize: '14px' }}>Audit MCP tool calls, PII redaction, and YAML agent policies.</p>
          </article>
        </section>
      </main>
    </div>
  );
}
