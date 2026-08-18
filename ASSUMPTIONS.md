## Things I thought about, and the answers documented

**Stock can never go negative.** Not "shouldn't" — can't. That answer is why the rule sits in
`InventoryItem.ApplyAdjustment` rather than in a validator: the entity computes the new
quantity, and if it's below zero it throws before the quantity is mutated and before any
movement is created. A rejected adjustment leaves no trace, which is what two of the domain
tests check. There's also a `CK_InventoryItems_Quantity_NotNegative` check constraint in the
database, because a rule stated that absolutely deserves enforcement below the application
too.

**The adjustment reason is a fixed set, plus an "Other" option that requires free text.** That
shape is exactly what `MovementType` encodes — `Receipt`, `Issue`, `Damage`, `Loss`,
`CountCorrection` are the fixed set, and `Other` is the escape hatch. `Other` is the one type
where `reason` is mandatory; the entity trims the string first, so whitespace doesn't satisfy
it. On the other types the reason is optional free text, which lets someone add "damaged in
transit, DHL claim #4471" without a new enum value.

**An inventory record starts at zero.** The `InventoryItem` constructor sets `Quantity = 0`
and takes no starting quantity — you can't create one pre-filled. Stock arrives the same way
it always does, through a `Receipt` adjustment, which means the very first unit in a warehouse
has a movement row behind it like every unit after it.

**Every stock change is reviewable, with no time window.** No archiving, no retention cutoff,
nothing purged. `StockMovements` is append-only: no update path, no delete path, `internal`
constructor, private setters everywhere. `GET /api/inventory/movements` filters by product,
warehouse, inventory item, user, type and date range, but the date range is a convenience for
the reader — it never limits what's kept.

**Only Warehouse Operators and Administrators adjust stock; Managers are read-only.** This is
the `CanAdjustStock` policy, and it guards both stock endpoints — the adjustment and the
creation of an inventory record, since creating the record is the first step of putting stock
somewhere.

**Administrators inherit Warehouse Operator rights.** No role hierarchy, no claims
transformation; `CanAdjustStock` simply lists both roles. For four roles and two policies, a
hierarchy would be more machinery than the problem justifies. Note it doesn't run the other
way — `CanManageCatalog` is Administrator only, so a Warehouse Operator cannot create products
or warehouses. Adjusting stock and defining the catalog are different jobs.


Products need a unique SKU. It's the identifier people actually use, and duplicates make stock
questions unanswerable. Enforced twice: the handler checks first so you get a readable 409
("A product with SKU 'X' already exists."), and `IX_Products_Sku` is a unique index so a
concurrent insert can't sneak past the check. Product *names* are deliberately not unique.
Warehouse `Code` and category `Name` follow the same pattern.

A product has at most one category, and the category is optional — a nullable `CategoryId` FK
rather than a many-to-many. Multi-category tagging is a real requirement in some catalogs, but
nothing in the brief suggested it, and it costs a join table plus a whole set of endpoints.
Optional rather than required because you shouldn't have to invent a taxonomy before you can
enter your first product.

Nothing is hard-deleted. `IsActive` exists on `Category`, `Product` and `Warehouse`, and all
the FKs are `Restrict` rather than `Cascade`. Deleting a warehouse that has stock history
would destroy audit records, and given how strongly the "reviewable forever" answer was
phrased, I read that as ruling out destructive deletes generally, not just for movements.

Movement types constrain the direction of the change, which is a stronger reading than "the
type is a label". `Receipt` must increase; `Issue`, `Damage` and `Loss` must decrease;
`CountCorrection` and `Other` can go either way. Without this, a typo'd sign turns a
write-off into a delivery and the movement history stops meaning anything. `CountCorrection`
is unconstrained because a physical count can find more or fewer than expected — that's its
whole purpose.

Creating an inventory record writes no movement. Nothing moved; a product is now merely
stockable in a warehouse at quantity zero. A zero-delta movement would be a lie, and the
domain rejects `delta == 0` anyway.

Quantities are whole units — `int` throughout, no decimals. Fine for discrete goods, wrong for
anything sold by weight or volume. If the catalog ever needs "12.5 kg", this is a schema
change, and I'd rather flag it than pretend `decimal` everywhere was free.

Reads are open to every authenticated employee. All four roles can list products, view
warehouse inventory and read the movement history. The brief separated who can *change* things;
it said nothing about restricting who can look, and inventory visibility is normally the point
of the system. Anonymous access gets nothing but the login endpoint.

`SalesAgent` is seeded but has no capability in this milestone. It satisfies neither policy, so
it can log in and read. It exists because ordering is coming and the role will matter then; I'd
rather the role and its seeded user already be there than retrofit them later.

"Maintain products and warehouses" was scoped to create, list and get. No update, no
deactivate, no delete. With four days I chose to make the stock workflow genuinely complete —
domain rules, concurrency, audit trail, authorization, tests — rather than spread thin CRUD
across every entity. The reviewable business process is the one the milestone is actually
about.

## Known limitations

