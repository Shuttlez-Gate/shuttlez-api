# Phase 3.1 — Mobile Contract (Backend / Admin Hardening)

**Audience:** Rider / Captain / Admin integrators for Phase 4.  
**Scope of this phase:** Harden and document existing contracts. **No Rider app, Captain app, Payment, or FCM changes.**

---

## 1. Trip lifecycle

Exact enum: `TripStatus` (`Shuttlez.Domain.Enums.TripStatus`)

| Value | Int | Meaning |
|-------|-----|---------|
| `Scheduled` | 1 | Created; may be unassigned |
| `DriverAssigned` | 2 | Driver set **or** seats depleted via `MarkTripFullIfNeeded` (legacy quirk — see limitations) |
| `InProgress` | 3 | Trip started |
| `Completed` | 4 | Finished |
| `Cancelled` | 5 | Cancelled / soft-deleted Admin cancel |

Conceptual flow:

```
Scheduled → DriverAssigned → InProgress → Completed
                ↘ Cancelled
Scheduled ─────────→ Cancelled
```

Admin launch creates `Scheduled` (no driver) or `DriverAssigned` (optional `driverId`).

---

## 2. Trip statuses (API strings)

Admin parse (`AdminMapper`): `scheduled`, `driverassigned` / `driver_assigned`, `inprogress` / `in_progress`, `completed`, `cancelled` / `canceled`.

Driver list UI mapping: `InProgress`→`active`; `Completed`/`Cancelled`→`finished`; past scheduled→`overdue`; else `upcoming`.

---

## 3. Trip discovery API (Rider)

**Reuse existing — do not invent a second catalog.**

| Method | Route | Auth |
|--------|-------|------|
| `GET` | `/api/v1/bookings/preview` | JWT (Passenger) |

Query (geo corridor match → bookable trips on matched route for ~6 workdays):

- `sourceLatitude`, `sourceLongitude`
- `destinationLatitude`, `destinationLongitude`
- optional addresses / times
- `vehicleTypeIndex` (default 1)

Server filter for offers:

- Same matched `RouteId`
- Status `Scheduled` **or** (`DriverAssigned` **and** `AvailableSeats > 0`)
- `ScheduledAt` in visible day window
- Optional vehicle type via assigned driver’s vehicle

**No** `GET /api/v1/trips` browse-by-route catalog exists. Phase 4 may add an additive catalog only if product requires non-geo discovery — **not** in 3.1.

---

## 4. Trip details API (Rider)

| Method | Route | Auth | Notes |
|--------|-------|------|-------|
| `GET` | `/api/v1/trips/me` | JWT | User’s booked trips |
| `GET` | `/api/v1/trips/{id}` | JWT | Details for **own booking** on that trip |
| `GET` | `/api/v1/trips/{id}/invoice` | JWT | Own booking invoice |
| `POST` | `/api/v1/trips/{id}/cancel` | JWT | Cancel own booking (≥2h before `ScheduledAt`; trip not InProgress/Completed/Cancelled) |

Rider trip DTOs do **not** expose commission, readiness, mapping, or Admin notes.

---

## 5. Booking API

| Method | Route | Auth |
|--------|-------|------|
| `POST` | `/api/v1/bookings` | JWT |

Body:

```json
{ "tripId": "...", "seatCount": 1, "paymentMethod": "cash" }
```

Server behavior:

1. Serializable transaction + per-user advisory lock  
2. Trip must be bookable status + `!IsDeleted`  
3. `ScheduledAt` > now  
4. No duplicate active booking for same user+trip  
5. Atomic `TryDecrementTripSeatsAsync`  
6. Snapshot: `PricePerSeat` from **Trip**; commission via `ShuttleCommissionResolver`; totals via `ShuttleFinancialCalculator`  
7. Booking status `Confirmed` (existing behavior)

Demand is **never** converted to Booking by launch.

---

## 6. Captain Trip API

| Method | Route | Auth |
|--------|-------|------|
| `GET` | `/api/v1/drivers/me/trips` | JWT role `Driver` |

Returns only trips where `Trip.DriverId ==` authenticated driver’s id.

Fields (operational): schedule labels, from/to, passengers, earnings estimate, status, `ScheduledAt`, optional route id / vehicle asset.

**No** Captain endpoints to start/complete/cancel trips or change seats (Phase 4+).

---

## 7. Request/response DTOs (summary)

| Surface | Key types |
|---------|-----------|
| Preview | `BookingPreviewDto`, `ShuttleOfferDto` (`Id`=TripId, `PricePerSeat`, `AvailableSeats`, …) |
| Create booking | `CreateBookingRequest` → `CreateBookingResponse` |
| Rider trips | `TripListResponse`, `TripDetailsDto`, `TripInvoiceDto` |
| Driver | `DriverTripListResponseDto`, `DriverTripCardDto` |
| Admin launch | `LaunchRouteDemandRequest` → `RouteDemandLaunchResultDto` |
| Admin trips | `SaveTripRequest`, `AdminTripDto` |

---

## 8. Authentication requirements

| Role | Claim `role` | Trip launch | Book | Driver trips |
|------|--------------|-------------|------|--------------|
| Admin | `Admin` | Yes (`[AdminOnly]`) | Admin booking APIs | N/A |
| Passenger | `Passenger` | **No** | Yes | **No** |
| Driver | `Driver` | **No** | Own passenger bookings only if also user | Own assigned trips |

Controllers are globally `[AllowAnonymous]` at ASP.NET level; **handlers / AdminOnly enforce auth**. Mobile must still send Bearer JWT.

