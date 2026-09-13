# OrderFlow

A .NET 9 order management API demonstrating **Clean Architecture**, **Vertical Slice Features**, and **CQRS with MediatR**, backed by SQL Server 2022 (EF Core for the transactional write model, Dapper for fast read models), Redis caching, and background processing.

---

## 1. Architecture Overview

### Clean Architecture

The solution is layered with dependencies pointing strictly inward:

```
┌─────────────────────────────────────────────┐
│                OrderFlow.Api                │  ← Controllers, exception handlers, Program.cs
├─────────────────────────────────────────────┤
│           OrderFlow.Infrastructure          │  ← EF Core DbContext, Dapper factory, Redis,
│  (implements Application abstractions)      │    background jobs, migrations
├─────────────────────────────────────────────┤
│           OrderFlow.Application             │  ← Vertical slice features, CQRS handlers,
│  (depends only on Domain + abstractions)    │    pipeline behaviors, interfaces
├─────────────────────────────────────────────┤
│              OrderFlow.Domain               │  ← Order aggregate, OrderItem, OrderStatus
│           (no dependencies at all)          │    (entities encapsulate all invariants)
└─────────────────────────────────────────────┘
```

- **Domain** — the `Order` aggregate root. State changes go through guarded methods (`Order.Create`, `OrderItem.Create`, `Order.MarkCompleted`); no property is publicly settable.
- **Application** — contains **no implementation details**. It declares the abstractions (`IApplicationDbContext`, `ISqlConnectionFactory`, `ICacheService`) that Infrastructure implements, plus all MediatR features.
- **Infrastructure** — EF Core (`ApplicationDbContext`), Dapper (`SqlConnectionFactory`), Redis (`RedisCacheService`), and two `BackgroundService` jobs.
- **Api** — thin controllers that dispatch commands/queries via `ISender` and translate results into HTTP responses.

### Vertical Slice Organization

Each feature is a self-contained slice under `Features/Orders` — command/query, validator, handler, and DTOs live together, not in separate repository/service layers:

```
src/OrderFlow.Application/Features/Orders/
├── Commands/
│   └── CreateOrder/          CreateOrderCommand + Validator + Handler
└── Queries/
    ├── GetOrderById/         Cache-aside handler (Redis → Dapper → cache)
    ├── GetOrdersList/        Lightweight Dapper list query
    └── GetDashboardOrders/   Reads the OrdersDashboard read-model table
```

### CQRS: Write Model vs. Read Model

| Concern | Write model | Read model |
|---|---|---|
| Data access | EF Core (`ApplicationDbContext`) via Dapper (`ISqlConnectionFactory`) |
| Operations | `CreateOrderCommand` (and background status updates) | All queries (`GetOrderById`, `GetOrdersList`, `GetDashboardOrders`) |
| Storage | `[Orders]` + `[OrderItems]` (source of truth) | `[OrdersDashboard]` read-optimized table |
| Reasoning | Full aggregate, transactions, domain invariants | Flattened, denormalized, projection-friendly rows |

Every request flows through the MediatR pipeline:

```
Request → LoggingBehavior → ValidationBehavior (FluentValidation) → Handler
```

- **Writes** hydrate and persist the domain aggregate through EF Core.
- **Reads** bypass EF Core entirely and use Dapper with hand-written SQL — one `QueryMultipleAsync` round trip for `GetOrderById`, a grouped join for the list, and a direct read of the dashboard table.
- **Caching** uses the cache-aside pattern: `GetOrderById` checks Redis first (5-minute TTL); on a miss it queries SQL Server and populates the cache. `CreateOrder` invalidates the list and dashboard cache keys after committing.

---

## 2. Tech Stack

| Component | Technology | Version |
|---|---|---|
| Runtime & language | .NET / C# | 9 |
| Write-model ORM | Microsoft.EntityFrameworkCore (SqlServer) | 9.0.x |
| Read-model micro-ORM | Dapper | 2.1.x |
| Mediation | MediatR | 12.5 |
| Validation | FluentValidation | 12.1 |
| Cache | Redis via `IDistributedCache` (StackExchange.Redis) + System.Text.Json | — |
| Database | SQL Server 2022 (`mcr.microsoft.com/mssql/server:2022-latest`) | 2022 |
| Cache server | Redis (`redis:alpine`) | — |
| API docs | Swashbuckle (Swagger UI) | 9.0 |
| Containers | Docker Compose | — |

---

## 3. How to Run

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- Docker (with Compose) for SQL Server and Redis

### 1) Start the infrastructure

```bash
docker compose up -d
```

This starts:
- **SQL Server 2022** on `localhost:1433` (`sa` / `YourStrong!Passw0rd`)
- **Redis** on `localhost:6379`

Wait for both containers to report healthy:

```bash
docker compose ps
```

### 2) Run the API

```bash
dotnet run --project src/OrderFlow.Api
```

The API listens on `http://localhost:5120` (Development profile).

### Migrations

Migrations are **applied automatically on startup** for evaluation convenience: `Program.cs` checks `Database.GetPendingMigrationsAsync()` and runs `MigrateAsync()` only when needed, logging the outcome.

To manage migrations manually:

