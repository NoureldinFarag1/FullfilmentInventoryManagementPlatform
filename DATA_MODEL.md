# Data model

Five business tables — `Categories`, `Products`, `Warehouses`, `InventoryItems`,
`StockMovements` — plus the standard ASP.NET Core Identity tables. All types below are read
from the migrations in `src/Fulfillment.Infrastructure/Migrations/`, which are the truth about
what SQL Server actually has.

Everything uses `uniqueidentifier` primary keys. `BaseEntity` assigns `Guid.NewGuid()` in its
constructor, so IDs exist client-side before insert, which is what lets a stock adjustment
build the `InventoryItem` → `StockMovement` link in memory and save both in one round trip.

```
Categories ──0..1─── Products ───┐
                                 ├──< InventoryItems >──── Warehouses
                                 │          │
                                 │          └──< StockMovements >──── AspNetUsers
```

## Categories

| Column | Type | Null | Notes |
| --- | --- | --- | --- |
| `Id` | `uniqueidentifier` | no | PK `PK_Categories` |
| `Name` | `nvarchar(100)` | no | unique |
| `Description` | `nvarchar(500)` | yes | |
| `IsActive` | `bit` | no | defaults to `true` in the entity |
| `CreatedAt` | `datetime2` | no | see the audit-column note below |
| `UpdatedAt` | `datetime2` | yes | |
| `CreatedByUserId` | `uniqueidentifier` | yes | |
| `UpdatedByUserId` | `uniqueidentifier` | yes | |

Index: `IX_Categories_Name` (unique).

## Products

| Column | Type | Null | Notes |
| --- | --- | --- | --- |
| `Id` | `uniqueidentifier` | no | PK `PK_Products` |
| `Sku` | `nvarchar(100)` | no | unique |
| `Name` | `nvarchar(100)` | no | |
| `Description` | `nvarchar(1000)` | yes | |
| `IsActive` | `bit` | no | |
| `CategoryId` | `uniqueidentifier` | yes | FK `FK_Products_Categories_CategoryId`, `ReferentialAction.Restrict` |
| `CreatedAt` | `datetime2` | no | |
| `UpdatedAt` | `datetime2` | yes | |
| `CreatedByUserId` | `uniqueidentifier` | yes | |
| `UpdatedByUserId` | `uniqueidentifier` | yes | |

Indexes: `IX_Products_Sku` (unique), `IX_Products_Name` (non-unique), `IX_Products_CategoryId`.

`Name` is indexed but not unique — two suppliers can plausibly ship things called "Blue
Widget", and the SKU is what disambiguates them. The index exists because `search` does a
`Contains` on name and SKU; a leading-wildcard `LIKE` won't seek on it, but it still helps the
ordering, which is always by name.

The `CategoryId` FK is `Restrict`, not `Cascade`. Deleting a category should never take
products with it, and since nothing is hard-deleted in this milestone anyway (see
ASSUMPTIONS.md), the restriction is a guard rail rather than a workflow.

## Warehouses

| Column | Type | Null | Notes |
| --- | --- | --- | --- |
| `Id` | `uniqueidentifier` | no | PK `PK_Warehouses` |
| `Code` | `nvarchar(20)` | no | unique |
| `Name` | `nvarchar(200)` | no | |
| `Address` | `nvarchar(500)` | yes | |
| `IsActive` | `bit` | no | |
| `CreatedAt` | `datetime2` | no | |
| `UpdatedAt` | `datetime2` | yes | |
| `CreatedByUserId` | `uniqueidentifier` | yes | |
| `UpdatedByUserId` | `uniqueidentifier` | yes | |

Index: `IX_Warehouses_Code` (unique).

`Code` is short (20) on purpose — it's the human handle people type and say out loud
("WH-01"), and warehouse lists are ordered by it.

## InventoryItems

| Column | Type | Null | Notes |
| --- | --- | --- | --- |
| `Id` | `uniqueidentifier` | no | PK `PK_InventoryItems` |
| `ProductId` | `uniqueidentifier` | no | FK `FK_InventoryItems_Products_ProductId`, `ReferentialAction.Restrict` |
| `WarehouseId` | `uniqueidentifier` | no | FK `FK_InventoryItems_Warehouses_WarehouseId`, `ReferentialAction.Restrict` |
| `Quantity` | `int` | no | |
| `RowVersion` | `rowversion` | no | optimistic concurrency token |
| `CreatedAt` | `datetime2` | no | |
| `UpdatedAt` | `datetime2` | yes | |
| `CreatedByUserId` | `uniqueidentifier` | yes | |
| `UpdatedByUserId` | `uniqueidentifier` | yes | |

Indexes: `IX_InventoryItems_ProductId_WarehouseId` (unique), `IX_InventoryItems_WarehouseId`.

Check constraint: `CK_InventoryItems_Quantity_NotNegative` — `[Quantity] >= 0`.

