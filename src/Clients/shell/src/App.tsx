import React, { useState } from 'react';

type Tab = 'trading' | 'portfolio' | 'admin';

export default function App() {
  const [activeTab, setActiveTab] = useState<Tab>('trading');
  const [user] = useState({
    username: 'trader1',
    role: 'Trader',
    permissions: ['Permissions.Orders.Place', 'Permissions.Orders.Cancel', 'Permissions.Orders.ViewBlotter']
  });

  return (
    <div style={{ minHeight: '100vh', display: 'flex', flexDirection: 'column', background: '#090d16', color: '#e2e8f0' }}>
      {/* Top Navigation Bar */}
      <header style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', padding: '12px 24px', background: '#111827', borderBottom: '1px solid #1f2937' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: '24px' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
            <span style={{ fontSize: '20px', fontWeight: 'bold', color: '#38bdf8' }}>PALANTIR</span>
            <span style={{ fontSize: '11px', background: '#1e293b', padding: '2px 8px', borderRadius: '4px', color: '#94a3b8' }}>.NET 10 CLOUD-NATIVE</span>
          </div>

          <nav style={{ display: 'flex', gap: '8px' }}>
            <button
              onClick={() => setActiveTab('trading')}
              style={{
                background: activeTab === 'trading' ? '#0284c7' : 'transparent',
                color: '#fff',
                border: 'none',
                padding: '8px 16px',
                borderRadius: '6px',
                cursor: 'pointer',
                fontWeight: 500
              }}>
              Trading Desk
            </button>
            <button
              onClick={() => setActiveTab('portfolio')}
              style={{
                background: activeTab === 'portfolio' ? '#0284c7' : 'transparent',
                color: '#fff',
                border: 'none',
                padding: '8px 16px',
                borderRadius: '6px',
                cursor: 'pointer',
                fontWeight: 500
              }}>
              Portfolio & Risk
            </button>
            <button
              onClick={() => setActiveTab('admin')}
              style={{
                background: activeTab === 'admin' ? '#0284c7' : 'transparent',
                color: '#fff',
                border: 'none',
                padding: '8px 16px',
                borderRadius: '6px',
                cursor: 'pointer',
                fontWeight: 500
              }}>
              IAM & Admin
            </button>
          </nav>
        </div>

        {/* User Badge */}
        <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
          <div style={{ textAlign: 'right', fontSize: '12px' }}>
            <div style={{ fontWeight: 600, color: '#f8fafc' }}>{user.username}</div>
            <div style={{ color: '#38bdf8' }}>{user.role}</div>
          </div>
          <div style={{ width: '32px', height: '32px', borderRadius: '50%', background: '#0284c7', display: 'flex', alignItems: 'center', justifyContent: 'center', fontWeight: 'bold' }}>
            AT
          </div>
        </div>
      </header>

      {/* Main Workspace Workspace */}
      <main style={{ flex: 1, padding: '20px', display: 'flex', gap: '20px' }}>
        {activeTab === 'trading' && <TradingDeskView />}
        {activeTab === 'portfolio' && <PortfolioView />}
        {activeTab === 'admin' && <AdminView />}
      </main>
    </div>
  );
}

