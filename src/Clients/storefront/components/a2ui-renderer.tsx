import React from 'react';

export interface TradeProposalCardPayload {
  ProposalId: string;
  Symbol: string;
  Side: string;
  Quantity: number;
  Price: number;
  ExecutionStatus: string;
  Confidence: number;
  ReasoningNote: string;
}

export interface RiskGaugePayload {
  CurrentMarginPercent: number;
  ProjectedMarginPercent: number;
  RiskTier: string;
  WithinRiskTolerance: boolean;
}

export function A2UIRenderer({
  proposal,
  riskGauge,
}: {
  proposal?: TradeProposalCardPayload | null;
  riskGauge?: RiskGaugePayload | null;
}) {
  if (!proposal && !riskGauge) return null;

  return (
    <section aria-label="AI Copilot Interactive A2UI Surface" style={{ display: 'grid', gap: '16px', marginTop: '16px' }}>
      {proposal && (
        <article
          aria-label={`Trade Proposal ${proposal.Symbol}`}
          style={{ padding: '16px', background: '#111827', borderRadius: '8px', border: '1px solid #38bdf8' }}
        >
          <h3 style={{ margin: '0 0 8px 0', color: '#38bdf8' }}>
            A2UI Proposal: {proposal.Side} {proposal.Quantity} {proposal.Symbol} @ ${proposal.Price.toFixed(2)}
          </h3>
          <p style={{ margin: '0 0 8px 0', color: '#e2e8f0' }}>{proposal.ReasoningNote}</p>
          <div style={{ fontSize: '13px', color: '#94a3b8' }}>
            Status: <strong>{proposal.ExecutionStatus}</strong> &bull; Confidence:{' '}
            <strong>{Math.round(proposal.Confidence * 100)}%</strong>
          </div>
        </article>
      )}
      {riskGauge && (
        <article
          aria-label="Pre-Trade Margin Risk Gauge"
          style={{ padding: '16px', background: '#111827', borderRadius: '8px', border: '1px solid #1f2937' }}
        >
          <h3 style={{ margin: '0 0 8px 0', color: '#f8fafc' }}>
            Risk Tier: {riskGauge.RiskTier} ({riskGauge.WithinRiskTolerance ? 'APPROVED' : 'BLOCKED'})
          </h3>
          <div
            role="progressbar"
            aria-valuenow={Math.round(riskGauge.ProjectedMarginPercent)}
            aria-valuemin={0}
            aria-valuemax={100}
            aria-label="Projected Margin Utilization"
            style={{ width: '100%', height: '10px', background: '#1e293b', borderRadius: '5px', overflow: 'hidden' }}
          >
            <div
              style={{
                width: `${Math.min(100, riskGauge.ProjectedMarginPercent)}%`,
                height: '100%',
                background: riskGauge.WithinRiskTolerance ? '#10b981' : '#ef4444',
              }}
            />
          </div>
        </article>
      )}
    </section>
  );
}
