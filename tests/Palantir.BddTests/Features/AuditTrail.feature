Feature: Audit Trail
  As a compliance officer
  I want the Audit service to be healthy and secured
  So that regulatory audit logs are protected from unauthorized access

  Scenario: Audit service reports healthy status
    When I check the Audit service health endpoint
    Then the audit response status code should be 200

  Scenario: Unauthenticated audit log query is rejected
    When I query the audit logs endpoint without credentials
    Then the audit response status code should be 401