function TradingDeskView() {
  const [orders, setOrders] = useState([
    { id: 'ord-101', symbol: 'PAL/USD', side: 'BUY', qty: 250, price: 100.50, status: 'FILLED' },
    { id: 'ord-102', symbol: 'PAL/USD', side: 'SELL', qty: 100, price: 101.20, status: 'SUBMITTED' }
  ]);
  const [symbol, setSymbol] = useState('PAL/USD');
  const [qty, setQty] = useState(100);
  const [price, setPrice] = useState(100.50);

  const placeOrder = (side: 'BUY' | 'SELL') => {
    const newOrd = {
      id: `ord-${Date.now().toString().slice(-4)}`,
      symbol,
      side,
      qty,
      price,
      status: 'FILLED'
    };
    setOrders([newOrd, ...orders]);
  };

  return (
    <div style={{ display: 'flex', gap: '20px', width: '100%' }}>
      {/* Left: Order Book & Chart */}
      <div style={{ flex: 2, display: 'flex', flexDirection: 'column', gap: '20px' }}>
        <div style={{ background: '#111827', borderRadius: '8px', padding: '16px', border: '1px solid #1f2937' }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '12px' }}>
            <h3 style={{ margin: 0, fontSize: '16px' }}>{symbol} OHLC Real-Time Chart</h3>
            <span style={{ color: '#4ade80', fontWeight: 'bold' }}>$100.52 +1.24%</span>
          </div>
          <div style={{ height: '220px', background: '#0b0f19', borderRadius: '6px', display: 'flex', alignItems: 'center', justifyContent: 'center', color: '#64748b' }}>
            [TimescaleDB Continuous Aggregate Candlesticks: 1m / 5m / 1h / 1d Stream]
          </div>
        </div>

        {/* Order Blotter */}
        <div style={{ background: '#111827', borderRadius: '8px', padding: '16px', border: '1px solid #1f2937', flex: 1 }}>
          <h3 style={{ margin: '0 0 12px 0', fontSize: '16px' }}>Trade Blotter (Active & Executed)</h3>
          <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '13px' }}>
            <thead>
              <tr style={{ color: '#94a3b8', borderBottom: '1px solid #1f2937', textAlign: 'left' }}>
                <th style={{ padding: '8px' }}>Order ID</th>
                <th>Symbol</th>
                <th>Side</th>
                <th>Qty</th>
                <th>Price</th>
                <th>Status</th>
              </tr>
            </thead>
            <tbody>
              {orders.map(o => (
                <tr key={o.id} style={{ borderBottom: '1px solid #1f2937' }}>
                  <td style={{ padding: '8px', fontFamily: 'monospace' }}>{o.id}</td>
                  <td>{o.symbol}</td>
                  <td style={{ color: o.side === 'BUY' ? '#4ade80' : '#f87171', fontWeight: 'bold' }}>{o.side}</td>
                  <td>{o.qty}</td>
                  <td>${o.price.toFixed(2)}</td>
                  <td><span style={{ background: '#1e293b', padding: '2px 6px', borderRadius: '4px', fontSize: '11px' }}>{o.status}</span></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      {/* Right: Order Entry & Order Book */}
      <div style={{ flex: 1, display: 'flex', flexDirection: 'column', gap: '20px' }}>
        <div style={{ background: '#111827', borderRadius: '8px', padding: '16px', border: '1px solid #1f2937' }}>
          <h3 style={{ margin: '0 0 16px 0', fontSize: '16px' }}>Order Entry (Orleans OMS)</h3>
          <div style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
            <div>
              <label style={{ fontSize: '12px', color: '#94a3b8' }}>Symbol</label>
              <input value={symbol} onChange={e => setSymbol(e.target.value)} style={{ width: '100%', padding: '8px', background: '#1e293b', border: '1px solid #334155', color: '#fff', borderRadius: '4px' }} />
            </div>
            <div>
              <label style={{ fontSize: '12px', color: '#94a3b8' }}>Quantity</label>
              <input type="number" value={qty} onChange={e => setQty(Number(e.target.value))} style={{ width: '100%', padding: '8px', background: '#1e293b', border: '1px solid #334155', color: '#fff', borderRadius: '4px' }} />
            </div>
            <div>
              <label style={{ fontSize: '12px', color: '#94a3b8' }}>Limit Price</label>
              <input type="number" step="0.01" value={price} onChange={e => setPrice(Number(e.target.value))} style={{ width: '100%', padding: '8px', background: '#1e293b', border: '1px solid #334155', color: '#fff', borderRadius: '4px' }} />
            </div>
            <div style={{ display: 'flex', gap: '12px', marginTop: '8px' }}>
              <button onClick={() => placeOrder('BUY')} style={{ flex: 1, background: '#16a34a', color: '#fff', border: 'none', padding: '10px', borderRadius: '6px', fontWeight: 'bold', cursor: 'pointer' }}>
                BUY / LONG
              </button>
              <button onClick={() => placeOrder('SELL')} style={{ flex: 1, background: '#dc2626', color: '#fff', border: 'none', padding: '10px', borderRadius: '6px', fontWeight: 'bold', cursor: 'pointer' }}>
                SELL / SHORT
              </button>
            </div>
          </div>
        </div>

        <div style={{ background: '#111827', borderRadius: '8px', padding: '16px', border: '1px solid #1f2937', flex: 1 }}>
          <h3 style={{ margin: '0 0 12px 0', fontSize: '16px' }}>L2 Order Book</h3>
          <div style={{ display: 'flex', flexDirection: 'column', gap: '4px', fontSize: '12px' }}>
            <div style={{ color: '#f87171', display: 'flex', justifyContent: 'space-between' }}><span>100.60</span><span>1,200</span></div>
            <div style={{ color: '#f87171', display: 'flex', justifyContent: 'space-between' }}><span>100.55</span><span>850</span></div>
            <div style={{ padding: '6px 0', textAlign: 'center', fontWeight: 'bold', borderTop: '1px solid #334155', borderBottom: '1px solid #334155' }}>Spread: 0.05 (Last: 100.52)</div>
            <div style={{ color: '#4ade80', display: 'flex', justifyContent: 'space-between' }}><span>100.50</span><span>1,500</span></div>
            <div style={{ color: '#4ade80', display: 'flex', justifyContent: 'space-between' }}><span>100.45</span><span>2,400</span></div>
          </div>
        </div>
      </div>
    </div>
  );
}

function PortfolioView() {
  return (
    <div style={{ width: '100%', display: 'flex', flexDirection: 'column', gap: '20px' }}>
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: '16px' }}>
        <div style={{ background: '#111827', padding: '16px', borderRadius: '8px', border: '1px solid #1f2937' }}>
          <div style={{ color: '#94a3b8', fontSize: '12px' }}>Cash Balance</div>
          <div style={{ fontSize: '24px', fontWeight: 'bold', color: '#f8fafc' }}>$100,000.00</div>
        </div>
        <div style={{ background: '#111827', padding: '16px', borderRadius: '8px', border: '1px solid #1f2937' }}>
          <div style={{ color: '#94a3b8', fontSize: '12px' }}>Reserved Margin</div>
          <div style={{ fontSize: '24px', fontWeight: 'bold', color: '#f59e0b' }}>$15,200.00</div>
        </div>
        <div style={{ background: '#111827', padding: '16px', borderRadius: '8px', border: '1px solid #1f2937' }}>
          <div style={{ color: '#94a3b8', fontSize: '12px' }}>Available Margin</div>
          <div style={{ fontSize: '24px', fontWeight: 'bold', color: '#4ade80' }}>$84,800.00</div>
        </div>
        <div style={{ background: '#111827', padding: '16px', borderRadius: '8px', border: '1px solid #1f2937' }}>
          <div style={{ color: '#94a3b8', fontSize: '12px' }}>Unrealized PnL</div>
          <div style={{ fontSize: '24px', fontWeight: 'bold', color: '#4ade80' }}>+$2,750.00</div>
        </div>
      </div>
    </div>
  );
}

function AdminView() {
  return (
    <div style={{ width: '100%', display: 'flex', flexDirection: 'column', gap: '20px' }}>
      <div style={{ background: '#111827', padding: '20px', borderRadius: '8px', border: '1px solid #1f2937' }}>
        <h3 style={{ margin: '0 0 16px 0' }}>Keycloak IAM & Permission Matrix</h3>
        <p style={{ color: '#94a3b8', fontSize: '13px' }}>
          Granular permissions evaluated via Redis Permission Token Cache (&lt;1ms check time).
        </p>
        <div style={{ display: 'flex', gap: '8px', flexWrap: 'wrap' }}>
          {['AllowPlaceOrder', 'AllowCancelOrder', 'AllowDefineSymbol', 'AllowOverrideRisk', 'AllowViewBlotter', 'AllowViewAuditLogs'].map(p => (
            <span key={p} style={{ background: '#1e293b', color: '#38bdf8', padding: '4px 10px', borderRadius: '4px', fontSize: '12px' }}>
              ✓ {p}
            </span>
          ))}
        </div>
      </div>
    </div>
  );
}