---

## 9. Price semantics

| Concept | Source | Mutable after create? |
|---------|--------|------------------------|
| **Trip.PricePerSeat** | Launch: `PricingRule.OneWayPrice` via resolver + normalize. Admin create: request body. | **No** — Admin update rejects change (`TRIP_PRICE_IMMUTABLE`) |
| **Booking.PricePerSeat** | Copied from Trip at booking time | Not rewritten by existing APIs |
| Later PricingRule edit | Affects **new** launches / new trips only | Does not rewrite existing Trip or Booking |

Frontend must **never** treat UI price as authoritative for create.

---

## 10. Seat semantics

| Concept | Meaning |
|---------|---------|
| **AvailableSeats** | Remaining seats on Trip (starts at authoritative capacity on launch) |
| **Demand** | Interest only — **not** seats reserved |
| Reservation | Atomic SQL update: seats ≥ count **and** status ∈ {Scheduled, DriverAssigned} |
| Cancel booking | Increments seats (rider cancel / Admin deactivate) |
| Oversell protection | `TryDecrementTripSeatsAsync` + serializable txn; Postgres concurrency test exists |

Vehicle master capacities (SoT): CarShuttle=4, MiniBus=13, Bus=24 — resolved from backend VehiclesSet / readiness, **not** Angular constants.

---

## 11. Cancellation semantics

- **Trip Cancelled / soft-deleted:** not bookable (query + decrement gate).  
- **Admin DELETE trip:** blocked if active bookings; sets `Cancelled` + `IsDeleted`.  
- **Booking Cancelled:** seats returned; financial snapshot fields retained on row.  
- Do not hard-delete historical trips with history.

---

## 12. Error codes (stable)

Booking:

- `SEAT_UNAVAILABLE` (409)
- `TRIP_NOT_BOOKABLE`
- `PRICING_NOT_AVAILABLE`
- `DUPLICATE_BOOKING`
- `INVALID_SEAT_COUNT`
- `SUBSCRIPTION_*`

Launch / trip hardening:

- `ROUTE_DEMAND_NOT_FOUND`
- `ROUTE_NOT_FOUND`
- `ROUTE_NOT_LINKED`
- `NOT_READY_TO_LAUNCH`
- `PRICING_NOT_CONFIGURED`
- `NO_VEHICLE_CONFIG` / `NO_VEHICLE_AVAILABLE`
- `INVALID_SERVICE_DATE`
- `DUPLICATE_OPERATIONAL_TRIP`
- `DRIVER_NOT_FOUND`
- `TRIP_PRICE_IMMUTABLE`

Returned in `ApiResponse.code` (camelCase JSON).

---

## 13. Date/time contract

- Storage / comparison: **UTC** (`DateTime` Kind handled via `ToUtc` / `IDateTimeProvider.UtcNow`).  
- Launch: `scheduledAt` **required**, must be **> UtcNow**. No silent `DateTime.Now` / default 08:00.  
- Admin UI should send ISO UTC (`toISOString()`).  
- PostgreSQL `timestamptz` via EF — preserve UTC convention.

---

## 14. Authorization (who sees what)

| Data | Rider | Captain (assigned) | Admin |
|------|-------|--------------------|-------|
| Trip schedule, route ends, seats, trip price | Via preview / own booking | Yes (own trips) | Yes |
| Commission / captain earnings formulas | No | Card may show earnings estimate | Yes (earnings APIs) |
| Route demand / readiness / mapping | No | No | Yes |
| Other captains’ trips | No | No | Yes |

---

## 15. What Rider sees

Operational booking surface: offers (trip id, times, addresses, plate label, seats, price/seat), own trip list/details/invoice. No readiness, commission rules, or demand metadata.

---

## 16. What Captain sees

Own assigned trips only (`drivers/me/trips`). No Admin launch. No other drivers’ trips.

---

## 17. What Admin sees

Full Route Demand, readiness, launch, trips CRUD (price immutable after create), bookings, pricing, commission, earnings.

Launch body may only choose: `scheduledAt`, optional `driverId`, optional `vehicleId` (capacity resolution). Never price/capacity/commission from UI as authority.

---

## Distinctions (mandatory)

| Term | Is |
|------|-----|
| Trip price | Snapshot on Trip at create/launch |
| Booking price | Snapshot on Booking at book time |
| Commission | Resolved at book time → Booking fields |
| Captain earnings | Booking snapshot split |
| Demand | Not seats, not booking |
| Available seats | Remaining Trip inventory |

---

## Duplicate Trip protection

Launch: serializable transaction + PostgreSQL advisory lock + reject non-cancelled Trip with same `RouteId` + `ScheduledAt`.  
**No** DB unique index (cancellation semantics). Do not weaken to app-only check.

---

## Known limitations (Phase 4 awareness)

1. No non-geo trip catalog for riders.  
2. `MarkTripFullIfNeeded` sets status `DriverAssigned` when seats hit 0 without a driver — semantic quirk; do not “fix” without product decision.  
3. `POST /api/v1/customer-trips` can create trips with hardcoded price/seats — side channel outside launch (out of 3.1 rewrite scope).  
4. Corridor demand apply may default price — Admin ops path.  
5. Trip has **no** `VehicleId` column — launch `vehicleId` resolves capacity only.  
6. No Captain start/complete trip APIs yet.  
7. Controllers AllowAnonymous + handler auth — new endpoints must not skip handler checks.

---

## Production

Read-only verification only in this phase. No seed, no mass launch, no fake trips/bookings.
