# Fulfillment and Inventory Management Platform

## Project structure

The solution file is `Fulfillment.slnx`. It groups four projects under `src/` and one under `tests/`.

| Project | Contents |
| --- | --- |
| `src/Fulfillment.Domain` | Entities (`Product`, `Category`, `Warehouse`, `InventoryItem`, `StockMovement`, `Customer`, `Order`, `OrderItem`), the `MovementType` and `OrderStatus` enums, and the domain exceptions. All business invariants live here. |
| `src/Fulfillment.Application` | MediatR commands, queries and handlers organised by feature, FluentValidation validators, the `IApplicationDbContext` abstraction, and the validation pipeline behaviour. |
| `src/Fulfillment.Infrastructure` | `FulfillmentDbContext`, EF Core entity configurations, migrations, the auditing interceptor, the order reference generator, ASP.NET Core Identity types, and JWT token issuing. |
| `src/Fulfillment.Api` | Controllers, request contracts, the exception handling middleware, dependency wiring and the HTTP pipeline in `Program.cs`. |
| `tests/Fulfillment.Tests` | xUnit tests. `Domain/` holds pure unit tests, `Application/` holds handler and controller tests backed by SQLite in memory. |

Project references, read from the `.csproj` files:

```
Fulfillment.Domain          (no project references)
Fulfillment.Application  -> Fulfillment.Domain
Fulfillment.Infrastructure -> Fulfillment.Application
Fulfillment.Api          -> Fulfillment.Application, Fulfillment.Infrastructure
Fulfillment.Tests        -> Fulfillment.Api
```

Dependencies point inward. Domain depends on nothing. Infrastructure depends on Application rather than the reverse, and supplies `FulfillmentDbContext` as the implementation of `IApplicationDbContext`, which `Program.cs` registers. Api references Infrastructure directly, so composition and migrations run from the Api project. Infrastructure also carries a `FrameworkReference` to `Microsoft.AspNetCore.App` because it hosts the ASP.NET Core Identity stores. The test project references only Api, and reaches the inner layers transitively.

## Overview

The system manages a product catalog and warehouse stock, then processes customer orders against that stock. Products are stocked per warehouse through `InventoryItem` records, and every quantity change writes an append only `StockMovement` row. Orders belong to a customer, ship from a single warehouse, capture a price snapshot on each line at the time the line is added, and move through a fixed lifecycle that deducts and restores stock at defined points.

## Prerequisites

* .NET SDK 10.0. Every project targets `net10.0`.
* A reachable SQL Server instance. The Infrastructure project uses the `Microsoft.EntityFrameworkCore.SqlServer` provider.
* The EF Core command line tools, for creating the database:

```
dotnet tool install --global dotnet-ef
```

No other tooling is required. Tests need no database.

## Setup

Restore and build from the repository root:

```
dotnet restore
dotnet build
```

The connection string and the JWT signing key are not stored in the repository. Both are read from user secrets. The `UserSecretsId` is declared in `src/Fulfillment.Api/Fulfillment.Api.csproj`, so both commands target that project:

```
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=FulfillmentDb;User Id=sa;Password=<password>;TrustServerCertificate=True" --project src/Fulfillment.Api
```

```
dotnet user-secrets set "Jwt:Key" "<at-least-32-characters>" --project src/Fulfillment.Api
```

`Jwt:Key` must be at least 32 characters. `Program.cs` binds `JwtOptions` with `.Validate(...)` and `.ValidateOnStart()`, so a missing or shorter key fails startup with the message `Jwt:Key must be configured and at least 32 characters.` rather than failing on the first request.

The remaining JWT settings are already in `appsettings.json` and need no secret: issuer `Fulfillment.Api`, audience `Fulfillment.Client`, and an expiry of 120 minutes applied by `JwtTokenService`.

Create the database by applying the six migrations:

```
dotnet ef database update --project src/Fulfillment.Infrastructure --startup-project src/Fulfillment.Api
```

Both flags are required. The `DbContext` and the migrations live in Infrastructure, while the configuration and the host builder live in Api.

## Running the application

```
dotnet run --project src/Fulfillment.Api
```

The default `http` launch profile binds `http://localhost:5138`. The `https` profile binds `https://localhost:7020` and `http://localhost:5138`. Swagger UI is served at:

```
http://localhost:5138/swagger
```

A liveness endpoint backed by a `DbContext` check is served at `/health` and requires no authentication.

