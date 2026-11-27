Feature: Risk Validation
  As a risk officer
  I want the Risk service to remain healthy and enforce authentication
  So that pre-trade risk limits are protected

  Scenario: Risk service reports healthy status
    When I check the Risk service health endpoint
    Then the risk response status code should be 200

  Scenario: Unauthenticated risk validation call is rejected
    When I request risk validation for "PAL/USD" with quantity 50 and price 100.00
    Then the risk response status code should be 401