```bash
# Apply pending migrations
dotnet ef database update --project src/OrderFlow.Infrastructure --startup-project src/OrderFlow.Api

# Add a new migration after changing the model
dotnet ef migrations add <Name> --project src/OrderFlow.Infrastructure --startup-project src/OrderFlow.Api --output-dir Persistence/Migrations
```

Connection strings live in `src/OrderFlow.Api/appsettings.json` (`DefaultConnection`, `Redis`) and already point at the docker-compose services.

---

## 4. Background Jobs

Both jobs are .NET `BackgroundService` implementations registered via `AddHostedService` in `OrderFlow.Infrastructure.DependencyInjection`. Each runs its own `PeriodicTimer` loop, resolves **scoped** dependencies through `IServiceScopeFactory` (a new scope per tick, since hosted services are singletons), isolates every tick in a try/catch so one failure never kills the loop, and honors the `CancellationToken` for graceful shutdown.

### PendingOrderProcessorJob

**Purpose** — simulates order fulfillment by transitioning newly created orders from `Pending` to `Completed`.

| Aspect | Detail |
|---|---|
| Interval | Every 15 seconds |
| Scope | Batch of up to 100 `Pending` orders per tick |
| Flow | Query pending orders via EF Core → `order.MarkCompleted()` (domain method with invalid-transition guard) → single `SaveChangesAsync` → log count |

After it runs, `GET /api/orders` shows the new `Completed` status within ~15 seconds of order creation.

### DashboardMaterializedViewRefresherJob

**Purpose** — keeps the `[OrdersDashboard]` read-optimized table synchronized with the transactional `[Orders]` table, so the dashboard endpoint never has to join or aggregate on the fly.

| Aspect | Detail |
|---|---|
| Interval | Every 30 seconds |
| Flow | Load `Orders` (with item counts, `AsNoTracking`) → load existing dashboard rows → **diff**: insert missing rows, update only rows whose data actually changed (field-level comparison so `LastUpdatedUtc` doesn't churn), delete stale rows → single atomic `SaveChangesAsync` |
| Logging | Skips writing entirely (Debug log) when nothing changed; otherwise logs added / updated / removed counts |

`[OrdersDashboard]` is explicitly **not the source of truth** — it is a materialized projection rebuilt from `Orders` and safe to drop and regenerate at any time.

---

## 5. Testing the Endpoints

### Swagger UI

With the API running in Development, open:

```
http://localhost:5120/swagger
```

All four endpoints are documented and directly executable.

### cURL

**Create an order** — returns `201 Created` with a `Location` header pointing at the new resource:

```bash
curl -i -X POST http://localhost:5120/api/orders \
  -H "Content-Type: application/json" \
  -d '{
    "customerName": "Ada Lovelace",
    "items": [
      { "productName": "Mechanical Keyboard", "quantity": 1, "unitPrice": 129.99 },
      { "productName": "USB-C Cable",        "quantity": 3, "unitPrice": 9.50 }
    ]
  }'
```

Response:

```json
{ "id": "3f21944c-9e1a-4c72-9a58-0f7dbb9f31d4" }
```

**Validation failure** — invalid payloads return `400` with a ProblemDetails body, courtesy of the FluentValidation pipeline behavior:

```bash
curl -s http://localhost:5120/api/orders \
  -H "Content-Type: application/json" \
  -X POST \
  -d '{ "customerName": "", "items": [] }' | jq
```

```json
{
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "CustomerName": ["Customer name is required."],
    "Items": ["An order must contain at least one item."]
  }
}
```

**Get an order by id** — cache-aside: the first call hits SQL Server and populates Redis (5-minute TTL); subsequent calls are served from cache:

```bash
curl -s http://localhost:5120/api/orders/3f21944c-9e1a-4c72-9a58-0f7dbb9f31d4 | jq
```

A missing id returns `404`:

```bash
curl -i http://localhost:5120/api/orders/00000000-0000-0000-0000-000000000000
```

**List all orders** — lightweight summary with per-order item counts:

```bash
curl -s http://localhost:5120/api/orders | jq
```

**Dashboard orders** — served from the `OrdersDashboard` materialized table (refreshed by the background job every 30 seconds):

```bash
curl -s http://localhost:5120/api/dashboard/orders | jq
```

### End-to-end flow to observe

1. `POST /api/orders` → order created with status `Pending`.
2. `GET /api/orders` → status `Pending` (write model, read immediately).
3. Wait ~15–20 seconds (`PendingOrderProcessorJob`) → status becomes `Completed`.
4. Wait up to ~30 more seconds (`DashboardMaterializedViewRefresherJob`) → `GET /api/dashboard/orders` reflects the new order and its `Completed` status.

### Endpoint summary

| Method | Route | Query / Command | Response |
|---|---|---|---|
| POST | `/api/orders` | `CreateOrderCommand` | `201 Created` + `{ id }` |
| GET | `/api/orders/{id}` | `GetOrderByIdQuery` | `200` `OrderDetailsDto` / `404` |
| GET | `/api/orders` | `GetOrdersListQuery` | `200` `List<OrderSummaryDto>` |
| GET | `/api/dashboard/orders` | `GetDashboardOrdersQuery` | `200` `List<OrderDashboardReadModel>` |

All error responses (400 / 404 / 500) use RFC 9457 ProblemDetails, produced by the .NET 9 `IExceptionHandler` pipeline in `OrderFlow.Api/Middleware`.
