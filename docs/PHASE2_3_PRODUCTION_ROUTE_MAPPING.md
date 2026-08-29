# Phase 2.3 — Production Route Mapping + Launch Readiness Verification

**Status:** Complete — STOP  
**Date:** 2026-08-20  
**Principle:** Production data is the source of truth. Accuracy > completeness.  
**No fuzzy route matching is used.**  
**No automatic mapping was performed.**

---

## 1. Production database state

Already applied (not re-run in this phase):

- `20260820122126_Phase3ShuttlePricingCommissionConcurrency`
- `20260820131615_PhasePricingRulesAndEarnings`
- `20260820143356_Phase22_RouteDemandMappedRoute`

| Object | Status |
|--------|--------|
| PricingRulesSet | Present — **2** active vehicle defaults (CarShuttle / MiniBus) |
| CommissionRulesSet | Present — **1** active launch rule (0%) |
| MappedRouteId columns | Present |
| Explicit MappedRouteId rows | **0** |
| Vehicle capacity SoT | CarShuttle **4**, MiniBus **13**, Bus **24** (`VEHICLE_MASTER`) |

No migrations, seeds, or booking updates in Phase 2.3.

---

## 2. Total demand groups

**32** (read-only Aggregate via `IRouteDemandAnalysisService`; previously ~31).

## 3. Existing mappings

| Type | Count |
|------|-------|
| EXACT_KEY | **1** (`حلوان, القاهرة ↔ مدينة نصر, القاهرة`) |
| EXPLICIT (Admin) | **0** |
| MANUAL_OVERRIDE | **0** |

## 4. Missing mappings

**31** with `MISSING_ROUTE_LINK` / `UNKNOWN`.

## 5. Pricing availability

| Metric | Count |
|--------|-------|
| Pricing available | **1** |
| NO_PRICING | **0** |
| Linked corridor one-way | **75** (MiniBus vehicle-default from DB — not Angular) |

## 6. Commission availability

Launch commission rule active at **0%** (DB). Linked readiness uses PricingRule launch/permanent fields via existing calculator — not hardcoded in Admin.

## 7. Capacity conflicts

**31** rows with `HasCapacityConflict` (demand-band vs vehicle master). Not auto-reconciled.

## 8. Readiness distribution

| Status | Count |
|--------|-------|
| READY | 0 |
| ALMOST_READY | 0 |
| NOT_READY | **1** (linked Helwan↔Nasr City: confirmed 2 &lt; min) |
| MISSING_CONFIGURATION | 0 |
| UNKNOWN | **31** |
| NO_PRICING | 0 |

## 9. Mapping rules

Priority:

1. Explicit `MappedRouteId` → `RouteLinkSource=EXPLICIT`
2. Exact bidirectional RouteKey from `Route.Name` → `EXACT_KEY`
3. Else unlinked

Compatibility (Phase 2.3):

- `EXACT_BIDIRECTIONAL` — demand key equals derived route name key
- `MANUAL_OVERRIDE` — Admin explicit map without exact key match (allowed; labeled; never fuzzy)

APIs (unchanged contract):

- `PUT /api/v1/admin/route-demand/map-route?routeKey=`
- `DELETE /api/v1/admin/route-demand/map-route?routeKey=`

## 10. Manual mapping process

1. Admin opens «ربط المسار» on unlinked row.  
2. Searches existing routes via `GET /api/v1/admin/routes`.  
3. Confirms: «هل أنت متأكد من ربط طلب المسار بهذا الخط؟»  
4. Backend validates demand + active route + exclusive MappedRouteId.  
5. Readiness recalculated; row patched without full page reload.  
6. Unlink confirms separately; demand/route/pricing untouched.

## 11. Remaining issues

- Most corridors include governorate in demand labels while many `RoutesSet.Name` values do not → exact key miss until Admin maps or routes are renamed.  
- No READY corridors yet (demand/confirmed below mins).  
- Capacity marketing vs master conflicts remain visible.  
- Bus still has no pricing seed (by design).

## 12. Confirmation

- **No automatic mapping of the 31 unlinked groups.**  
- **No Booking / Trip / Captain writes.**  
- **No production seed / migration.**

Raw dump: `tools/DemandVerify/phase23-verify.md`