Three things in `Program.cs` are gated on `app.Environment.IsDevelopment()`: HTTP request logging, Swagger and Swagger UI, and the data seeder. The launch profiles set `ASPNETCORE_ENVIRONMENT=Development`, so `dotnet run` produces a seeded database and a browsable API. Running under any other environment leaves the database empty, returns 404 for `/swagger`, and logs no HTTP requests. Seeded accounts are therefore development accounts and are never created in production.

To authenticate, call `POST /api/auth/login`, then send the returned token as `Authorization: Bearer <token>`.

## Seeded users

The seeder runs on every Development startup. User creation is idempotent per email, and the catalog block is skipped entirely if any warehouse already exists.

All four accounts share the password `Passw0rd!2026`, defined as `DefaultPassword` in `DataSeeder.cs`.

| Email | Role | Full name |
| --- | --- | --- |
| `admin@fulfillment.local` | `Administrator` | System Administrator |
| `operator@fulfillment.local` | `WareHouseOperator` | Warehouse Operator |
| `manager@fulfillment.local` | `Manager` | Operations Manager |
| `sales@fulfillment.local` | `SalesAgent` | Sales Agent |

Four policies are declared in `Program.cs`. Each maps to a fixed set of roles.

| Policy | Roles accepted |
| --- | --- |
| `CanManageCatalog` | `Administrator` |
| `CanAdjustStock` | `Administrator`, `WareHouseOperator` |
| `CanManageOrders` | `Administrator`, `SalesAgent` |
| `CanProcessOrders` | `Administrator`, `WareHouseOperator` |

What each account can do, derived from those policies and the `[Authorize(Policy = ...)]` attributes on the controllers:

**Administrator** appears in all four policies and can call every endpoint: create categories, products and warehouses, change product active status, create inventory items, adjust stock, create customers, create and cancel orders, add order lines, and confirm and complete orders.

**WareHouseOperator** holds `CanAdjustStock` and `CanProcessOrders`. It can create inventory items, adjust stock, confirm orders and complete orders. It cannot create categories, products or warehouses, cannot change product status, and cannot create customers, create orders, add order lines or cancel orders.

**SalesAgent** holds `CanManageOrders`. It can create customers, create orders, add order lines and cancel orders. It cannot touch the catalog, cannot create inventory items or adjust stock, and cannot confirm or complete orders.

**Manager** appears in no policy. Every policy protected endpoint returns 403 for this account. It can call the fourteen endpoints marked "any authenticated user" below, which covers reading products, categories, warehouses, customers, stock, stock movements, orders, order history and the operations summary. The role is read only by construction, not by an explicit rule.

Endpoints marked "any authenticated user" apply no role check at all, so all four accounts can read stock movements including the user id recorded against each one.

## API reference

Every controller carries a class level `[Authorize]`, so all endpoints except `POST /api/auth/login` and `/health` require a valid token. Requests without a token receive 401. Requests with a token whose role is not in the required policy receive 403.

| Method | Route | Required policy | Purpose |
| --- | --- | --- | --- |
| POST | `/api/auth/login` | anonymous | Exchange email and password for a JWT. |
| GET | `/api/auth/me` | any authenticated user | Return the caller's user id and roles. |
| POST | `/api/categories` | `CanManageCatalog` | Create a category. |
| GET | `/api/categories` | any authenticated user | List categories, paged. |
| POST | `/api/products` | `CanManageCatalog` | Create a product. |
| GET | `/api/products` | any authenticated user | List products with search, category, active and low stock filters, paged. |
| GET | `/api/products/{id}` | any authenticated user | Fetch one product. |
| PATCH | `/api/products/{id}/status` | `CanManageCatalog` | Activate or deactivate a product. |
| POST | `/api/warehouses` | `CanManageCatalog` | Create a warehouse. |
| GET | `/api/warehouses` | any authenticated user | List warehouses, paged. |
| GET | `/api/warehouses/{id}` | any authenticated user | Fetch one warehouse. |
| POST | `/api/customers` | `CanManageOrders` | Create a customer. |
| GET | `/api/customers` | any authenticated user | List customers with search, paged. |
| POST | `/api/inventory/items` | `CanAdjustStock` | Stock a product in a warehouse by creating an inventory item. |
| POST | `/api/inventory/items/{id}/adjustments` | `CanAdjustStock` | Apply a stock adjustment and write a movement. |
| GET | `/api/inventory/warehouses/{warehouseId}/products` | any authenticated user | List stock held in one warehouse, paged. |
| GET | `/api/inventory/movements` | any authenticated user | List stock movements with filters, paged. |
| GET | `/api/inventory/products/{productId}/stock` | any authenticated user | List stock for one product across warehouses, paged. |
| POST | `/api/orders` | `CanManageOrders` | Create a Draft order. Honours an `Idempotency-Key` header. |
| POST | `/api/orders/{id}/items` | `CanManageOrders` | Add a line to a Draft order. |
| POST | `/api/orders/{id}/confirm` | `CanProcessOrders` | Confirm an order and deduct stock. |
| POST | `/api/orders/{id}/complete` | `CanProcessOrders` | Mark a Confirmed order Completed. |
| POST | `/api/orders/{id}/cancel` | `CanManageOrders` | Cancel an order and restore stock if it had been confirmed. |
| GET | `/api/orders` | any authenticated user | List orders with status, customer, warehouse, date, search and sort options, paged. |
| GET | `/api/orders/{id}` | any authenticated user | Fetch one order with its lines. |
| GET | `/api/orders/{id}/history` | any authenticated user | Return a timeline of order events and related stock movements. |
| GET | `/api/reports/operations-summary` | any authenticated user | Return order counts by status, revenue, top products, stock by warehouse and a low stock count. |
| GET | `/health` | anonymous | Report application and database health. |

