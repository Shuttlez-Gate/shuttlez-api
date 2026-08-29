# Phase 2 — Route Demand × Pricing × Launch Readiness

## Principle

Backend is the source of truth. Accuracy over completeness.

- No fuzzy route matching.
- No invented prices, capacities, rider counts, or statuses.
- Demand ≠ confirmed bookings ≠ revenue.
- Admin visibility only — no auto launch / trip / captain assignment.

## Data sources

| Concern | Source |
|--------|--------|
| Demand corridors | Landing leads + RouteRequests aggregated by `RouteKey` |
| Demand / unique / confirmed | `TotalRequests`, unique phones, `approved`/`converted` statuses |
| Canonical route | `Routes` — only via **exact** bidirectional key from `Route.Name` |
| Pricing | `PricingRules` via same priority as `PricingRuleResolver` |
| Capacity | Fleet `Vehicles.Capacity` max per type, else hints 4 / 13 / 24 |
| Commission window | PricingRule launch/permanent via `ShuttlePricingCalculator` |
| Money math | `ShuttleFinancialCalculator` (decimal) |

## Matching strategy

1. Build demand `RouteKey` = ordered normalized From|To (existing).
2. Derive candidate keys from `Route.Name` split on `-` / `↔` (exact endpoints only).
3. Link **only** if exactly one Route matches that key.
4. Ambiguous or missing → `launchStatus=UNKNOWN`, `reasonCode=MISSING_ROUTE_LINK`, **no pricing attached**.

**No fuzzy text** (`حلوان` ≉ `حلوان، القاهرة`).

## Pricing resolution (only when RouteId linked)

1. Active Route + VehicleType rule  
2. Else active VehicleType default (`RouteId` null)  
3. Else → `NO_PRICING`

EffectiveFrom / EffectiveTo / IsActive respected in batch load.

## Launch readiness statuses

| Status | When |
|--------|------|
| `UNKNOWN` | No safe RouteId, missing vehicle, or invalid capacity |
| `NO_PRICING` | Linked route but no active PricingRule |
| `MISSING_CONFIGURATION` | Pricing present but min/target missing or invalid (e.g. target > capacity) |
| `NOT_READY` | Confirmed &lt; minimumLaunchRiders |
| `ALMOST_READY` | Confirmed ≥ minimum **and** &lt; targetOccupancy (explicit threshold only) |
| `READY` | Confirmed ≥ targetOccupancy |

Launch metric: **`confirmedPassengers`** (not raw demand).

### Formulas

```
occupancy% = confirmed / capacity × 100   (null if capacity missing/≤0)
ridersRequired = max(minimumLaunchRiders - confirmed, 0)   (null if min missing)
financialAtMinimum* = prices × minimumLaunchRiders + commission split
```

Financial preview uses **configured minimumLaunchRiders only** — never invents seat counts from demand.

## API

Extended existing:

- `GET /api/v1/admin/route-demand` → `readiness` on each row  
- `GET /api/v1/admin/route-demand/details?routeKey=` → `readiness`  
- Filters: `launchStatus`, `pricingAvailable`, `readyToLaunch`

Admin auth unchanged.

## Admin UI

Route Demand table/detail: pricing, capacity, confirmed, occupancy, min, riders remaining, captain earnings at minimum, status + reason.

Shows `لا يوجد تسعير` / `غير محدد` when backend says so — never hardcodes 75/120/150.

## Database

**No migration.** Reuses PricingRule fields (`MinimumLaunchRiders`, `TargetOccupancy`, prices, launch window).

## Security

Admin endpoints only. Rider/Captain apps unmodified. No Rider API exposure of commission/pricing readiness.

## Tests

Backend unit tests: statuses, occupancy on confirmed, financial at minimum, exact key match, route-specific pricing override, snapshot immutability.

Admin: Arabic status labels / chips.

## Known limitations

1. Most demand corridors have **no** `RouteId` until Admin creates a Route whose name endpoints match exactly → they correctly show **UNKNOWN**.
2. Demand-band vehicle recommendation (legacy 3/13/33 UI) is not capacity SoT; readiness uses fleet/hints.
3. `ALMOST_READY` requires both min and target on PricingRule; if only min existed historically, behavior is NOT_READY/READY against that single bar (target null → MISSING_CONFIGURATION).
4. Operational Admin status (`collecting_demand`, etc.) is separate from launch readiness.

## Out of scope (STOP)

Ride, Group, Payment, FCM, dispatch, auto trip/booking/captain, Phase 3+.
