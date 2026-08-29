# Phase 2.2 — Production Activation + Route Demand Mapping

**Status:** Complete — STOP  
**Date:** 2026-08-20  

## Principle

Production data is the source of truth. Accuracy > completeness.  
**No fuzzy route matching is used.**

---

## 1. Production schema status (live Neon, pre-operator apply)

| Object | Status |
|--------|--------|
| `PricingRulesSet` | **ABSENT** |
| `CommissionRulesSet` | **ABSENT** |
| `RouteDemandGroupStatesSet.MappedRouteId` | **ABSENT** until Phase 2.2 migration applied |
| Vehicle capacities | CarShuttle **4**, MiniBus **13**, Bus **24** (`VEHICLE_MASTER`) |
| Demand groups | **31** (read-only verify) |
| Exact-key linked | **1/31** |
| Explicit Admin mappings | **0** until migration + Admin action |

---

## 2. Migration safety

### Pending (in apply order)

| Migration | Tables / ops | Destructive? | Verdict |
|-----------|--------------|--------------|---------|
| `20260820122126_Phase3ShuttlePricingCommissionConcurrency` | Creates `CommissionRulesSet`; adds Booking money columns; alters numeric precision; **SQL UPDATE** backfills booking earnings | **Mutates existing BookingsSet rows** matching filter | **NOT auto-safe** — needs operator review |
| `20260820131615_PhasePricingRulesAndEarnings` | Creates `PricingRulesSet` + indexes + FK | Additive only | Safe schema-wise after prior migration |
| `20260820143356_Phase22_RouteDemandMappedRoute` | Adds `MappedRouteId`, `MappedAt`, `MappedByUserId` + index + FK to `RoutesSet` | Additive only | **SAFE TO APPLY** (schema) |

### Production migration policy

- No `ALLOW_MIGRATE` / approval flag in repo.
- Production startup does **not** auto-migrate.
- **Migration execution BLOCKED pending operator approval.**

Do **not** claim migrations were applied unless executed successfully by an operator.

### Operator approve checklist

1. Review booking backfill SQL in `Phase3ShuttlePricingCommissionConcurrency`.
2. Apply migrations in order (dev: `dotnet ef database update`; prod: approved pipeline).
3. Seed or Admin-create pricing/commission (seeder is **Development-only**):
   - CarShuttle defaults: 120 / 240 / 1100 / 4500 (launch 0%, permanent 10%, min 3, target 3)
   - MiniBus defaults: 75 / 150 / 690 / 2850 (launch 0%, permanent 10%, min 8, target 10)
   - Commission launch rule: 0% platform (existing `DbSeeder`)
4. Apply Phase 2.2 mapping columns.
5. Use Admin «ربط المسار» for corridors that cannot exact-match.

---

## 3. Pricing activation

- Resolver unchanged: Route+Vehicle → Vehicle default → none → `NO_PRICING`.
- No Angular hardcoded prices.
- Live prices resolved today: **0** (table missing).

## 4. Commission activation

- Launch/permanent % from `PricingRule` via existing calculator.
- Global `CommissionRulesSet` still for booking path after migrate.
- Historical booking snapshots: **not modified by Phase 2.2 code** (pending migration may backfill — operator must approve).

---

## 5. Route mapping architecture

Domain owner: **`RouteDemandGroupState`** (same as status / captain assignment).

| Field | Purpose |
|-------|---------|
| `MappedRouteId` | Explicit catalog `RoutesSet.Id` |
| `MappedAt` | UTC timestamp |
| `MappedByUserId` | Admin user |

Priority:

1. **EXPLICIT** `MappedRouteId` (if route still active)
2. **EXACT_KEY** bidirectional key from `Route.Name`
3. Else `MISSING_ROUTE_LINK`

Stale explicit id → missing link (no fuzzy fallback).  
One `MappedRouteId` may not be shared by two demand corridors.

---

## 6. Matching rules

Allowed: explicit Admin map · exact bidirectional RouteKey.  
Forbidden: contains, startsWith, similarity, governorate omission, Levenshtein.

---

## 7. Capacity SoT

Unchanged Phase 2.1: fleet max / hints 4 / 13 / 24. Demand-band 3/33 not authoritative. Conflicts exposed via `HasCapacityConflict`.

---

## 8. Conflict handling

Admin shows amber conflict text; values not auto-reconciled.

---

## 9. Readiness behavior

Phase 2 calculator unchanged. Pipeline: Demand → Route link → Vehicle → Capacity → Pricing → Commission → Status.

---

## 10. Mapping API

| Method | Path | Auth |
|--------|------|------|
| PUT | `/api/v1/admin/route-demand/map-route?routeKey=` body `{ routeId }` | Admin |
| DELETE | `/api/v1/admin/route-demand/map-route?routeKey=` | Admin |

Validates demand group exists, route active, no conflicting map. No Trip/Booking/Captain side effects.

---

## 11. Admin UI

Route Demand: «ربط المسار» modal — search existing routes via `GET /api/v1/admin/routes`. Cannot create routes from dialog.

---

## 12. Production verification (read-only, migrations not applied)

From Phase 2.1 live report (still valid until operator migrates):

- Groups: 31  
- Linked (exact): 1  
- Missing link: 30  
- Pricing available: 0  
- Capacity conflicts: 30  

After mapping migration + Admin maps + pricing seed, re-run `tools/DemandVerify`.

---

## 13. Known limitations

- Exact key fails when demand labels include governorate and `Route.Name` does not.
- Explicit map requires Phase 2.2 migration.
- Pricing/commission require pending Phase pricing migrations + seed/Admin config.
- Do not invent Bus pricing.

## Hard stop

No Rider / Captain / Ride / Group / Payment / FCM / auto Trip / Booking / Captain assignment / Phase 3+.