List endpoints accept `pageNumber` and `pageSize`, both defaulting to 1 and 20. `PaginatedList.CreateAsync` clamps `pageNumber` to a minimum of 1 and `pageSize` to the range 1 to 100, so out of range values are corrected rather than rejected.

`GET /api/orders` accepts `sortBy` values `total`, `status` and `customer`. Any other value, including an unrecognised one, falls through to the default sort by `OrderedAt`. Sorting is applied from a fixed allow list, not by reflecting over the supplied name.

### Reference values

`OrderStatus`:

| Value | Name |
| --- | --- |
| 1 | Draft |
| 2 | Confirmed |
| 3 | Completed |
| 4 | Cancelled |

`MovementType`, with the direction each type permits:

| Value | Name | Permitted delta |
| --- | --- | --- |
| 1 | Receipt | Positive |
| 2 | Issue | Negative |
| 3 | Damage | Negative |
| 4 | Loss | Negative |
| 5 | CountCorrection | Either |
| 6 | Other | Either, and a reason is required |
| 7 | OrderAllocation | Negative |
| 8 | OrderCancellation | Positive |

Seeded catalog data, created only when no warehouse exists:

| Entity | Values |
| --- | --- |
| Category | General |
| Product | SKU-001, Test Widget, price 25.00, low stock threshold 20 |
| Product | SKU-002, Test Gadget, price 40.00, low stock threshold 10 |
| Warehouse | WH-01, Main Warehouse, Cairo |
| Customer | Acme Corporation, orders@acme.example |

No inventory items are seeded. Stock must be created through `POST /api/inventory/items` followed by an adjustment.

## Order lifecycle

An order has four states: Draft, Confirmed, Completed and Cancelled. A new order starts in Draft with a total of zero.

`Order.EnsureTransitionAllowed` permits exactly four transitions:

| From | To | Allowed |
| --- | --- | --- |
| Draft | Confirmed | Yes |
| Draft | Cancelled | Yes |
| Confirmed | Completed | Yes |
| Confirmed | Cancelled | Yes |
| Draft | Completed | No |
| Confirmed | Confirmed | No |
| Completed | anything | No |
| Cancelled | anything | No |

Every other combination throws `InvalidOrderStateException`, which the middleware maps to 422 with a message naming both states, for example `An order cannot move from Completed to Cancelled.` Completed and Cancelled are terminal.

Two further rules apply on top of the transition table. Confirming an order with no lines throws `BusinessRuleViolationException`. Adding a line to an order that is not in Draft throws `InvalidOrderStateException`.

Stock moves at two points:

* **Confirm deducts stock.** `ConfirmOrderCommandHandler` calls `order.Confirm()` first, so an invalid transition never reaches inventory. It then loads the inventory items for the order's warehouse, rejects the request if a line's product is not stocked there or if available quantity is below the line quantity, and applies one `OrderAllocation` adjustment per line. The status change, the quantity changes and the movement rows are written by a single `SaveChangesAsync`, so a failure at any point leaves order and inventory unchanged.
* **Cancel restores stock, conditionally.** `Order.Cancel()` returns a boolean that is true only when the order was Confirmed at the moment of cancellation. `CancelOrderCommandHandler` writes `OrderCancellation` adjustments only when that flag is true. Cancelling a Draft order therefore moves no stock, because none was ever taken. Since Cancelled is terminal, an order cannot be cancelled twice, so stock cannot be restored twice.

