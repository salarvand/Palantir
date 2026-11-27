# ADR 0005: Zero-Trust AuthN & RFC 8693 Token Exchange

## Status
Accepted

## Context
Microservices must never blindly trust unverified downstream calls or use coarse-grained service accounts that lose user identity.

## Decision
Authenticate clients via Keycloak OIDC/PKCE, validate JWTs per service, exchange subject tokens per target audience via RFC 8693 (`KeycloakTokenExchangeService`), and authorize requests using the Dimension-Resource-Context `PermissionEvaluator`.

## Consequences
- Downstream services verify audience-scoped tokens while `PermissionEvaluator` evaluates contextual specifications (network/VPN, market state, ownership).
