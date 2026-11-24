import React, { useState } from 'react';

interface AuditLog {
  id: string;
  eventType: string;
  userId: string;
  action: string;
  timestamp: string;
  status: 'SUCCESS' | 'VIOLATION';
}

export default function AdminApp() {
  const [users] = useState([
    { id: 'usr-001', username: 'trader1', role: 'Senior Trader', kycTier: 'Tier 3 (Institutional)', status: 'ACTIVE' },
    { id: 'usr-002', username: 'trader2', role: 'Junior Trader', kycTier: 'Tier 2 (Verified)', status: 'ACTIVE' },
    { id: 'usr-003', username: 'risk_officer', role: 'Risk Admin', kycTier: 'Internal', status: 'ACTIVE' }
  ]);

  const [permissions, setPermissions] = useState<{ [key: string]: boolean }>({
    'AllowPlaceOrder': true,
    'AllowCancelOrder': true,
    'AllowViewBlotter': true,
    'AllowMarginOverride': false,
    'AllowManageSymbols': true,
    'AllowExportAudit': true
  });

  const [symbols] = useState([
    { ticker: 'PAL/USD', base: 'PAL', quote: 'USD', tickSize: '0.01', lotSize: '1.0' },
    { ticker: 'ETH/USD', base: 'ETH', quote: 'USD', tickSize: '0.10', lotSize: '0.1' },
    { ticker: 'BTC/USD', base: 'BTC', quote: 'USD', tickSize: '1.00', lotSize: '0.01' }
  ]);

  const [auditLogs] = useState<AuditLog[]>([
    { id: 'es-1001', eventType: 'ORDER_SUBMITTED', userId: 'usr-001', action: 'Place Buy Limit 100 PAL/USD @ 150.25', timestamp: '2026-11-23 10:14:02', status: 'SUCCESS' },
    { id: 'es-1002', eventType: 'MARGIN_CHECK', userId: 'usr-001', action: 'Reserved $1,502.50 pre-trade margin', timestamp: '2026-11-23 10:14:02', status: 'SUCCESS' },
    { id: 'es-1003', eventType: 'AI_PII_SCRUBBED', userId: 'ai-copilot', action: 'Presidio redacted 1 trader email from prompt', timestamp: '2026-11-23 10:15:30', status: 'SUCCESS' },
    { id: 'es-1004', eventType: 'POLICY_VIOLATION_BLOCKED', userId: 'usr-002', action: 'Attempted leverage override (denied)', timestamp: '2026-11-23 10:20:11', status: 'VIOLATION' }
  ]);

  const togglePermission = (perm: string) => {
    setPermissions(prev => ({ ...prev, [perm]: !prev[perm] }));
  };

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '24px', width: '100%' }}>
      {/* 1. User & KYC Tier Management */}
      <section aria-labelledby="user-mgmt-heading" style={{ background: '#111827', borderRadius: '8px', padding: '20px', border: '1px solid #1f2937' }}>
        <h2 id="user-mgmt-heading" style={{ fontSize: '18px', fontWeight: 600, color: '#f8fafc', margin: '0 0 16px 0' }}>Trader Profiles & Keycloak IAM</h2>
        <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left', fontSize: '13px' }}>
          <thead>
            <tr style={{ color: '#94a3b8', borderBottom: '1px solid #1f2937' }}>
              <th style={{ padding: '10px' }}>User ID</th>
              <th style={{ padding: '10px' }}>Username</th>
              <th style={{ padding: '10px' }}>Role</th>
              <th style={{ padding: '10px' }}>KYC Tier</th>
              <th style={{ padding: '10px' }}>Status</th>
            </tr>
          </thead>
          <tbody>
            {users.map(u => (
              <tr key={u.id} style={{ borderBottom: '1px solid #1e293b', color: '#f8fafc' }}>
                <td style={{ padding: '10px', fontFamily: 'monospace' }}>{u.id}</td>
                <td style={{ padding: '10px', fontWeight: 600 }}>{u.username}</td>
                <td style={{ padding: '10px', color: '#38bdf8' }}>{u.role}</td>
                <td style={{ padding: '10px' }}>{u.kycTier}</td>
                <td style={{ padding: '10px' }}><span style={{ padding: '2px 8px', borderRadius: '4px', background: '#064e3b', color: '#34d399', fontSize: '11px' }}>{u.status}</span></td>
              </tr>
            ))}
          </tbody>
        </table>
      </section>

      {/* 2. Granular PBAC Permission Matrix */}
      <section aria-labelledby="perm-matrix-heading" style={{ background: '#111827', borderRadius: '8px', padding: '20px', border: '1px solid #1f2937' }}>
        <h2 id="perm-matrix-heading" style={{ fontSize: '18px', fontWeight: 600, color: '#f8fafc', margin: '0 0 16px 0' }}>Redis-Cached Permission Matrix (PBAC)</h2>
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: '16px' }}>
          {Object.entries(permissions).map(([perm, enabled]) => (
            <div key={perm} style={{ background: '#090d16', padding: '14px', borderRadius: '6px', border: '1px solid #1e293b', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <span style={{ fontSize: '13px', color: '#e2e8f0', fontWeight: 500 }}>{perm}</span>
              <button
                type="button"
                onClick={() => togglePermission(perm)}
                aria-pressed={enabled}
                style={{
                  padding: '6px 12px',
                  borderRadius: '4px',
                  border: 'none',
                  fontWeight: 600,
                  fontSize: '12px',
                  cursor: 'pointer',
                  background: enabled ? '#10b981' : '#475569',
                  color: '#ffffff'
                }}>
                {enabled ? 'GRANTED' : 'DENIED'}
              </button>
            </div>
          ))}
        </div>
      </section>

      {/* 3. Symbol Configurator & Audit Log Grid */}
      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '20px' }}>
        <section aria-labelledby="symbol-config-heading" style={{ background: '#111827', borderRadius: '8px', padding: '20px', border: '1px solid #1f2937' }}>
          <h2 id="symbol-config-heading" style={{ fontSize: '18px', fontWeight: 600, color: '#f8fafc', margin: '0 0 16px 0' }}>Trading Symbol Registry</h2>
          <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left', fontSize: '13px' }}>
            <thead>
              <tr style={{ color: '#94a3b8', borderBottom: '1px solid #1f2937' }}>
                <th style={{ padding: '8px' }}>Ticker</th>
                <th style={{ padding: '8px' }}>Base</th>
                <th style={{ padding: '8px' }}>Quote</th>
                <th style={{ padding: '8px' }}>Tick Size</th>
                <th style={{ padding: '8px' }}>Lot Size</th>
              </tr>
            </thead>
            <tbody>
              {symbols.map(s => (
                <tr key={s.ticker} style={{ borderBottom: '1px solid #1e293b', color: '#f8fafc' }}>
                  <td style={{ padding: '8px', fontWeight: 600, color: '#38bdf8' }}>{s.ticker}</td>
                  <td style={{ padding: '8px' }}>{s.base}</td>
                  <td style={{ padding: '8px' }}>{s.quote}</td>
                  <td style={{ padding: '8px' }}>{s.tickSize}</td>
                  <td style={{ padding: '8px' }}>{s.lotSize}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </section>

        <section aria-labelledby="audit-log-heading" style={{ background: '#111827', borderRadius: '8px', padding: '20px', border: '1px solid #1f2937' }}>
          <h2 id="audit-log-heading" style={{ fontSize: '18px', fontWeight: 600, color: '#f8fafc', margin: '0 0 16px 0' }}>Meilisearch Compliance Audit Viewer</h2>
          <div style={{ display: 'flex', flexDirection: 'column', gap: '8px', maxHeight: '220px', overflowY: 'auto' }}>
            {auditLogs.map(l => (
              <div key={l.id} style={{ background: '#090d16', padding: '10px', borderRadius: '4px', border: '1px solid #1e293b', fontSize: '12px' }}>
                <div style={{ display: 'flex', justifyContent: 'space-between', color: '#94a3b8' }}>
                  <span style={{ fontWeight: 600, color: l.status === 'SUCCESS' ? '#34d399' : '#f87171' }}>{l.eventType}</span>
                  <span>{l.timestamp}</span>
                </div>
                <div style={{ color: '#e2e8f0', marginTop: '4px' }}>{l.action}</div>
              </div>
            ))}
          </div>
        </section>
      </div>
    </div>
  );
}
