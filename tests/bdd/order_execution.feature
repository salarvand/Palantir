@order_execution
Feature: High-Speed Order Placement and Matching Flow
  As a verified institutional trader
  I want to submit limit orders to the in-memory matching engine
  So that my orders are matched in sub-millisecond time and settled safely via sagas

  Background:
    Given The Palantir trading platform infrastructure is healthy
    And Trader "trader1" has an active Keycloak session with "Permissions.AllowPlaceOrder"
    And Symbol "PAL/USD" is active in the order book

  @smoke @e2e
  Scenario: Limit Buy Order executes against existing resting Ask
    Given The order book for "PAL/USD" has a resting sell order of 100 shares at 150.25
    When Trader submits a Limit Buy order for 100 shares of "PAL/USD" at 150.25
    Then The matching engine should execute the match over HTTP/2 gRPC
    And The execution report status should be "Filled"
    And An execution tick should be published to RabbitMQ exchange "market.data" (queue "market.ticks.v1")
    And The order blotter view should display status "Filled" with average price 150.25
    And The Wolverine settlement saga should transition to "Settled"