Complete moves no stock. It records `CompletedAt` and changes the status only.

Stock movements caused by an order carry that order's id in `StockMovement.OrderId`, a nullable foreign key to `Orders`. Adjustments made directly through the inventory endpoints leave it null.

## Design decisions and assumptions

### Customer data

A customer is a standalone entity with `Name`, `Email`, optional `Phone`, optional `Address`, and a collection of orders. `Email` carries a unique index, and `CreateCustomerCommandHandler` checks for an existing address before inserting, so a duplicate returns 409 rather than a database error. `Name` is limited to 200 characters, `Email` to 256, `Phone` to 30 and `Address` to 500, enforced both by the validator and by the column mapping.

Only two operations exist: create, at `POST /api/customers`, and list with search, at `GET /api/customers`. There is no update, no delete and no fetch by id. Orders reference customers with `DeleteBehavior.Restrict`, so a customer with orders could not be removed even if a delete endpoint existed. The assumption is that customer records are created once and read thereafter, and that correcting customer details is outside the scope of order processing. An order snapshots nothing from the customer, so a later change to a customer's name would be reflected in existing orders when read back.

### Order states

The lifecycle is Draft, Confirmed, Completed, with Cancelled reachable from either of the first two. Draft exists so that lines can be assembled over several requests without touching stock, which is what makes an order editable and cancellable at no cost. Confirmed is the point of commitment, and is therefore the point at which stock is taken. Completed marks fulfilment and is terminal. Cancelled is terminal and unreachable from Completed, on the assumption that a shipped order is corrected by a separate return process rather than by reversing the original order.

The transition table is deliberately small and is enforced in the domain entity, not in a handler, so an unexpected request cannot bypass it. Both terminal states are final, which removes any question of an order being confirmed twice or restoring stock twice.

### When stock changes

Stock is deducted on confirm and restored on cancel, only if the order had reached Confirmed. Completion does not move stock.

The considered alternative was reserve then deduct: hold a reservation at Draft or Confirm, and deduct the physical quantity at Complete. That models a warehouse more faithfully, since goods leave the building at dispatch rather than at order acceptance. It was not chosen because it requires a second quantity concept, reserved against on hand, tracked per inventory item and honoured by every availability check, along with a policy for expiring stale reservations. Deducting at confirm keeps a single `Quantity` per inventory item and one meaning for availability.

The trade off is that stock is unavailable from the moment an order is confirmed rather than from dispatch, which understates physically present stock between confirm and complete. Because a movement row records every change, the physical position can still be reconstructed from the movement log.

Quantity can never go negative. `InventoryItem.ApplyAdjustment` rejects any adjustment that would take it below zero, and a check constraint on `StockMovements.QuantityAfter` enforces the same at the database level. A second check constraint rejects a zero delta.

## Known limitations

**Order reference numbers race under concurrency.** `OrderReferenceGenerator` reads the highest existing reference for the year and adds one. The class carries a documentation comment stating this. Two concurrent creations can compute the same number. The unique index on `Order.ReferenceNumber` rejects the loser's insert, so the outcome is a 409 rather than a duplicate reference, but a legitimate request can fail for a reason unrelated to its own content. A database sequence would remove the race.

**Order history timeline entries have no user id for status changes.** `GetOrderHistoryQueryHandler` builds the timeline from the order's timestamps and its related stock movements. Entries derived from stock movements carry `PerformedByUserId`. The Confirmed, Completed and Cancelled entries pass `null`, so the history shows who moved stock but not who confirmed, completed or cancelled the order, even though `Order.UpdatedByUserId` is populated by the auditing interceptor.

**Error response shapes are not consistent across failure paths.** Four distinct shapes were observed against a running instance:

| Failure | Shape |
| --- | --- |
| FluentValidation failure (400) | `{"title":"Validation failed.","status":400,"instance":"...","errors":{...},"traceId":"..."}` |
| Model binding failure, such as a missing field or malformed JSON (400) | `{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"One or more validation errors occurred.","status":400,"errors":{...},"traceId":"..."}` |
| Not found (404), conflict (409), business rule violation (422) | `{"title":"Resource not found.","status":404,"detail":"...","instance":"...","traceId":"..."}` |
| Missing or invalid token (401), insufficient role (403) | Empty body. `WWW-Authenticate: Bearer` is present on 401. |
| Failed login (401) | `{"message":"Invalid email or password."}` |

