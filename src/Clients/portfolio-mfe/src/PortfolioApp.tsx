import React, { useState } from 'react';

interface Position {
  symbol: string;
  side: 'LONG' | 'SHORT';
  quantity: number;
  entryPrice: number;
  markPrice: number;
  unrealizedPnL: number;
  returnOnEquity: number;
}

export default function PortfolioApp() {
  const [totalEquity] = useState(125400.50);
  const [cashBalance] = useState(85000.00);
  const [reservedMargin] = useState(24500.00);
  const [availableMargin] = useState(60500.00);
  const [marginUtilization] = useState(28.8); // 28.8%

  const [positions] = useState<Position[]>([
    { symbol: 'PAL/USD', side: 'LONG', quantity: 500, entryPrice: 148.20, markPrice: 150.25, unrealizedPnL: 1025.00, returnOnEquity: 6.92 },
    { symbol: 'ETH/USD', side: 'LONG', quantity: 15, entryPrice: 3450.00, markPrice: 3520.00, unrealizedPnL: 1050.00, returnOnEquity: 2.03 },
    { symbol: 'BTC/USD', side: 'SHORT', quantity: 0.5, entryPrice: 96500.00, markPrice: 95800.00, unrealizedPnL: 350.00, returnOnEquity: 0.73 }
  ]);

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '20px', width: '100%' }}>
      {/* 1. Account Summary Bar */}
      <section aria-labelledby="portfolio-summary-heading" style={{ background: '#111827', borderRadius: '8px', padding: '20px', border: '1px solid #1f2937' }}>
        <h2 id="portfolio-summary-heading" style={{ fontSize: '18px', fontWeight: 600, color: '#f8fafc', margin: '0 0 16px 0' }}>Portfolio & Risk Summary</h2>
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: '16px' }}>
          <div style={{ background: '#090d16', padding: '16px', borderRadius: '6px', border: '1px solid #1e293b' }}>
            <div style={{ fontSize: '12px', color: '#94a3b8', textTransform: 'uppercase' }}>Total Net Equity</div>
            <div style={{ fontSize: '24px', fontWeight: 700, color: '#f8fafc', marginTop: '4px' }}>${totalEquity.toLocaleString('en-US', { minimumFractionDigits: 2 })}</div>
          </div>

          <div style={{ background: '#090d16', padding: '16px', borderRadius: '6px', border: '1px solid #1e293b' }}>
            <div style={{ fontSize: '12px', color: '#94a3b8', textTransform: 'uppercase' }}>Cash Balance</div>
            <div style={{ fontSize: '24px', fontWeight: 700, color: '#38bdf8', marginTop: '4px' }}>${cashBalance.toLocaleString('en-US', { minimumFractionDigits: 2 })}</div>
          </div>

          <div style={{ background: '#090d16', padding: '16px', borderRadius: '6px', border: '1px solid #1e293b' }}>
            <div style={{ fontSize: '12px', color: '#94a3b8', textTransform: 'uppercase' }}>Reserved Margin (Sagas)</div>
            <div style={{ fontSize: '24px', fontWeight: 700, color: '#fbbf24', marginTop: '4px' }}>${reservedMargin.toLocaleString('en-US', { minimumFractionDigits: 2 })}</div>
          </div>

          <div style={{ background: '#090d16', padding: '16px', borderRadius: '6px', border: '1px solid #1e293b' }}>
            <div style={{ fontSize: '12px', color: '#94a3b8', textTransform: 'uppercase' }}>Available Free Margin</div>
            <div style={{ fontSize: '24px', fontWeight: 700, color: '#10b981', marginTop: '4px' }}>${availableMargin.toLocaleString('en-US', { minimumFractionDigits: 2 })}</div>
          </div>
        </div>

        {/* Margin Meter Progress Bar (WCAG 2.1 AA compliant) */}
        <div style={{ marginTop: '20px' }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '13px', marginBottom: '6px', color: '#cbd5e1' }}>
            <span>Portfolio Margin Utilization: <strong>{marginUtilization}%</strong></span>
            <span>Risk Limit: <strong>85.0% Max</strong></span>
          </div>
          <div
            role="progressbar"
            aria-valuenow={marginUtilization}
            aria-valuemin={0}
            aria-valuemax={100}
            aria-label="Margin Utilization"
            style={{ width: '100%', height: '10px', background: '#1e293b', borderRadius: '5px', overflow: 'hidden' }}>
            <div style={{ width: `${marginUtilization}%`, height: '100%', background: marginUtilization > 80 ? '#ef4444' : '#10b981' }} />
          </div>
        </div>
      </section>

      {/* 2. Real-Time Positions Grid */}
      <section aria-labelledby="positions-heading" style={{ background: '#111827', borderRadius: '8px', padding: '20px', border: '1px solid #1f2937' }}>
        <h2 id="positions-heading" style={{ fontSize: '18px', fontWeight: 600, color: '#f8fafc', margin: '0 0 16px 0' }}>Open Positions (Mark-to-Market)</h2>
        <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left', fontSize: '13px' }}>
          <thead>
            <tr style={{ color: '#94a3b8', borderBottom: '1px solid #1f2937' }}>
              <th style={{ padding: '12px' }}>Symbol</th>
              <th style={{ padding: '12px' }}>Side</th>
              <th style={{ padding: '12px' }}>Position Size</th>
              <th style={{ padding: '12px' }}>Entry Price</th>
              <th style={{ padding: '12px' }}>Mark Price</th>
              <th style={{ padding: '12px' }}>Unrealized PnL ($)</th>
              <th style={{ padding: '12px' }}>ROE (%)</th>
            </tr>
          </thead>
          <tbody>
            {positions.map(p => (
              <tr key={p.symbol} style={{ borderBottom: '1px solid #1e293b', color: '#f8fafc' }}>
                <td style={{ padding: '12px', fontWeight: 600 }}>{p.symbol}</td>
                <td style={{ padding: '12px', color: p.side === 'LONG' ? '#10b981' : '#ef4444', fontWeight: 600 }}>{p.side}</td>
                <td style={{ padding: '12px' }}>{p.quantity}</td>
                <td style={{ padding: '12px' }}>${p.entryPrice.toFixed(2)}</td>
                <td style={{ padding: '12px' }}>${p.markPrice.toFixed(2)}</td>
                <td style={{ padding: '12px', color: p.unrealizedPnL >= 0 ? '#10b981' : '#ef4444', fontWeight: 600 }}>
                  {p.unrealizedPnL >= 0 ? '+' : ''}${p.unrealizedPnL.toFixed(2)}
                </td>
                <td style={{ padding: '12px', color: p.returnOnEquity >= 0 ? '#10b981' : '#ef4444', fontWeight: 600 }}>
                  {p.returnOnEquity >= 0 ? '+' : ''}{p.returnOnEquity.toFixed(2)}%
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </section>
    </div>
  );
}
