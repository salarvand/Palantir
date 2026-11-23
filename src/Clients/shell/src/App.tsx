import React, { useState, Suspense } from 'react';

// Webpack 5 Module Federation Remote Imports with Lazy Loading
const TradingDeskRemote = React.lazy(() =>
  import('tradingDesk/TradingDeskApp').catch(() => ({
    default: () => (
      <div role="alert" style={{ padding: '24px', background: '#1e293b', borderRadius: '8px', border: '1px solid #475569', color: '#f8fafc' }}>
        <h3 style={{ color: '#f87171', margin: '0 0 8px 0' }}>Trading Desk Remote Unavailable</h3>
        <p style={{ margin: 0, color: '#94a3b8' }}>Please verify that the Trading Desk remote service is running on port 3001.</p>
      </div>
    )
  }))
);

const PortfolioRemote = React.lazy(() =>
  import('portfolio/PortfolioApp').catch(() => ({
    default: () => (
      <div role="alert" style={{ padding: '24px', background: '#1e293b', borderRadius: '8px', border: '1px solid #475569', color: '#f8fafc' }}>
        <h3 style={{ color: '#f87171', margin: '0 0 8px 0' }}>Portfolio Remote Unavailable</h3>
        <p style={{ margin: 0, color: '#94a3b8' }}>Please verify that the Portfolio & Risk remote service is running on port 3002.</p>
      </div>
    )
  }))
);

const AdminRemote = React.lazy(() =>
  import('admin/AdminApp').catch(() => ({
    default: () => (
      <div role="alert" style={{ padding: '24px', background: '#1e293b', borderRadius: '8px', border: '1px solid #475569', color: '#f8fafc' }}>
        <h3 style={{ color: '#f87171', margin: '0 0 8px 0' }}>Admin Remote Unavailable</h3>
        <p style={{ margin: 0, color: '#94a3b8' }}>Please verify that the Admin & IAM remote service is running on port 3003.</p>
      </div>
    )
  }))
);

type Tab = 'trading' | 'portfolio' | 'admin';

export default function App() {
  const [activeTab, setActiveTab] = useState<Tab>('trading');
  const [user] = useState({
    username: 'trader1',
    role: 'Senior Trader',
    kycTier: 'Tier 3 (Institutional)',
    permissions: ['AllowPlaceOrder', 'AllowCancelOrder', 'AllowViewBlotter']
  });

  return (
    <div style={{ minHeight: '100vh', display: 'flex', flexDirection: 'column', background: '#090d16', color: '#e2e8f0' }}>
      {/* Top Navigation Bar - WCAG 2.1 AA Compliant */}
      <header role="banner" style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', padding: '14px 24px', background: '#111827', borderBottom: '1px solid #1f2937' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: '28px' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
            <span style={{ fontSize: '22px', fontWeight: 800, color: '#38bdf8', letterSpacing: '-0.5px' }}>PALANTIR</span>
            <span style={{ fontSize: '11px', background: '#1e293b', padding: '3px 8px', borderRadius: '4px', color: '#94a3b8', fontWeight: 600 }}>.NET 10 CLOUD-NATIVE</span>
          </div>

          <nav role="navigation" aria-label="Main Navigation" style={{ display: 'flex', gap: '10px' }}>
            <button
              type="button"
              onClick={() => setActiveTab('trading')}
              aria-current={activeTab === 'trading' ? 'page' : undefined}
              style={{
                background: activeTab === 'trading' ? '#0284c7' : 'transparent',
                color: '#ffffff',
                border: activeTab === 'trading' ? '1px solid #38bdf8' : '1px solid transparent',
                padding: '8px 18px',
                borderRadius: '6px',
                cursor: 'pointer',
                fontWeight: 600,
                fontSize: '14px',
                outline: 'none',
                transition: 'all 0.15s ease'
              }}>
              Trading Desk (:3001)
            </button>
            <button
              type="button"
              onClick={() => setActiveTab('portfolio')}
              aria-current={activeTab === 'portfolio' ? 'page' : undefined}
              style={{
                background: activeTab === 'portfolio' ? '#0284c7' : 'transparent',
                color: '#ffffff',
                border: activeTab === 'portfolio' ? '1px solid #38bdf8' : '1px solid transparent',
                padding: '8px 18px',
                borderRadius: '6px',
                cursor: 'pointer',
                fontWeight: 600,
                fontSize: '14px',
                outline: 'none',
                transition: 'all 0.15s ease'
              }}>
              Portfolio & Risk (:3002)
            </button>
            <button
              type="button"
              onClick={() => setActiveTab('admin')}
              aria-current={activeTab === 'admin' ? 'page' : undefined}
              style={{
                background: activeTab === 'admin' ? '#0284c7' : 'transparent',
                color: '#ffffff',
                border: activeTab === 'admin' ? '1px solid #38bdf8' : '1px solid transparent',
                padding: '8px 18px',
                borderRadius: '6px',
                cursor: 'pointer',
                fontWeight: 600,
                fontSize: '14px',
                outline: 'none',
                transition: 'all 0.15s ease'
              }}>
              IAM & Admin (:3003)
            </button>
          </nav>
        </div>

        {/* User Identity & Keycloak PKCE Session Indicator */}
        <div style={{ display: 'flex', alignItems: 'center', gap: '14px' }}>
          <div style={{ textAlign: 'right', fontSize: '13px' }}>
            <div style={{ fontWeight: 600, color: '#f8fafc' }}>{user.username}</div>
            <div style={{ color: '#38bdf8', fontSize: '11px' }}>{user.role} &bull; {user.kycTier}</div>
          </div>
          <div
            aria-hidden="true"
            style={{
              width: '36px',
              height: '36px',
              borderRadius: '50%',
              background: '#0284c7',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              fontWeight: 700,
              color: '#ffffff'
            }}>
            T1
          </div>
        </div>
      </header>

      {/* Main Microfrontend Workspace Viewport */}
      <main role="main" style={{ flex: 1, padding: '24px', display: 'flex' }}>
        <Suspense
          fallback={
            <div role="status" aria-live="polite" style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', width: '100%', height: '300px', color: '#94a3b8' }}>
              Loading Module Federation remote container...
            </div>
          }>
          {activeTab === 'trading' && <TradingDeskRemote />}
          {activeTab === 'portfolio' && <PortfolioRemote />}
          {activeTab === 'admin' && <AdminRemote />}
        </Suspense>
      </main>
    </div>
  );
}
