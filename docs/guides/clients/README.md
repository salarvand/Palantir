# Frontend Microfrontends & Multi-Zones Guide

The `src/Clients/` workspace ([package.json](../../../src/Clients/package.json)) hosts 6 independent frontend applications combining **Webpack 5 Module Federation** and **Next.js Multi-Zones**, all launched and wired by `Palantir.AppHost`.

---

## 1. Application Topology

| App | Framework | Port | Role | Entry / Config |
| --- | --- | --- | --- | --- |
| `shell` | React 19 + Webpack 5 MF Host | `:3000` | Hosts navigation shell, Keycloak PKCE session indicator, lazy-loads MFEs | [App.tsx](../../../src/Clients/shell/src/App.tsx), [webpack.config.js](../../../src/Clients/shell/webpack.config.js) |
| `trading-desk` | React 19 + Webpack 5 Remote | `:3001` | Order entry, live market ticker, blotter (`tradingDesk/TradingDeskApp`) | [TradingDeskApp.tsx](../../../src/Clients/trading-desk/src/TradingDeskApp.tsx) |
| `portfolio-mfe` | React 19 + Webpack 5 Remote | `:3002` | Positions, margin utilization, risk exposure (`portfolio/PortfolioApp`) | [PortfolioApp.tsx](../../../src/Clients/portfolio-mfe/src/PortfolioApp.tsx) |
| `admin-mfe` | React 19 + Webpack 5 Remote | `:3003` | User & IAM permission administration (`admin/AdminApp`) | [AdminApp.tsx](../../../src/Clients/admin-mfe/src/AdminApp.tsx) |
| `storefront` | Next.js Multi-Zone | `:3100` | Public/retail landing zone, AI Copilot `A2UIRenderer`, rewrites `/desk` & `/backoffice` | [next.config.mjs](../../../src/Clients/storefront/next.config.mjs), [a2ui-renderer.tsx](../../../src/Clients/storefront/components/a2ui-renderer.tsx) |
| `backoffice` | Next.js Multi-Zone | `:3101` | Operations & compliance backoffice zone (`/backoffice`) | [page.tsx](../../../src/Clients/backoffice/app/page.tsx) |

---

## 2. How Module Federation & Multi-Zones Connect

1. `Palantir.AppHost` passes `NEXT_PUBLIC_GATEWAY_URL`, `NEXT_PUBLIC_KEYCLOAK_URL`, `MFE_SHELL_URL`, and `BACKOFFICE_URL` to each frontend process.
2. `shell` mounts `tradingDesk@http://localhost:3001/remoteEntry.js`, `portfolio@http://localhost:3002/remoteEntry.js`, and `admin@http://localhost:3003/remoteEntry.js` via `React.lazy` with fallback alert panels when a remote is offline, sharing `react` and `react-dom` (`^19.0.0`) as eager singletons.
3. `storefront` (`:3100`) uses Next.js `rewrites()` to proxy `/desk/*` to `shell` (`:3000`) and `/backoffice/*` to `backoffice` (`:3101`), giving users a single unified origin.
4. `storefront/components/a2ui-renderer.tsx` renders structured `a2ui_component` payloads (`TradeProposalCard`, `RiskGauge`, `DepthImpactChart`) streamed over SSE from `Palantir.AiAssistantService`.

---

## 3. Accessibility & Frontend Testing

All UIs target **WCAG 2.1 Level AA** ([ACCESSIBILITY.md](../../ACCESSIBILITY.md)): semantic landmarks (`banner`, `navigation`, `main`, `dialog`), `aria-live="polite"` updates, high contrast (`>= 4.5:1`), and visible focus rings.

```bash
cd src/Clients
npm test       # Runs Vitest + @testing-library/react across all 6 workspaces
npm run test:e2e # Runs playwright-bdd + @axe-core/playwright for tests/bdd/*.feature
```
