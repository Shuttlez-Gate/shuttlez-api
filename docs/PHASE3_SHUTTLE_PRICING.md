# Phase 3 — Shuttle pricing, commission, concurrency, subscription usage

## Phase 3.1 — production fixes

- **Multi-seat:** `SelectShuttleArgs.seatCount` → create booking `seatCount`.
- **Offline pricing:** no seed `100 EGP`; API down → pricing unavailable + Retry; confirm blocked if `pricePerSeat <= 0`.
- **Subscription:** `subscription` payment + exhausted → `SUBSCRIPTION_LIMIT_REACHED`; expired → `SUBSCRIPTION_EXPIRED`; `cash` never consumes credits.
- **Concurrency:** Serializable tx + `pg_advisory_xact_lock` per user; seat race covered by Testcontainers PostgreSQL test.

Scope: scheduled Shuttle only. No On-Demand Ride, Group booking, payment gateway, or FCM.

## Pricing flow

1. **Source of truth:** `Trip.PricePerSeat` (admin/ops set per trip; seed default `100` EGP for generated trips).
2. Rider preview (`GET` booking preview) returns `pricePerSeat`, seat labels, and display string from the trip.
3. Rider create booking sends only `TripId` + `SeatCount` (+ payment method). **No client totals.**
4. Server computes `TotalAmount = Round(PricePerSeat × SeatCount, 2)` via `ShuttleFinancialCalculator`.
5. Booking snapshots `PricePerSeat`, `TotalAmount`, commission fields. Invoice `Amount = TotalAmount`.

Hardcoded / seed values (not deleted):

| Value | Where | Role |
|------|--------|------|
| `100` | Trip seed `PricePerSeat` | Demo/ops default trip fare |
| `800` / `1200` / `2200` | Subscription package seed prices | Catalog package prices |
| `1000` / `1500` / `2800` | `oldPrice` in package descriptions | Marketing strikethrough |
| `PlatformCommissionPercent: 0` | `appsettings` + seed commission rule | Launch configurable default (not a permanent commercial policy) |
| Rider booking-preview seed | Offline/local only | No fake prices (`pricePerSeat=0`); **not used** by production repository |

## Commission flow

1. `IShuttleCommissionResolver` resolves platform % from active `CommissionRule`, else launch-offer inversion (`100 - CaptainLaunchOffer.ProfitPercent`), else `ShuttleCommission:PlatformCommissionPercent`.
2. `CommissionAmount = Total × rate`; `CaptainEarnings = Total − Commission`.
3. Admin API: `GET/POST/PUT api/v1/admin/commission-rules` (existing Admin auth).
4. Captain trip list earnings sum `CaptainEarnings` when `Booking.PricePerSeat > 0` (Phase-3 snapshot); legacy rows fall back to `TotalAmount`.

**Business decision still required:** post-launch permanent platform commission %. Launch default is **0% platform / 100% captain**, configurable — not hardcoded forever.

## Seat concurrency

1. Atomic PostgreSQL update: `ExecuteUpdate` where `AvailableSeats >= seatCount`.
2. Rows affected `0` → HTTP **409** + code `SEAT_UNAVAILABLE`.
3. If booking persist fails after decrement → compensating `TryIncrementTripSeatsAsync`.
4. Cancel restores seats the same way.

## Subscription usage

1. Usage = sum of `SeatCount` on **Confirmed** bookings with `UsesSubscriptionCredit` in the activation window.
2. Cancelled bookings do **not** count (credits restored by status change).
3. Failed bookings never set the flag / never save.
4. Exhausted package → cash booking without credit (does not throw `SUBSCRIPTION_LIMIT_REACHED` yet).
5. Expired package → cash path (no credit).
6. Rider reads `usedTrips` / `remainingTrips` from package list / subscribe / `GET .../subscription-packages/me`. Demo counters removed from production paths.

## Error codes

| Code | HTTP | Meaning |
|------|------|---------|
| `SEAT_UNAVAILABLE` | 409 | Concurrent/oversold seat |
| `INVALID_SEAT_COUNT` | 400 | SeatCount ≤ 0 |
| `PRICING_NOT_AVAILABLE` | 400 | Negative trip price |
| `SUBSCRIPTION_EXPIRED` | reserved | Not thrown on book (cash fallback) |
| `SUBSCRIPTION_LIMIT_REACHED` | reserved | Not thrown on book (cash fallback) |
| `TRIP_NOT_BOOKABLE` | 400/404 | Trip missing/past |
| `DUPLICATE_BOOKING` | 400 | Same user already booked trip |

## Rider 409 UX

Arabic: المقعد لم يعد متاحًا، برجاء تحديث الرحلة والمحاولة مرة أخرى.  
English: The seat is no longer available. Please refresh the trip and try again.  
Then preview reloads.

## Migration

`20260820122126_Phase3ShuttlePricingCommissionConcurrency`

- Booking: `PricePerSeat`, `CommissionRate`, `CommissionAmount`, `CaptainEarnings`, `UsesSubscriptionCredit`
- User: `SubscriptionActivatedAt`
- Table: `CommissionRulesSet`
- Money precision `numeric(18,2)` on booking/invoice/trip prices
- SQL backfill: legacy bookings → `CaptainEarnings = TotalAmount`

## Not in this phase / remaining

- Full concurrent booking integration test harness (WebApplicationFactory + isolated Postgres) — unit coverage for money + subscription math is present; concurrent 409 relies on DB atomic update.
- Subscription credit TOCTOU under extreme concurrency (two credit bookings racing) — seats are atomic; credit flag is check-then-write. Prefer Serializable tx or credit counter if product requires hard guarantees.
- Admin Panel UI for commission rules (API only).
- Captain App UI changes (API already returns earnings).
- Permanent commercial commission % after launch.
- Enforcing `SUBSCRIPTION_LIMIT_REACHED` instead of cash fallback.
