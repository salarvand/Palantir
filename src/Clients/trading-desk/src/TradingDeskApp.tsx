import React, { useState, useEffect } from 'react';

interface OrderBookLevel {
  price: number;
  quantity: number;
}

interface Order {
  id: string;
  symbol: string;
  side: 'BUY' | 'SELL';
  quantity: number;
  price: number;
  status: string;
  timestamp: string;
}

interface CopilotProposal {
  symbol: string;
  side: 'BUY' | 'SELL';
  quantity: number;
  price: number;
  status: string;
  confidence: number;
  reasoning: string;
}

export default function TradingDeskApp() {
  const [symbol, setSymbol] = useState('PAL/USD');
  const [side, setSide] = useState<'BUY' | 'SELL'>('BUY');
  const [quantity, setQuantity] = useState<number>(100);
  const [price, setPrice] = useState<number>(150.25);
  const [orders, setOrders] = useState<Order[]>([
    { id: 'ord-8101', symbol: 'PAL/USD', side: 'BUY', quantity: 100, price: 150.25, status: 'FILLED', timestamp: new Date().toLocaleTimeString() }
  ]);
  const [bids] = useState<OrderBookLevel[]>([
    { price: 150.20, quantity: 250 },
    { price: 150.15, quantity: 400 },
    { price: 150.10, quantity: 1200 },
  ]);
  const [asks] = useState<OrderBookLevel[]>([
    { price: 150.30, quantity: 300 },
    { price: 150.35, quantity: 650 },
    { price: 150.40, quantity: 800 },
  ]);
  const [isCopilotOpen, setIsCopilotOpen] = useState(false);
  const [copilotLoading, setCopilotLoading] = useState(false);
  const [proposal, setProposal] = useState<CopilotProposal | null>(null);
  const [liveAnnouncement, setLiveAnnouncement] = useState('Trading Desk ready.');

  const handlePlaceOrder = (e: React.FormEvent) => {
    e.preventDefault();
    const newOrder: Order = {
      id: `ord-${Math.floor(1000 + Math.random() * 9000)}`,
      symbol,
      side,
      quantity,
      price,
      status: 'SUBMITTED',
      timestamp: new Date().toLocaleTimeString()
    };
    setOrders([newOrder, ...orders]);
    setLiveAnnouncement(`Order submitted: ${side} ${quantity} ${symbol} at $${price.toFixed(2)}.`);
  };

  const requestCopilotAnalysis = async () => {
    setIsCopilotOpen(true);
    setCopilotLoading(true);
    setLiveAnnouncement('Copilot evaluating order book depth and risk invariants...');

    setTimeout(() => {
      setProposal({
        symbol,
        side,
        quantity,
        price,
        status: 'READY_TO_EXECUTE',
        confidence: 0.88,
        reasoning: `Order book imbalance is +18% bullish on ${symbol}. Projected margin utilization is 24.5%, safely below 85% limit.`
      });
      setCopilotLoading(false);
      setLiveAnnouncement('Copilot analysis ready: Proposal card generated.');
    }, 600);
  };

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '20px', width: '100%' }}>
      {/* Hidden ARIA Live region for screen readers (WCAG 2.1 AA) */}
      <div role="status" aria-live="polite" aria-atomic="true" style={{ position: 'absolute', width: '1px', height: '1px', padding: 0, overflow: 'hidden', clip: 'rect(0,0,0,0)', border: 0 }}>
        {liveAnnouncement}
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: '320px 1fr 340px', gap: '20px' }}>
        {/* 1. Order Entry Ticket */}
        <section aria-labelledby="order-ticket-heading" style={{ background: '#111827', borderRadius: '8px', padding: '16px', border: '1px solid #1f2937' }}>
          <h2 id="order-ticket-heading" style={{ fontSize: '16px', fontWeight: 600, color: '#f8fafc', margin: '0 0 16px 0' }}>Order Entry Ticket</h2>
          <form onSubmit={handlePlaceOrder} style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
            <div>
              <label htmlFor="symbol-input" style={{ display: 'block', fontSize: '13px', color: '#cbd5e1', marginBottom: '4px' }}>Symbol</label>
              <input
                id="symbol-input"
                type="text"
                value={symbol}
                onChange={e => setSymbol(e.target.value.toUpperCase())}
                style={{ width: '100%', boxSizing: 'border-box', padding: '8px 12px', background: '#090d16', border: '1px solid #334155', borderRadius: '4px', color: '#f8fafc', fontSize: '14px' }}
                required
              />
            </div>

            <div role="radiogroup" aria-label="Trade Side" style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '8px' }}>
              <button
                type="button"
                onClick={() => setSide('BUY')}
                aria-pressed={side === 'BUY'}
                style={{
                  padding: '10px',
                  background: side === 'BUY' ? '#10b981' : '#1e293b',
                  color: '#ffffff',
                  fontWeight: 600,
                  border: 'none',
                  borderRadius: '4px',
                  cursor: 'pointer'
                }}>
                BUY
              </button>
              <button
                type="button"
                onClick={() => setSide('SELL')}
                aria-pressed={side === 'SELL'}
                style={{
                  padding: '10px',
                  background: side === 'SELL' ? '#ef4444' : '#1e293b',
                  color: '#ffffff',
                  fontWeight: 600,
                  border: 'none',
                  borderRadius: '4px',
                  cursor: 'pointer'
                }}>
                SELL
              </button>
            </div>

            <div>
              <label htmlFor="qty-input" style={{ display: 'block', fontSize: '13px', color: '#cbd5e1', marginBottom: '4px' }}>Quantity</label>
              <input
                id="qty-input"
                type="number"
                min="1"
                step="1"
                value={quantity}
                onChange={e => setQuantity(Number(e.target.value))}
                style={{ width: '100%', boxSizing: 'border-box', padding: '8px 12px', background: '#090d16', border: '1px solid #334155', borderRadius: '4px', color: '#f8fafc', fontSize: '14px' }}
                required
              />
            </div>

            <div>
              <label htmlFor="price-input" style={{ display: 'block', fontSize: '13px', color: '#cbd5e1', marginBottom: '4px' }}>Limit Price ($)</label>
              <input
                id="price-input"
                type="number"
                min="0.01"
                step="0.01"
                value={price}
                onChange={e => setPrice(Number(e.target.value))}
                style={{ width: '100%', boxSizing: 'border-box', padding: '8px 12px', background: '#090d16', border: '1px solid #334155', borderRadius: '4px', color: '#f8fafc', fontSize: '14px' }}
                required
              />
            </div>

            <button
              type="submit"
              style={{
                marginTop: '8px',
                padding: '12px',
                background: '#0284c7',
                color: '#ffffff',
                border: 'none',
                borderRadius: '6px',
                fontWeight: 600,
                fontSize: '14px',
                cursor: 'pointer'
              }}>
              Submit Order
            </button>

            <button
              type="button"
              onClick={requestCopilotAnalysis}
              style={{
                padding: '10px',
                background: '#4f46e5',
                color: '#ffffff',
                border: 'none',
                borderRadius: '6px',
                fontWeight: 600,
                fontSize: '13px',
                cursor: 'pointer'
              }}>
              Copilot Pre-Trade Review
            </button>
          </form>
        </section>

        {/* 2. Market Candlestick & Depth Center */}
        <section aria-labelledby="chart-heading" style={{ display: 'flex', flexDirection: 'column', gap: '20px' }}>
          <div style={{ background: '#111827', borderRadius: '8px', padding: '16px', border: '1px solid #1f2937' }}>
            <h2 id="chart-heading" style={{ fontSize: '16px', fontWeight: 600, color: '#f8fafc', margin: '0 0 16px 0' }}>TimescaleDB Momentum Feed ({symbol})</h2>
            <div style={{ height: '240px', background: '#090d16', borderRadius: '6px', display: 'flex', alignItems: 'center', justifyContent: 'center', border: '1px solid #1e293b' }}>
              <span style={{ color: '#94a3b8', fontSize: '14px' }}>[Interactive Candlestick Chart: Continuous 1m Aggregates]</span>
            </div>
          </div>

          {/* Active Blotter Table */}
          <div style={{ background: '#111827', borderRadius: '8px', padding: '16px', border: '1px solid #1f2937' }}>
            <h2 style={{ fontSize: '16px', fontWeight: 600, color: '#f8fafc', margin: '0 0 12px 0' }}>Active Order Blotter</h2>
            <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left', fontSize: '13px' }}>
              <thead>
                <tr style={{ color: '#94a3b8', borderBottom: '1px solid #1f2937' }}>
                  <th style={{ padding: '8px' }}>Order ID</th>
                  <th style={{ padding: '8px' }}>Side</th>
                  <th style={{ padding: '8px' }}>Qty</th>
                  <th style={{ padding: '8px' }}>Price</th>
                  <th style={{ padding: '8px' }}>Status</th>
                  <th style={{ padding: '8px' }}>Time</th>
                </tr>
              </thead>
              <tbody>
                {orders.map(ord => (
                  <tr key={ord.id} style={{ borderBottom: '1px solid #1e293b', color: '#f8fafc' }}>
                    <td style={{ padding: '8px', fontFamily: 'monospace' }}>{ord.id}</td>
                    <td style={{ padding: '8px', color: ord.side === 'BUY' ? '#10b981' : '#ef4444', fontWeight: 600 }}>{ord.side}</td>
                    <td style={{ padding: '8px' }}>{ord.quantity}</td>
                    <td style={{ padding: '8px' }}>${ord.price.toFixed(2)}</td>
                    <td style={{ padding: '8px' }}><span style={{ padding: '2px 8px', borderRadius: '4px', background: '#1e293b', fontSize: '11px', color: '#38bdf8' }}>{ord.status}</span></td>
                    <td style={{ padding: '8px', color: '#94a3b8' }}>{ord.timestamp}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>

        {/* 3. L2 Order Book Widget */}
        <section aria-labelledby="orderbook-heading" style={{ background: '#111827', borderRadius: '8px', padding: '16px', border: '1px solid #1f2937' }}>
          <h2 id="orderbook-heading" style={{ fontSize: '16px', fontWeight: 600, color: '#f8fafc', margin: '0 0 16px 0' }}>L2 Order Book</h2>
          <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
            <div style={{ fontSize: '12px', fontWeight: 600, color: '#ef4444', borderBottom: '1px solid #374151', paddingBottom: '4px' }}>ASKS</div>
            {asks.slice().reverse().map((a, i) => (
              <div key={i} style={{ display: 'flex', justifyContent: 'space-between', fontSize: '13px', color: '#fca5a5' }}>
                <span>${a.price.toFixed(2)}</span>
                <span>{a.quantity}</span>
              </div>
            ))}
            <div style={{ margin: '8px 0', padding: '6px', textAlign: 'center', background: '#1e293b', borderRadius: '4px', fontWeight: 700, color: '#38bdf8', fontSize: '14px' }}>
              Spread: $0.10
            </div>
            <div style={{ fontSize: '12px', fontWeight: 600, color: '#10b981', borderBottom: '1px solid #374151', paddingBottom: '4px' }}>BIDS</div>
            {bids.map((b, i) => (
              <div key={i} style={{ display: 'flex', justifyContent: 'space-between', fontSize: '13px', color: '#86efac' }}>
                <span>${b.price.toFixed(2)}</span>
                <span>{b.quantity}</span>
              </div>
            ))}
          </div>
        </section>
      </div>

      {/* A2UI Copilot Drawer (Accessible dialog) */}
      {isCopilotOpen && (
        <aside
          role="dialog"
          aria-labelledby="copilot-drawer-title"
          aria-modal="true"
          style={{
            position: 'fixed',
            right: 0,
            top: 0,
            bottom: 0,
            width: '380px',
            background: '#0f172a',
            borderLeft: '1px solid #334155',
            padding: '24px',
            boxShadow: '-4px 0 24px rgba(0,0,0,0.5)',
            zIndex: 1000,
            display: 'flex',
            flexDirection: 'column',
            gap: '16px'
          }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <h3 id="copilot-drawer-title" style={{ color: '#38bdf8', fontSize: '18px', margin: 0 }}>AI Trade Copilot (A2UI)</h3>
            <button
              onClick={() => setIsCopilotOpen(false)}
              aria-label="Close Copilot drawer"
              style={{ background: 'transparent', border: 'none', color: '#94a3b8', fontSize: '20px', cursor: 'pointer' }}>
              &times;
            </button>
          </div>

          {copilotLoading ? (
            <div style={{ color: '#cbd5e1', fontSize: '14px' }}>Streaming reasoning tokens over AG-UI...</div>
          ) : proposal && (
            <div style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
              {/* Dynamic A2UI TradeProposalCard */}
              <div style={{ background: '#1e293b', border: '1px solid #475569', borderRadius: '8px', padding: '16px' }}>
                <div style={{ fontSize: '12px', color: '#94a3b8', textTransform: 'uppercase' }}>A2UI Component: TradeProposalCard</div>
                <h4 style={{ color: '#f8fafc', margin: '8px 0' }}>{proposal.side} {proposal.quantity} {proposal.symbol}</h4>
                <div style={{ fontSize: '13px', color: '#cbd5e1', lineHeight: '1.4' }}>{proposal.reasoning}</div>
                <div style={{ marginTop: '12px', display: 'flex', justifyContent: 'space-between', fontSize: '12px' }}>
                  <span style={{ color: '#10b981', fontWeight: 600 }}>Confidence: {(proposal.confidence * 100).toFixed(0)}%</span>
                  <span style={{ color: '#38bdf8', fontWeight: 600 }}>Status: {proposal.status}</span>
                </div>
                <button
                  type="button"
                  onClick={() => {
                    handlePlaceOrder({ preventDefault: () => {} } as any);
                    setIsCopilotOpen(false);
                  }}
                  style={{
                    marginTop: '12px',
                    width: '100%',
                    padding: '10px',
                    background: '#10b981',
                    color: '#ffffff',
                    border: 'none',
                    borderRadius: '4px',
                    fontWeight: 600,
                    cursor: 'pointer'
                  }}>
                  1-Click Execute Proposal
                </button>
              </div>
            </div>
          )}
        </aside>
      )}
    </div>
  );
}