These are all confirmed against the code or a running instance, not suspicions.

**The audit columns on the catalog entities are dead.** `BaseEntity` gives `Category`,
`Product`, `Warehouse` and `InventoryItem` a `CreatedAt`, `UpdatedAt`, `CreatedByUserId` and
`UpdatedByUserId`, and nothing populates them. There is no `SaveChanges` interceptor and no
handler sets them. `CreatedAt` is non-nullable, so every row stores `default(DateTime)` —
`0001-01-01 00:00:00.0000000`. I checked this against the seeded rows in the running database
rather than assuming it. The fix is one interceptor reading `ICurrentUser`, which I ran out of
time for.

Stock attribution is not affected by this and shares none of its plumbing.
`StockMovement.PerformedByUserId` and `OccuredAt` are set in the entity's constructor from the
authenticated caller and the clock, they are covered by tests, and I verified live that an
adjustment made by the seeded operator comes back with that operator's user ID. The audit
trail that the milestone is graded on is correct; four columns on the catalog tables are not.

**`OccuredAt` is misspelled.** It should be `OccurredAt`. The typo is baked into the entity
property, the column, the two index names that mention it, both migrations, the model
snapshot, and the JSON field clients see. Renaming it means a third migration renaming a
column and recreating those two indexes, plus a breaking change to the response contract — for one letter. Out of scope now; it goes with the next real schema change. Details in DATA_MODEL.md.

**`IsActive` can be filtered but never set.** `GET /api/products`, `/api/categories` and
`/api/warehouses` all accept an `isActive` query parameter, and new rows default to `true`.
There is no endpoint to flip it, because there is no update endpoint at all. The soft-delete
mechanism is in the schema and half-wired to the API; the other half is a milestone-2 update
endpoint.

**Validation failures return a 400 without the field errors.** This is the one I'd fix first.
`ExceptionHandlingMiddleware` builds a `ValidationProblemDetails` (which carries the
per-field `Errors` dictionary) but assigns it to a variable typed `ProblemDetails`, and
`WriteAsJsonAsync` serializes using the declared type. The `Errors` dictionary is silently
dropped. A `POST /api/products` with an empty SKU and name really returns:

```json
{"title":"Validation failed.","status":400,"instance":"/api/products"}
```

The status and title are right; the caller just isn't told which fields failed. The fix is to
serialize as the runtime type, but it's a code change and this pass was documentation.

**MediatR logs a license warning.** On the first request that goes through the pipeline — not
at startup, despite what I'd assumed:

```
warn: LuckyPennySoftware.MediatR.License[0]
      You do not have a valid license key for the Lucky Penny software MediatR. This is
      allowed for development and testing scenarios. If you are running in production you
      are required to have a licensed version.
```

Explicitly permitted for development and testing, which is what this is. A production
deployment would need a paid key, or MediatR would need to go — the handlers themselves don't
depend on it for anything but dispatch.

**FluentAssertions 8 is non-commercial only.** Version 8.10.0's bundled LICENSE is an Xceed
"Community License Agreement (for Non-Commercial Use)"; anything commercial requires a paid
licence. Same category of problem as MediatR: fine for an assessment project, a real decision
for a product. Worth noting that on this machine it produces no warning — a clean rebuild
reports zero warnings and `dotnet test` output is silent about it — so the constraint is in
the licence text, not in anything the build will remind you about.

What remains is 12 domain tests, all passing, over `InventoryItem.ApplyAdjustment`.

I verified authorization by hand against the running API instead, and specifically:

- an anonymous `POST /api/inventory/items/{id}/adjustments` returns **401**
- the same request as `manager@fulfillment.local` returns **403**
- the same request as `operator@fulfillment.local` returns **200**, with the movement in the
  response body attributed to that operator's user ID and the quantity incremented

I also checked that a Manager and a Warehouse Operator both get 403 from `POST /api/products`,
and that an over-issue attempt from the operator returns 422 with `Stock cannot go below zero.
Current quantity is 14 and the requested change is -99999.` This is manual verification, not
automated regression coverage — nothing stops a future change from breaking it silently.

**The controllers reference `Fulfillment.Infrastructure.Identity`.** They do it only to reach
the `Policies` and `Roles` constants, so no behaviour leaks across the boundary, but it's an
Api → Infrastructure coupling that the rest of the architecture avoids. Those two constant
classes describe application-level authorization concepts, not infrastructure, and they belong
in `Fulfillment.Application`. Moving them is a small change I'd make given more time — I
noticed it too late to redo the `using` statements safely.

**`CancellationToken` isn't threaded all the way down.** `IIdentityService.AuthenticateAsync`
takes only an email and password, so the token that the controller and MediatR pipeline are
carefully passing around stops at the login handler; the `UserManager` calls underneath it
can't be cancelled. `DataSeeder.SeedAsync` has the same gap. Everything else — every query
handler, every `SaveChangesAsync` — passes it properly. Cosmetically inconsistent rather than
harmful at this scale, but it's the kind of gap that gets copied.
