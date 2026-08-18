# Fulfillment & Inventory Management Platform — Milestone 1

This milestone is the foundation: a .NET Web API with JWT authentication, role-based
authorization, a relational schema for products/warehouses/stock, and one real business
workflow — adjusting stock with a full, attributable audit trail.

The workflow that matters is the stock adjustment. Every change to a quantity goes through
`InventoryItem.ApplyAdjustment`, which enforces the rules and returns a `StockMovement`
recording the delta, the resulting quantity, the type, the optional reason, and the user who
did it. Nothing else in the codebase can set `Quantity` — the setter is private and the
handler never touches it.

Alongside that, there are catalog endpoints (create/list/get for products, categories and
warehouses), read endpoints for warehouse inventory, per-product stock across warehouses,
and a filterable movement history.

## Tech stack

- .NET 10 (`net10.0`), ASP.NET Core Web API, C#
- Entity Framework Core 10.0.11, SQL Server provider
- ASP.NET Core Identity with `Guid` keys, JWT bearer tokens
- MediatR 14.2.0 for the command/query split, FluentValidation 12.1.1 for input validation
- Swashbuckle 10.2.3 for Swagger
- xUnit + FluentAssertions for tests

## Prerequisites

- .NET SDK 10 (built and tested on 10.0.301)
- Docker, for SQL Server
- `dotnet-ef` CLI. I used 10.0.9:
  ```bash
  dotnet tool install --global dotnet-ef
  ```

## Starting SQL Server

I develop on Apple Silicon, and Microsoft does not ship an arm64 build of the SQL Server
image — `mcr.microsoft.com/mssql/server:2022-latest` is amd64 and runs under Rosetta
emulation. It works fine for this project; startup is just slower than native.

```bash
docker run -d --name fulfillment-sql \
  -e "ACCEPT_EULA=Y" \
  -e "MSSQL_SA_PASSWORD=<your-strong-password>" \
  -e "MSSQL_PID=developer" \
  -p 1433:1433 \
  mcr.microsoft.com/mssql/server:2022-latest
```

SQL Server rejects weak SA passwords at container startup, so pick something with mixed
case, a digit and a symbol. If the container exits immediately, `docker logs fulfillment-sql`
will normally say so in the first few lines.

## Configuration (user secrets)

Nothing sensitive is committed. `appsettings.json` holds only `Jwt:Issuer`, `Jwt:Audience`
and `Jwt:ExpiryMinutes`; the connection string and the signing key live in user secrets.
The `UserSecretsId` is already set in `src/Fulfillment.Api/Fulfillment.Api.csproj`.

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
  "Server=localhost,1433;Database=FulfillmentDb;User Id=sa;Password=<your-strong-password>;Encrypt=True;TrustServerCertificate=True" \
  --project src/Fulfillment.Api

dotnet user-secrets set "Jwt:Key" "<at-least-32-characters-of-random-text>" \
  --project src/Fulfillment.Api
```

Two things to watch.

The JWT key must be at least 32 characters. `Program.cs` binds `JwtOptions` with
`.Validate(...)` and `.ValidateOnStart()`, so a shorter key doesn't fail lazily at the first
login — the host refuses to start with `Jwt:Key must be configured and at least 32 characters.`
That's deliberate: HMAC-SHA256 wants a key at least as long as the hash output, and a
weak signing key is the kind of thing that quietly ships. I generate one with
`openssl rand -base64 48`.

The other is a zsh quirk rather than a project one. If your SA password contains `!`, do not
put it inside double quotes — zsh history expansion will try to expand it and you'll either
get an error or, worse, a silently wrong password stored. Use single quotes for the argument:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
  'Server=localhost,1433;Database=FulfillmentDb;User Id=sa;Password=P@ssw0rd!Example;Encrypt=True;TrustServerCertificate=True' \
  --project src/Fulfillment.Api
```

Never commit a real password or signing key. `dotnet user-secrets list --project src/Fulfillment.Api`
will show you what's currently stored.

## Creating the database

The app does not call `Migrate()` or `EnsureCreated()` anywhere, so this step is mandatory
before the first run. Migrations live in the Infrastructure project but the host that knows
how to build the `DbContext` (and holds the user secrets) is the API project, hence both
flags:

```bash
dotnet ef database update \
  --project src/Fulfillment.Infrastructure \
  --startup-project src/Fulfillment.Api
```

## Running

```bash
dotnet run --project src/Fulfillment.Api --launch-profile http
```

Swagger UI: <http://localhost:5138/swagger>. The port comes from the `http` profile in
`src/Fulfillment.Api/Properties/launchSettings.json`; the `https` profile listens on
`https://localhost:7020` in addition to 5138.

