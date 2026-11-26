# Palantir Accessibility Statement & Conformance Report

**Conformance Target**: Web Content Accessibility Guidelines (WCAG) 2.1 Level AA  
**Evaluation Scope**: Next.js Multi-Zones (`storefront` `:3100`, `backoffice` `:3101`) and Webpack 5 Module Federation Microfrontends (`shell` `:3000`, `trading-desk` `:3001`, `portfolio-mfe` `:3002`, `admin-mfe` `:3003`).

---

## 1. Executive Summary

Palantir Trading Platform microfrontends are designed and implemented to provide an accessible, high-contrast, fully keyboard-navigable trading workstation for institutional and retail operators, meeting and exceeding **WCAG 2.1 Level AA** standards.

---

## 2. Accessibility Invariants & Core Technical Protections

### 2.1 Color Contrast Ratios (WCAG 1.4.3 & 1.4.11)
- **Normal Prose Text**: All body copy and tabular values maintain a minimum contrast ratio of **4.5:1** against adjacent dark backgrounds (`#090d16` and `#111827`).
- **Interactive Controls & Icons**: Buttons, active tab indicators, and status badges maintain a minimum contrast ratio of **3:1** against adjacent surfaces.
- **Color Independence (WCAG 1.4.1)**: Financial directional data (Buy/Sell, Gain/Loss) supplements color coding with unambiguous textual indicators (`BUY` / `SELL`, `+` / `-`).

### 2.2 Keyboard Navigation & Focus Indicators (WCAG 2.1.1 & 2.4.7)
- **Logical Tab Order**: All inputs, buttons, and dialogs follow the natural DOM reading sequence.
- **Focus Rings**: Interactive elements provide a visible 2px solid cyan focus ring (`#38bdf8`) with zero focus suppression (`outline: none` without replacement is strictly prohibited).
- **Dialog Containment**: The AI Copilot modal drawer (`role="dialog"`, `aria-modal="true"`) provides keyboard dismissal (`Escape` key and accessible close button).

### 2.3 Screen Reader Announcements & ARIA Landmarks (WCAG 4.1.2 & 4.1.3)
- **Landmarks**: Distinct landmarks (`role="banner"`, `role="navigation"`, `role="main"`, `role="dialog"`) segment the interface.
- **Dynamic Updates**: Real-time market tick notifications, order submission confirmations, and Copilot reasoning tokens are announced unobtrusively using `aria-live="polite"` regions.
- **Form Controls**: All inputs have explicit `<label>` elements linked via `htmlFor`/`id` pairings and `aria-describedby` error bindings.

---

## 3. Automated & Manual Verification Procedure

Automated accessibility testing is executed in CI via Vitest + Testing Library and Playwright BDD (`@axe-core/playwright`):
```bash
cd src/Clients
npm test
npm run test:e2e
```