### Why this is a join entity and not a column on Product

The obvious shortcut is a `Quantity` column on `Products`. It falls apart the moment there is
more than one warehouse, which is the entire premise of the system: "how many widgets do we
have" is not a single number, it's a number per location. `InventoryItem` is the answer to
"how much of *this* product is in *that* warehouse", and it's the row that gets locked,
versioned, and audited.

Making it a first-class entity with its own `Id` rather than a composite `(ProductId,
WarehouseId)` key was a practical call. The adjustment endpoint takes one route parameter
instead of two, and `StockMovements` carries one FK column instead of two. The unique index on
`(ProductId, WarehouseId)` keeps the pair honest — it's the real key, enforced as a constraint
rather than used as the PK. The handler also checks for the duplicate before inserting, so the
normal path returns a clean 409 with a readable message instead of a database error; the index
is the backstop for the race.

The second index on `WarehouseId` alone serves "list everything in this warehouse", which is a
core read. The composite index already leads with `ProductId`, so it can't serve that query.

`RowVersion` is why two operators adjusting the same item concurrently cannot lose an update.
Whoever saves second gets a `DbUpdateConcurrencyException`, which the middleware turns into a
409 telling them to reload.

The check constraint duplicates a rule the domain already enforces. That's intentional. The
entity is the only path *this application* offers, but a data-fix script or a future service
isn't bound by C#, and "stock can never go negative" was stated as an absolute. Belt and
braces on the one invariant that the business genuinely cannot tolerate breaking.

## StockMovements

| Column | Type | Null | Notes |
| --- | --- | --- | --- |
| `Id` | `uniqueidentifier` | no | PK `PK_StockMovements` |
| `InventoryItemId` | `uniqueidentifier` | no | FK `FK_StockMovements_InventoryItems_InventoryItemId`, `ReferentialAction.Restrict` |
| `Delta` | `int` | no | signed |
| `QuantityAfter` | `int` | no | |
| `Type` | `int` | no | `MovementType`, stored as its int value |
| `Reason` | `nvarchar(500)` | yes | required only when `Type = Other` |
| `PerformedByUserId` | `uniqueidentifier` | no | FK `FK_StockMovements_AspNetUsers_PerformedByUserId`, `ReferentialAction.Restrict` |
| `OccuredAt` | `datetime2` | no | spelling is wrong; see below |

Indexes: `IX_StockMovements_InventoryItemId_OccuredAt`, `IX_StockMovements_OccuredAt`,
`IX_StockMovements_PerformedByUserId`.

Check constraints: `CK_StockMovements_Delta_NonZero` (`[Delta] <> 0`) and
`CK_StockMovements_QuantityAfter_NonNegative` (`[QuantityAfter] >= 0`).

This table is append-only. There is no update path and no delete path anywhere in the code —
`StockMovement` has a private parameterless constructor for EF and an `internal` constructor,
so nothing outside the Domain assembly can create one, and every property has a private
setter. An audit trail
you can edit is not an audit trail.

Note it does not inherit `BaseEntity`. It has no `UpdatedAt` or `UpdatedByUserId` because
nothing ever updates it, and `CreatedAt`/`CreatedByUserId` would just be worse-named
duplicates of `OccuredAt`/`PerformedByUserId`.

`Type` is stored as `int` via an explicit `HasConversion<int>()`, and the enum values are
explicitly numbered in `MovementType.cs`. Storing the name as a string would be more readable
in ad-hoc SQL, but it makes renames a data migration; explicit ints mean the stored value is
stable and the C# name is free to change.

The three indexes match the three ways the history is actually read: one item's history in
time order, a global time window, and "everything this user did". The rest of the filters on
`GET /api/inventory/movements` (`productId`, `warehouseId`, `type`) go through the join to
`InventoryItems` or scan; at this data volume that's fine, and I'd rather add indexes against
observed query plans than guess.

### Why QuantityAfter is stored

It's derivable. Sum every `Delta` for an inventory item up to a point in time and you get the
same number, and I stored it anyway.

The reason is that a movement is a record of what was true when a person made a decision.
Recomputing it means the answer to "what did the count read after Sara's adjustment on
Tuesday" depends on every row inserted since — and if a row is ever back-dated, corrected, or
inserted out of order, the recomputed history silently changes. Stored, the number is a fact
about that moment; recomputed, it's a derivation that can drift.

It's also just faster and simpler: the movements endpoint returns the running quantity without
a window function, and it gives a cheap consistency check — the latest movement's
`QuantityAfter` should equal `InventoryItems.Quantity`, so if it ever doesn't, something wrote
around the domain. The redundancy is safe here because the row can never be updated.

### Why the AspNetUsers FK has no navigation property

