import path from 'node:path';
import url from 'node:url';

const __dirname = path.dirname(url.fileURLToPath(import.meta.url));

/** @type {import('@eventcatalog/core/bin/eventcatalog.config').Config} */
export default {
  title: 'Palantir Event & Domain Catalog',
  tagline: 'Centralized event-driven, OpenAPI, and AsyncAPI architecture catalog for Palantir Trading Platform',
  organizationName: 'Palantir Systems',
  homepageLink: 'https://github.com/palantir/trading-platform',
  editUrl: 'https://github.com/palantir/trading-platform/edit/main',
  trailingSlash: false,
  base: '/',
  generators: [
    [
      '@eventcatalog/generator-asyncapi',
      {
        services: [
          { path: path.join(__dirname, '..', 'asyncapi', 'order-events-asyncapi.yaml'), id: 'OmsService' },
          { path: path.join(__dirname, '..', 'asyncapi', 'market-ticks-asyncapi.yaml'), id: 'LocalEngineService' },
          { path: path.join(__dirname, '..', 'asyncapi', 'settlement-commands-asyncapi.yaml'), id: 'SettlementService' },
        ],
        domain: { id: 'OrderManagement', name: 'Order Management & Settlement', version: '1.0.0' },
      },
    ],
  ],
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
      summary: 'Sub-millisecond L2/L3 in-memory order book matching engine over gRPC and tick streaming.',
    },
    {
      id: 'Settlement',
      name: 'Post-Trade Settlement',
      version: '1.0.0',
      summary: 'Distributed Wolverine saga persisted in PostgreSQL with margin compensation over RabbitMQ.',
    },
    {
      id: 'RiskGovernance',
      name: 'Risk & AI Governance',
      version: '1.0.0',
      summary: 'Pre-trade margin validation, regulatory audit trails, and YAML policy-driven AI agent controls.',
    },
  ],
  services: [
    { id: 'OmsService', name: 'Palantir.OmsService', version: '1.0.0' },
    { id: 'LocalEngineService', name: 'Palantir.LocalEngineService', version: '1.0.0' },
    { id: 'ExternalGatewayService', name: 'Palantir.ExternalGatewayService', version: '1.0.0' },
    { id: 'MarketDataService', name: 'Palantir.MarketDataService', version: '1.0.0' },
    { id: 'RiskService', name: 'Palantir.RiskService', version: '1.0.0' },
    { id: 'PortfolioService', name: 'Palantir.PortfolioService', version: '1.0.0' },
    { id: 'SettlementService', name: 'Palantir.SettlementService', version: '1.0.0' },
    { id: 'AuditService', name: 'Palantir.AuditService', version: '1.0.0' },
    { id: 'IamService', name: 'Palantir.IamService', version: '1.0.0' },
    { id: 'AiAssistantService', name: 'Palantir.AiAssistantService', version: '1.0.0' },
  ],
};
