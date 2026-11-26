import path from 'node:path';
import { defineConfig } from 'vitest/config';

export default defineConfig({
  resolve: {
    alias: {
      'tradingDesk/TradingDeskApp': path.resolve(__dirname, '../trading-desk/src/TradingDeskApp.tsx'),
      'portfolio/PortfolioApp': path.resolve(__dirname, '../portfolio-mfe/src/PortfolioApp.tsx'),
      'admin/AdminApp': path.resolve(__dirname, '../admin-mfe/src/AdminApp.tsx'),
    },
  },
  test: {
    environment: 'jsdom',
  },
});
