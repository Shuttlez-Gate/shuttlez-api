# Phase 4B — Captain Trip Lifecycle

## Principle

Captain lifecycle does **not** create Trips or Bookings.  
Admin launches Trips; Captain only **starts** and **completes** trips assigned to them.  
Backend is the source of truth for every transition.

## Lifecycle

```
Scheduled / DriverAssigned  →  InProgress  →  Completed
         ↘ Cancelled (terminal)
```

Existing `TripStatus` enum reused (no new enum).

## APIs

| Method | Route | Body |
|--------|-------|------|
| GET | `/api/v1/drivers/me/trips` | — (existing) |
| POST | `/api/v1/drivers/me/trips/{tripId}/start` | none |
| POST | `/api/v1/drivers/me/trips/{tripId}/complete` | none |

Auth: JWT + Driver role. Ownership: `Trip.DriverId` must equal authenticated driver's id. Client must **not** send driverId/price/status.

### Start rules

- Allowed: `Scheduled`, `DriverAssigned`
- Idempotent if already `InProgress`
- Rejected: unassigned, other captain, `Cancelled`, `Completed`
- Sets `StartedAt` (UTC) when first started

### Complete rules

- Allowed: `InProgress` only
- Idempotent if already `Completed`
- Rejected: `Scheduled`/`DriverAssigned`/`Cancelled`
- Sets `CompletedAt` (UTC) when first completed

### Concurrency

Serializable transaction + PostgreSQL advisory lock keyed by trip id.

## Error codes

| Code | Meaning |
|------|---------|
| `TRIP_NOT_ASSIGNED` | No driver / not this captain |
| `TRIP_NOT_STARTABLE` | Wrong status for start |
| `TRIP_NOT_COMPLETABLE` | Wrong status for complete |
| `TRIP_CANCELLED` | Cancelled trip |
| `TRIP_ALREADY_STARTED` / `TRIP_ALREADY_COMPLETED` | Reserved for clients; server returns success message on idempotent path |

## Flutter Captain

- Reuses existing Live Trip UI funnel
- `markTripStarted` / `markTripFinished` call backend POSTs
- Confirm dialogs before start/complete
- Double-tap protected via `actionInFlight`
- No optimistic permanent status on API failure
- Live boarding simulation remains local UI (mock riders) — status sync is real

## Booking interaction

Start/Complete do **not** modify Booking financial snapshots, PricingRule, or CommissionRule.  
`TripBookability` already excludes `Completed` / non-bookable statuses from new bookings.

## Database

No migration. Uses existing `StartedAt` / `CompletedAt` on `Trip`.

## Known limitations

- Trip has no `VehicleId`
- Live trip rider/boarding still catalog-simulated
- No DB unique on `(RouteId, ScheduledAt)`
- Local prefs still cache started/finished ids as UX merge aids after successful API calls
