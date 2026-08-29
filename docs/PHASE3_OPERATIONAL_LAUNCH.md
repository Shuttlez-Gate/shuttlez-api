# Phase 3 — Operational Launch Foundation

## Scope

Admin-controlled transition from launch readiness (Phase 2) to creating a single operational **Trip**.

Hard rules enforced:

- Backend re-evaluates readiness at launch time (never trusts Angular).
- Only calculator status **`READY`** is launchable.
- Demand ≠ Booking — launch does **not** convert demand rows to bookings.
- No automatic captain assignment unless Admin optionally passes `driverId`.
- No Group entity invented (none exists in the operational Trip domain).
- No fake routes / trips / vehicles / bookings.
- No Rider / Captain / Payment / FCM changes.
- Unlinked demand corridors are **not** auto-mapped.

## API

`POST /api/v1/admin/route-demand/launch?routeKey={key}`

Request (Admin-chosen only):

```json
{
  "scheduledAt": "2026-08-25T08:00:00Z",
  "driverId": null,
  "vehicleId": null
}
```

Server resolves: RouteId, PricePerSeat (PricingRuleResolver), AvailableSeats (vehicle master or selected Vehicle), CommissionPercent (ShuttleCommissionResolver), Status.

## Eligibility

`RouteDemandLaunchEligibility` requires:

1. Demand group found
2. Readiness present
3. `PricingLinked` + `RouteId`
4. `LaunchStatus == READY`
5. `scheduledAt` in the future
6. Active Route
7. Resolvable vehicle type/capacity (or explicit Vehicle)
8. Active PricingRule with OneWayPrice > 0

## Trip behavior

Creates one `Trip` with:

- `RouteId`, `ScheduledAt`, `PricePerSeat`, `AvailableSeats`
- Optional `DriverId` → Status `DriverAssigned`, else `Scheduled`
- `ReferenceCode` generated

Does **not** set Trip.VehicleId (field does not exist on Trip).

## Idempotency / concurrency

Serializable transaction + PostgreSQL advisory lock on `(RouteId, ScheduledAt)`.

Rejects if a non-cancelled Trip already exists for the same Route + ScheduledAt (`DUPLICATE_OPERATIONAL_TRIP`).

## Database

**No migration** in Phase 3. Existing Trip schema is sufficient.

## Admin UI

Route Demand:

- «تشغيل الخط» enabled only when readiness is READY + linked
- Confirmation modal with Arabic copy that demand is not auto-booked
- Requires Admin-selected datetime
- Success shows Trip id + link to `/trips?routeId=`

## Tests

- Backend: `RouteDemandLaunchEligibilityTests`
- Admin: `launch-readiness.spec.ts` (`canShowLaunchAction`), `api-errors.spec.ts` launch codes

## Production impact

Zero automatic writes. Launch only via authenticated Admin action. Works with 0 READY corridors (UI empty / disabled).
