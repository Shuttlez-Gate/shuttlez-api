# Phase 2.1 — Production Data Reconciliation

**Status:** Complete — STOP (no Phase 3+ / Ride / Trip auto-launch)  
**Date:** 2026-08-20  
**Scope:** Accuracy hardening for Admin Route Demand × Pricing × Launch Readiness against the live Neon database.

## Principle

Backend is the source of truth. Accuracy > completeness.  
No fuzzy route matching is used.

---

## 1. Capacity source of truth

| Priority | Source | Values observed (live) |
|----------|--------|-------------------------|
| 1 | `VehiclesSet.Capacity` max per `VehicleType` among active, non-deleted vehicles | CarShuttle **4**, MiniBus **13**, Bus **24** (`VEHICLE_MASTER`) |
| 2 | Default hints in `RouteDemandReadinessEnricher` (only if no fleet row) | CarShuttle 4, MiniBus 13, Bus 24 (`DEFAULT_HINT`) |

**Not authoritative (marketing / demand-band heuristic only):**

| Label | Heuristic capacity (`RecommendVehicle`) |
|-------|----------------------------------------|
| Shuttlez Car | 3 |
| Microbus | 13 |
| MiniBus | 33 |

`RouteDemandReadinessDto` now exposes:

- `Capacity` — authoritative
- `CapacitySource` — `VEHICLE_MASTER` | `DEFAULT_HINT`
- `DemandBandCapacity` — heuristic (not SoT)
- `HasCapacityConflict` — true when band ≠ authoritative

Admin UI labels authoritative capacity and shows amber conflict text (e.g. تقدير الطلب 3 ≠ المعتمدة 4). Occupancy uses **confirmed ÷ authoritative capacity**.

---

## 2. Route matching strategy

Allowed (unchanged Phase 2):

1. Exact bidirectional canonical `RouteKey` derived from `RoutesSet.Name` endpoints (normalized)
2. Unique match only — ambiguous → no link

Never used: contains, startsWith, similarity, Levenshtein, partial Arabic matching, city guessing.

If no safe match: `pricingLinked = false`, status `UNKNOWN`, reason `MISSING_ROUTE_LINK`, prices null.

---

## 3. Demand semantics

| Field | Meaning | Used for readiness? |
|-------|---------|---------------------|
| **Demand** (`TotalRequests`) | All corridor requests (landing + app) | Display / estimates context only |
| **Unique** | Distinct phones | Display only |
| **Confirmed** | App requests with status approved/converted (landing = not confirmed) | **Yes** — occupancy, min/target remaining |

Do not silently replace Demand with Confirmed.

---

## 4. Pricing resolution

Uses existing batch pick aligned with `PricingRuleResolver`:

1. RouteId + VehicleType  
2. VehicleType default (`RouteId` null)  
3. Else no pricing → `NO_PRICING`

Effective dates respected in enricher filter. Angular must not hardcode fares.

**Live DB (2026-08-20):** table `PricingRulesSet` **does not exist** (migration not applied). Enricher soft-fails to empty rules → accurate `NO_PRICING` when route is linked; never invents prices.

---

## 5. Commission resolution

Uses `ShuttlePricingCalculator.ResolveCommissionPercent` from the linked `PricingRule` (launch vs permanent).  
Global `CommissionRulesSet` also **missing** on live DB. Booking financial snapshots remain untouched.

Intended seed (code/seeder, not live until migrated): Launch 0%, Permanent 10%.

---

## 6. Readiness calculation

Unchanged calculator states: READY, ALMOST_READY, NOT_READY, NO_PRICING, MISSING_CONFIGURATION, UNKNOWN  
(API strings; planning layer may map labels elsewhere — Phase 2.1 does not rewrite.)

---

## 7. Financial estimates

When pricing + min available: preview at **minimum launch riders** (gross / commission / captain).  
Labeled as تقديري in Admin. Null when pricing unavailable.  
No booking/trip side effects.

---

## 8. Capacity discrepancy (documented, not auto-fixed)

| Type | Demand heuristic | Backend SoT |
|------|------------------|-------------|
| CarShuttle | 3 | **4** |
| MiniBus | 13 | **13** |
| Bus | 33 (band) | **24** |

Example: Demand=3 with Car → occupancy **75%** at capacity 4, not 100%.

---

## 9. Route mapping limitations (live)

- **31** demand groups loaded (brief mentioned ~27; live has more — not seeded/overwritten).
- **17** active `RoutesSet` rows; most demand corridors are **not** exact key matches (addresses often include `، القاهرة` while route names do not).
- Exact match example: `حلوان, القاهرة ↔ مدينة نصر, القاهرة` ↔ route `حلوان, القاهرة - مدينة نصر, القاهرة` → linked.
- Near-miss (correctly **not** linked): `المعادي، القاهرة ↔ مصر الجديدة، القاهرة` vs route `مصر الجديدة - المعادي` (`المعادي|مصر الجديدة`).

---

## 10. Data quality issues (live Neon)

1. **PricingRulesSet missing** — migrations `20260820122126_*` and `20260820131615_*` not in `__EFMigrationsHistory`.
2. **CommissionRulesSet missing** — same.
3. **UsersSet drift** — history claims `AddUserActiveSubscription` but full-entity User include failed on `SubscriptionActivatedAt`; demand path now projects only needed columns.
4. **Capacity conflicts** — 30/31 groups (Car band 3 vs SoT 4; some preferred MiniBus → SoT 13 vs band 3).
5. **MISSING_ROUTE_LINK** — 30/31 groups.
6. **NO_PRICING** — 1 linked group (Helwan/Nasr City corridor) because pricing table absent.
7. Vehicle type for readiness may come from passenger preferred type / assignment, not only demand-band recommendation — can yield AuthCap 13 while UI recommendation still says Shuttlez Car.

### Migration STOP (not applied)

Applying existing migrations would create:

- Entity/table: `PricingRulesSet`, `CommissionRulesSet` (+ related indexes/FKs already defined in repo migrations)
- Impact: enables real pricing/commission resolution; **backward compatible** if seeded after migrate
- Phase 2.1 **does not** apply migrations automatically — operator must approve

---

## 11. Tests

- Existing Phase 2 readiness tests retained
- Added: `Occupancy_CarShuttle_UsesAuthoritativeCapacityFour_NotMarketingThree` (3/4 = 75%)
- Route key / PickPricingRule unit tests unchanged (exact match only)

---

## 12. Build / verification

- Live verify tool: `tools/DemandVerify` → `verify-report.md`
- Backend + Admin builds/tests run as part of Phase 2.1 closeout

---

## Hard stop

Do **not** proceed to Ride / Group / Payment / FCM / auto Trip / Booking / Captain assignment / Phase 3+ marketplace.
