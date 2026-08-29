# Phase 3 — Launch Planning & Operations

## Scope

Admin-only **planning** layer on Route Demand + Pricing + Launch Readiness.

Does **not**: create Trip, Booking, assign Captain, reserve vehicle, mutate demand, or invent money.

## APIs

| Method | Path |
|--------|------|
| GET | `/api/v1/admin/route-demand/launch-plan` |
| GET | `/api/v1/admin/route-demand/vehicle-capacities` |

Filters: `routeKey`, `vehicleType`, `launchStatus`, `readyToLaunch`, `pricingAvailable`, `search`.

## Formulas

- Planning status mapped from Phase 2 readiness (`READY`→`READY_TO_LAUNCH`, `NOT_READY`→`COLLECTING_DEMAND`, …).
- `expectedSeats` = `targetOccupancy` (not capacity fill).
- `gross` = `oneWayPrice × expectedSeats` via `ShuttlePricingCalculator`.
- Commission split via `ShuttleFinancialCalculator`.
- Financials **null** when pricing missing or demand = 0 (never fake zeros).
- `isEstimate = true` when financials present.

## Vehicle capacity

`GET .../vehicle-capacities` returns fleet max capacity per type (`VEHICLE_MASTER`) or hints 4/13/24 (`DEFAULT_HINT`). UI displays backend capacity only — marketing 3/33 not used.

## Admin

Route: `/launch-planning` — KPI cards, table, details modal, CSV export, copy summary. No launch mutation button.

## Limitations

- Unlinked corridors → `DATA_INCOMPLETE` / no pricing (same as Phase 2).
- Ambiguous Route.Name keys → `AMBIGUOUS_ROUTE_MATCH`.
- Demand > capacity → reason `DEMAND_EXCEEDS_CAPACITY`; may show `FULL` when readiness was Ready/AlmostReady.

## Out of scope

Ride, Group, Payment, FCM, Rider/Captain apps, Phase 4.
