module.exports = {
  title: 'Palantir Event & Domain Catalog',
  tagline: 'Interactive enterprise architecture, message schemas, and flow graphs for Palantir Trading Platform',
  organizationName: 'Palantir Systems',
  homepageLink: 'https://github.com/palantir/trading-platform',
  editUrl: 'https://github.com/palantir/trading-platform/edit/main',
  primaryColor: '#0ea5e9',
  domains: [
    {
      id: 'OrderManagement',
      name: 'Order Management',
      version: '1.0.0',
      summary: 'Order lifecycle management, validation, Marten event sourcing, and blotter projections.',
    },
    {
      id: 'MarketExecution',
      name: 'Market Execution',
      version: '1.0.0',
      summary: 'Sub-millisecond L2/L3 in-memory order book matching engine and TimescaleDB tick aggregation.',
    },
    {
      id: 'Settlement',
      name: 'Post-Trade Settlement',
      version: '1.0.0',
      summary: 'Distributed MassTransit state machine saga with compensation.',
    },
    {
      id: 'RiskGovernance',
      name: 'Risk & Governance',
      version: '1.0.0',
      summary: 'Pre-trade margin calculation, concentration limits, and AI agent policy enforcement.',
    }
  ],
  services: [
    { id: 'OmsService', name: 'Palantir.OmsService', version: '1.0.0' },
    { id: 'LocalEngineService', name: 'Palantir.LocalEngineService', version: '1.0.0' },
    { id: 'SettlementService', name: 'Palantir.SettlementService', version: '1.0.0' },
    { id: 'AiAssistantService', name: 'Palantir.AiAssistantService', version: '1.0.0' }
  ]
};
