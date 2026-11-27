Feature: Order Management
  As a trader
  I want to submit orders and check service availability
  So that trading operations are validated and recorded reliably

  Scenario: OMS service reports healthy status
    When I check the OMS service health endpoint
    Then the response status code should be 200

  Scenario: Unauthenticated order submission is rejected
    Given I have an unauthenticated client
    When I submit a limit order for "PAL/USD" at price 150.00 for quantity 100
    Then the response status code should be 401