Responses produced by `ExceptionHandlingMiddleware` use `application/problem+json`, carry `instance` and a W3C format `traceId`, and take their titles from `errors.json`. Responses produced by MVC's own model state filter carry a `type` but no `instance`. Authentication and authorization failures produce no body at all, and a failed login uses a bespoke object rather than problem details. A consumer needs to handle all four.

**Product prices cannot be changed through the API.** `CreateProductCommand` accepts a price, and `PATCH /api/products/{id}/status` toggles the active flag, but no endpoint updates an existing product's price or other fields.

**Inventory items cannot be removed and products cannot be deleted.** Only creation and status toggling are exposed.

## Testing

```
dotnet test
```

74 tests, all passing.

| Test class | Count | Scope |
| --- | --- | --- |
| `Domain.InventoryItemTests` | 36 | Stock invariants: zero delta, direction rules for all eight movement types, the below zero guard, the exact zero boundary, reason trimming and the required reason for `Other`, running `QuantityAfter` across sequential adjustments, and the order id recorded on a movement. |
| `Domain.OrderTests` | 26 | Price snapshotting, line merging on repeated products, total recalculation, quantity validation, deactivated product rejection, the full transition table including invalid transitions, and that rejected operations leave totals untouched. |
| `Application.CreateInventoryItemCommandHandlerTests` | 4 | The deactivated product guard, the active product path, the not found path, and the ordering of the deactivation check against the duplicate check. |
| `Application.OrdersControllerBindingTests` | 3 | That the `Idempotency-Key` header reaches `CreateOrderCommand.IdempotencyKey` and the body's notes reach `Notes`, asserted at the controller. |
| `Application.GetOperationsSummaryQueryHandlerTests` | 5 | That the summary query executes and returns populated results, plus a companion test asserting the previously untranslatable query shape still fails. |

Application tests run against SQLite in memory, configured in `tests/Fulfillment.Tests/Application/TestDbContextFactory.cs`. SQLite is a real relational provider, so a query that cannot be translated fails there as it would against SQL Server, which is what makes the operations summary test meaningful. Dialect and collation differences are not reproduced, and the test model clears the store generated and concurrency token flags on `RowVersion` because SQLite has no `rowversion` equivalent, so optimistic concurrency is not exercised by the suite. The test file documents both points.

## Conventions

Features are organised as vertical slices under `src/Fulfillment.Application`, one folder per feature area, split into `Commands` and `Queries`. Each command folder holds the command record, its handler and, where input needs checking, a FluentValidation validator:

```
Orders/
  Commands/
    CreateOrder/
      CreateOrderCommand.cs
      CreateOrderCommandHandler.cs
      CreateOrderCommandValidator.cs
  Queries/
    GetOrders/
      GetOrdersQuery.cs
      GetOrdersQueryHandler.cs
```

Validators are discovered by assembly scanning in `DependencyInjection.AddApplication` and run through `ValidationBehaviour`, a MediatR pipeline behaviour, before any handler executes. Adding a validator is enough to enable it.

Controllers stay thin. They bind a request, send a MediatR message and return a status code. They contain no business logic and catch no exceptions.

Handlers throw, and `ExceptionHandlingMiddleware` maps the exception to a status code and a problem details body whose title comes from `errors.json`:

| Exception | Status |
| --- | --- |
| `ValidationException` | 400 |
| `NotFoundException` | 404 |
| `ConflictException` | 409 |
| `DbUpdateConcurrencyException` | 409 |
| `DbUpdateException` with SQL error 2601 or 2627 | 409 |
| `BusinessRuleViolationException` | 422 |
| `InvalidOrderStateException` | 422 |
| `UnauthorizedAccessException` | 401 |
| Anything else | 500, logged with the stack trace, generic message returned |

Business invariants belong in the domain entities, not in handlers. `InventoryItem.ApplyAdjustment` and the methods on `Order` throw before mutating state, so a rejected operation leaves no trace. Handlers coordinate persistence and enforce rules that need database access, such as checking available stock across an order's lines.

Audit columns are populated automatically. `AuditableEntityInterceptor` fills `CreatedAt`, `CreatedByUserId`, `UpdatedAt` and `UpdatedByUserId` on save, so handlers never set them.

After changing an entity or a configuration, add a migration from the repository root:

```
dotnet ef migrations add <Name> --project src/Fulfillment.Infrastructure --startup-project src/Fulfillment.Api
```

Then apply it with the `database update` command shown in Setup.