`StockMovement.PerformedByUserId` is a real foreign key to `AspNetUsers`, with a database
constraint that prevents recording a movement against a user who doesn't exist and prevents
deleting a user who has stock history. But there is no `public ApplicationUser User { get; }`
on the entity.

`ApplicationUser` lives in `Fulfillment.Infrastructure`, and `StockMovement` lives in
`Fulfillment.Domain`, which references nothing. A navigation property would invert the whole
dependency direction — Domain would suddenly depend on ASP.NET Core Identity, on
Infrastructure, and transitively on EF Core, for the sake of one convenience property.

So the relationship is configured from the Infrastructure side only, in
`StockMovementConfiguration`:

```csharp
builder.HasOne<ApplicationUser>()
    .WithMany()
    .HasForeignKey(m => m.PerformedByUserId)
    .OnDelete(DeleteBehavior.Restrict);
```

`HasOne<ApplicationUser>()` with no lambda declares the relationship without a navigation on
either end. The referential integrity is real; only the object-graph convenience is missing.
The cost is that queries expose `performedByUserId` as a GUID rather than a name — resolving
that to a display name is a lookup against Identity, which belongs in a later milestone
anyway.

### The OccuredAt misspelling

It should be `OccurredAt`. The typo is in the entity property, and from there it propagated
into the column name, two of the three index names on this table
(`IX_StockMovements_InventoryItemId_OccuredAt` and `IX_StockMovements_OccuredAt`), the two
migrations, the model snapshot, and the JSON field name that clients see (`occuredAt` in
movement and adjustment responses).

Fixing it now means editing the entity, generating a third migration that renames a column and
drops/recreates two indexes, and breaking any client already reading the field — to change one
letter. I've left it and written it down instead. It's a cosmetic defect with a real cost to
fix mid-milestone; the right time is alongside the next schema change.

### The FixStockMovementIndex migration

The first migration created `IX_StockMovements_InventoryItemId_OccuredAt` as a **unique**
index. That was a mistake, and a bad one.

`OccuredAt` is set from `DateTime.UtcNow`, and `datetime2` on SQL Server has finer resolution
than the system clock actually ticks. Two adjustments to the same inventory item within the
same clock tick — a script correcting stock, or just two fast requests — would produce
identical `(InventoryItemId, OccuredAt)` pairs, and the second insert would be rejected by a
unique-constraint violation. The user would see a 409 conflict for an operation that is
completely legitimate: an audit log must accept two entries in the same instant.

The unique flag was there because I was thinking of the pair as an identifier. It isn't. The
identifier is `Id`; this index exists purely to make "one item's history in time order" a
seek plus an ordered scan.

`20260816234159_FixStockMovementIndex` drops the index and recreates it non-unique, with the
same name. That's the whole migration. I kept it as a separate migration rather than editing
the initial one, because the initial migration had already been applied to a database and
rewriting applied history is how you end up with schemas that don't match anywhere else.

## Identity tables

Standard ASP.NET Core Identity schema from `IdentityDbContext<ApplicationUser,
IdentityRole<Guid>, Guid>`: `AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`,
`AspNetUserClaims`, `AspNetRoleClaims`, `AspNetUserLogins`, `AspNetUserTokens`.

I changed two things. Keys are `Guid` (`uniqueidentifier`) rather than the default `string`,
which keeps them consistent with every other key in the schema and makes
`StockMovements.PerformedByUserId` a natural `uniqueidentifier` FK rather than an
`nvarchar(450)` one. And `ApplicationUser` adds one column:

| Column | Type | Null |
| --- | --- | --- |
| `Fullname` | `nvarchar(max)` | no |

`nvarchar(max)` is EF's default for an unconfigured string, and I didn't add a `HasMaxLength`
for it. It should be capped — it's the one column in the schema with a length I didn't choose
deliberately.

Identity's own indexes come along unchanged: `UserNameIndex` (unique, filtered on
`[NormalizedUserName] IS NOT NULL`), `EmailIndex` (non-unique), `RoleNameIndex` (unique,
filtered). Email uniqueness is enforced by `options.User.RequireUniqueEmail = true` in
`Program.cs` at the application level, not by the index.

## The audit columns

`BaseEntity` gives `Categories`, `Products`, `Warehouses` and `InventoryItems` four audit
columns — `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`. Nothing populates
them. There is no `SaveChanges` interceptor, and no handler sets them. Because `CreatedAt` is
non-nullable `datetime2`, every row is stored with `default(DateTime)`, which is
`0001-01-01 00:00:00.0000000` — I confirmed that against the seeded rows in the running
database.

This does not affect stock attribution, which is the audit trail that the milestone is
actually graded on. `StockMovements.PerformedByUserId` and `OccuredAt` are set in the
`StockMovement` constructor from the authenticated caller and the clock, they are covered by
tests, and they are correct. The unpopulated columns are on the catalog entities only. See
ASSUMPTIONS.md.