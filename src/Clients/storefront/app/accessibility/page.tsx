import React from 'react';

export default function AccessibilityStatementPage() {
  return (
    <main id="main-content" role="main" style={{ padding: '24px', maxWidth: '800px', margin: '0 auto' }}>
      <h1>Accessibility Statement (WCAG 2.1 AA)</h1>
      <p>
        Palantir Trading Platform targets full conformance with Web Content Accessibility Guidelines (WCAG) 2.1 Level AA
        across the Next.js Multi-Zone Storefront, Admin Backoffice, and Webpack 5 Module Federation microfrontends.
      </p>
      <ul>
        <li>Semantic HTML5 landmarks (banner, navigation, main) and skip-navigation links.</li>
        <li>ARIA live regions for order execution updates and AI Copilot A2UI cards.</li>
        <li>Full keyboard operability and 4.5:1 minimum text contrast ratios.</li>
      </ul>
    </main>
  );
}
