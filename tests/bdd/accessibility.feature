@accessibility @wcag
Feature: WCAG 2.1 Level AA Workstation Accessibility
  As an institutional trader using assistive technologies
  I want all trading station views and controls to conform to WCAG 2.1 Level AA
  So that I can monitor markets, navigate tables, and execute trades without barriers

  Scenario Outline: Accessibility compliance across microfrontends
    Given Trader navigates to the "<ViewName>" microfrontend view
    When Automated axe-core accessibility audit scans the full DOM tree
    Then Zero violations of WCAG 2.1 Level AA should be reported
    And All interactive elements must maintain a contrast ratio of at least 4.5 to 1
    And Focus rings must be visible when navigating via Tab key
    And Real-time ticker updates must announce via "aria-live" polite regions

    Examples:
      | ViewName          | Port |
      | Host Shell        | 3000 |
      | Trading Desk MFE  | 3001 |
      | Portfolio & Risk  | 3002 |
      | IAM & Admin MFE   | 3003 |