On the `http` profile you'll see `warn: Microsoft.AspNetCore.HttpsPolicy.HttpsRedirectionMiddleware[3]
Failed to determine the https port for redirect.` That's expected — `UseHttpsRedirection()` is
registered but there is no HTTPS endpoint to redirect to. Use the `https` profile if the
warning bothers you.

Run it in **Development**. Both launch profiles set `ASPNETCORE_ENVIRONMENT=Development`, and
two things are gated on it in `Program.cs`: Swagger (`UseSwagger`/`UseSwaggerUI`) and the
data seeder. In any other environment you get no API docs and no users, which makes the app
effectively unusable for review.

To authenticate in Swagger: `POST /api/auth/login`, copy the `token` from the response, click
**Authorize**, and paste it. The security scheme is HTTP bearer, so paste the raw token
without the `Bearer ` prefix.

## Seeded data

The seeder (`src/Fulfillment.Infrastructure/Persistence/DataSeeder.cs`) creates the four roles,
then four users, one per role. It is idempotent — users are skipped if the email already
exists, and the sample catalog is skipped entirely if any warehouse exists.

All four share the password `Passw0rd!2026`:

| Email | Role | Full name |
| --- | --- | --- |
| `admin@fulfillment.local` | `Administrator` | System Administrator |
| `operator@fulfillment.local` | `WareHouseOperator` | Warehouse Operator |
| `manager@fulfillment.local` | `Manager` | Operations Manager |
| `sales@fulfillment.local` | `SalesAgent` | Sales Agent |

The role string really is `WareHouseOperator` with a capital H — that's the constant in
`Roles.cs` and the value that ends up in the JWT role claim.

The sample catalog is one category (`General`), one product (`SKU-001` / "Test Widget", in
that category) and one warehouse (`WH-01` / "Main Warehouse", Cairo). No inventory item and
no stock — creating an inventory record is itself an endpoint, and I wanted the seed to leave
that to whoever is exercising the API.

## Endpoints

Every controller carries `[Authorize]` at class level, so anonymous access gets 401 everywhere
except login. The two policies are defined in `Program.cs`: `CanManageCatalog` requires
`Administrator`; `CanAdjustStock` requires `Administrator` or `WareHouseOperator`.

| Method | Route | Required role |
| --- | --- | --- |
| POST | `/api/auth/login` | anonymous |
| GET | `/api/auth/me` | any authenticated user |
| POST | `/api/products` | `Administrator` |
| GET | `/api/products` | any authenticated user |
| GET | `/api/products/{id:guid}` | any authenticated user |
| POST | `/api/categories` | `Administrator` |
| GET | `/api/categories` | any authenticated user |
| POST | `/api/warehouses` | `Administrator` |
| GET | `/api/warehouses` | any authenticated user |
| GET | `/api/warehouses/{id:guid}` | any authenticated user |
| POST | `/api/inventory/items` | `Administrator` or `WareHouseOperator` |
| POST | `/api/inventory/items/{id:guid}/adjustments` | `Administrator` or `WareHouseOperator` |
| GET | `/api/inventory/Warehouses/{warehouseId:guid}/products` | any authenticated user |
| GET | `/api/inventory/movements` | any authenticated user |
| GET | `/api/inventory/products/{productId:guid}/stock` | any authenticated user |

The capital `W` in `inventory/Warehouses/...` is how the route is written in
`InventoryController.cs`. ASP.NET Core route matching is case-insensitive so the lowercase
form works too, but that's the literal in the source.

Query parameters worth knowing:

- `GET /api/products` — `search` (matches name or SKU), `categoryId`, `isActive`, `pageNumber`, `pageSize`
- `GET /api/categories`, `GET /api/warehouses` — `search`, `isActive`, `pageNumber`, `pageSize`
- `GET /api/inventory/Warehouses/{warehouseId}/products` — `search` (product name or SKU), `pageNumber`, `pageSize`
- `GET /api/inventory/movements` — `productId`, `warehouseId`, `inventoryItemId`, `performedByUserId`, `type`, `from`, `to`, `pageNumber`, `pageSize`

The adjustment body is `{ "delta": int, "type": MovementType, "reason": string? }` — `delta` is
signed, and its sign is what makes an adjustment an increase or a decrease.

## Pagination

Every list endpoint returns the same envelope:

```json
{ "items": [], "pageSize": 20, "pageNumber": 1, "totalCount": 0, "totalPages": 0 }
```

Defaults are `pageNumber=1`, `pageSize=20`. `PaginatedList<T>.CreateAsync` clamps rather than
rejects: `pageNumber` is floored at 1 and `pageSize` is clamped into `[1, 100]`. So
`pageSize=5000` silently gives you 100 rather than a 400. I chose clamping because a caller
who asks for too much still gets a usable response, and the 100-row ceiling is what actually
protects the database. The echoed `pageSize` in the response tells you what you really got.

Note that `pageNumber` past the end returns an empty `items` array with the true `totalCount`,
not a 404.

## MovementType

`Delta` carries the sign; `MovementType` says why, and constrains which sign is legal.

| Value | Name | Allowed direction |
| --- | --- | --- |
| 1 | `Receipt` | increase only (`delta > 0`) |
| 2 | `Issue` | decrease only (`delta < 0`) |
| 3 | `Damage` | decrease only (`delta < 0`) |
| 4 | `Loss` | decrease only (`delta < 0`) |
| 5 | `CountCorrection` | either |
| 6 | `Other` | either, but `reason` is required |

`CountCorrection` is deliberately unconstrained — that's the point of a stock count; it can go
either way. `Other` is the escape hatch, and it's the one type where a free-text `reason` is
mandatory, so "other" can never mean "unexplained". A `delta` of 0 is rejected for every type.

## Error responses

Everything unhandled goes through `ExceptionHandlingMiddleware`, which returns
`application/problem+json` with `Instance` set to the request path.

| Status | When |
| --- | --- |
| 400 | FluentValidation rejected the request (shape, lengths, required fields, `delta != 0`, enum in range) |
| 401 | No token, invalid token, expired token, or `UnauthorizedAccessException` |
| 403 | Authenticated but the role doesn't satisfy the policy |
| 404 | `NotFoundException` — the product, warehouse or inventory item doesn't exist |
| 409 | `ConflictException` (duplicate SKU, warehouse code, category name, or a product already stocked in that warehouse), plus `DbUpdateException` and `DbUpdateConcurrencyException` |
| 422 | `BusinessRuleViolationException` — the request was well-formed but the domain refused it |
| 500 | Anything else; logged server-side, with a generic detail returned |

The 400/422 split is the one worth explaining. 400 means "I can't process this request as
written". 422 means "I understood you perfectly and the answer is no" — trying to issue 60
units when 50 are on hand is a business decision, not a malformed request, and the response
detail tells you both numbers:

```
Stock cannot go below zero. Current quantity is 50 and the requested change is -60.
```

409 from `DbUpdateConcurrencyException` is the optimistic-concurrency path: `InventoryItem`
has a `rowversion` column, so two simultaneous adjustments to the same item cannot silently
overwrite each other. The loser is told to reload and retry.

## Tests

```bash
dotnet test
```

12 tests, all passing. They're domain tests over `InventoryItem.ApplyAdjustment` — the one
place with real rules — and they cover: a new item starts at zero with no movements; an
adjustment that would go below zero is rejected; a rejected adjustment leaves both the
quantity and the movement list untouched; a successful adjustment records exactly one movement
with the right delta, resulting quantity, type, reason and user; zero delta is rejected;
`Other` without a reason is rejected (including whitespace-only); each of `Receipt`, `Issue`,
`Damage` and `Loss` is rejected in the wrong direction; a missing user is rejected; and the
movement collection is append-only and read-only from outside.

No infrastructure is involved — no database, no mocks. That's the point: the invariants belong
to the entity, so they can be tested as pure logic. There are no integration tests; see
ASSUMPTIONS.md for why, and for what I verified by hand instead.

## Project structure

```
src/
  Fulfillment.Domain/          entities, MovementType, BusinessRuleViolationException
  Fulfillment.Application/     MediatR commands/queries, validators, DTOs, interfaces
  Fulfillment.Infrastructure/  EF Core DbContext, configurations, migrations, Identity, seeder
  Fulfillment.Api/             controllers, exception middleware, composition root
tests/
  Fulfillment.Tests/           domain tests
```

Dependencies point inward. Domain references nothing at all — no EF Core, no ASP.NET — and
compiles with `TreatWarningsAsErrors`. Application references Domain. Infrastructure
references Application (and through it, Domain), which is what lets `FulfillmentDbContext`
implement `IApplicationDbContext`. Api references both Application and Infrastructure, and is
the only project that wires anything up.

The practical payoff is that Application handlers depend on `IApplicationDbContext`,
`ICurrentUser` and `IIdentityService` — interfaces it owns — and never on EF Core's concrete
context or on ASP.NET Core Identity. The one place I broke this is documented in
ASSUMPTIONS.md.

## Further reading

- [DATA_MODEL.md](DATA_MODEL.md) — schema, indexes, constraints, and why they look like that
- [ASSUMPTIONS.md](ASSUMPTIONS.md) — what I asked, what I decided alone, and what doesn't work yet
