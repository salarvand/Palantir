workspace "Palantir Trading Platform" "C4 architecture model for the .NET 10 + Aspire cloud-native trading platform." {

    model {
        trader = person "Trader" "Places orders, monitors blotter, and interacts with AI Copilot."
        riskOfficer = person "Risk Officer" "Configures exposure limits, halts trading, and reviews alerts."
        compliance = person "Compliance Auditor" "Queries and exports immutable regulatory audit records."
        admin = person "Platform Admin" "Manages users, roles, and dimension-resource-context permissions."

        keycloak = softwareSystem "Keycloak 26" "OIDC/PKCE identity provider and RFC 8693 token exchange server." "External"
        externalVenue = softwareSystem "External Liquidity Venue" "External exchange execution endpoint." "External"

        palantir = softwareSystem "Palantir Platform" "Enterprise .NET 10 + Aspire trading, risk, settlement, and audit platform." {
            frontend = container "Microfrontends & Multi-Zones" "Webpack 5 Module Federation (:3000-:3003) + Next.js Multi-Zones (:3100-:3101)." "TypeScript / React / Next.js"
            gateway = container "Palantir.Gateway" "YARP reverse proxy with Aspire service discovery and JWT validation." ".NET 10 / YARP"

            oms = container "Palantir.OmsService" "Order lifecycle management, validation specs, and blotter projections." ".NET 10 / Orleans / Marten" {
                omsEndpoint = component "CreateOrderEndpoint" "Vertical slice HTTP endpoint with FluentValidation + SpecificationValidator." "ASP.NET Minimal API"
                omsHandler = component "CreateOrderHandler" "Evaluates ValidationSpecification rules and emits non-blocking AuditEntry." "Wolverine Handler"
                orderGrain = component "OrderGrain" "Single-threaded virtual actor serializing order state mutations." "Microsoft Orleans"
                omsRepo = component "MartenOrderRepository" "Appends order events and updates OrderBlotterProjection." "Marten Event Store"
            }

            risk = container "Palantir.RiskService" "Pre-trade margin and notional risk engine." ".NET 10 / Wolverine"
            portfolio = container "Palantir.PortfolioService" "Account balances, margin holds, and positions." ".NET 10 / EF Core"
            settlement = container "Palantir.SettlementService" "Distributed order settlement saga orchestrator." ".NET 10 / Wolverine Saga"
            marketData = container "Palantir.MarketDataService" "Real-time quotes and L2 order book streaming." ".NET 10 / SignalR"
            iam = container "Palantir.IamService" "User profiles, permission token caching, and Keycloak sync." ".NET 10 / EF Core"
            audit = container "Palantir.AuditService" "Regulatory audit store and multi-sink audit pipeline." ".NET 10 / EF Core"
            ai = container "Palantir.AiAssistantService" "Multi-agent trading copilot (MCP, A2A, AG-UI, A2UI)." ".NET 10 / Microsoft.Extensions.AI"
            localEngine = container "Palantir.LocalEngineService" "Internal matching engine over HTTP/2 gRPC." ".NET 10 / gRPC"
            extGateway = container "Palantir.ExternalGatewayService" "External venue execution adapter with RFC 8693 token exchange." ".NET 10 / HTTP"

            postgres = container "PostgreSQL 16 + pgvector" "Schemas per service, Marten streams, Orleans storage, Quartz jobs, platform_audit." "PostgreSQL" "Database"
            rabbitmq = container "RabbitMQ" "Durable command/event broker with Wolverine outbox/inbox and DLQ." "RabbitMQ" "Queue"
        }

        trader -> frontend "Uses" "HTTPS"
        riskOfficer -> frontend "Monitors" "HTTPS"
        compliance -> frontend "Audits" "HTTPS"
        admin -> frontend "Administers" "HTTPS"

        frontend -> keycloak "Authenticates (PKCE)" "OIDC"
        frontend -> gateway "Calls APIs" "HTTPS / JWT"
        gateway -> oms "Routes /orders" "HTTP"
        gateway -> risk "Routes /risk" "HTTP"
        gateway -> portfolio "Routes /portfolio" "HTTP"
        gateway -> marketData "Routes /market" "HTTP / WS"
        gateway -> iam "Routes /iam" "HTTP"
        gateway -> audit "Routes /audit" "HTTP"
        gateway -> ai "Routes /ai" "HTTP / SSE"

        oms -> localEngine "Dispatches local orders" "gRPC HTTP/2"
        oms -> extGateway "Routes external orders" "HTTP + RFC 8693"
        extGateway -> externalVenue "Submits executions" "HTTPS"

        omsEndpoint -> omsHandler "Dispatches command"
        omsHandler -> orderGrain "Invokes actor"
        orderGrain -> omsRepo "Persists events"
        omsRepo -> postgres "Writes event stream"

        oms -> rabbitmq "Publishes commands/events" "AMQP"
        risk -> rabbitmq "Consumes/publishes risk events" "AMQP"
        portfolio -> rabbitmq "Updates margin" "AMQP"
        settlement -> rabbitmq "Orchestrates sagas" "AMQP"
        audit -> rabbitmq "Consumes audit.trail.v1" "AMQP"

        oms -> postgres "Reads/writes" "Npgsql"
        risk -> postgres "Reads/writes" "Npgsql"
        portfolio -> postgres "Reads/writes" "Npgsql"
        settlement -> postgres "Reads/writes" "Npgsql"
        iam -> postgres "Reads/writes" "Npgsql"
        audit -> postgres "Writes audit records" "Npgsql COPY"
        iam -> keycloak "Syncs users & tokens" "REST / RFC 8693"
    }

    views {
        systemContext palantir "SystemContext" {
            include *
            autolayout lr
        }

        container palantir "Containers" {
            include *
            autolayout tb
        }

        component oms "OmsComponents" {
            include *
            autolayout lr
        }

        styles {
            element "Person" {
                shape Person
                background #08427b
                color #ffffff
            }
            element "Software System" {
                background #1168bd
                color #ffffff
            }
            element "External" {
                background #999999
                color #ffffff
            }
            element "Database" {
                shape Cylinder
            }
            element "Queue" {
                shape Pipe
            }
        }
    }
}
