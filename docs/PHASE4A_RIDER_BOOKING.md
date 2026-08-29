# Phase 4A — Rider Trip Discovery → Booking

## Scope

Rider Flutter consumes existing backend:

1. `GET /api/v1/bookings/preview` — discovery  
2. `POST /api/v1/bookings` — create booking  

**No** new Rider catalog endpoint. **No** Payment / FCM / Captain / Admin launch changes.

## Funnel (existing + hardened)

```
Map (Places) → SelectShuttle (vehicle + seats) → BookingPreview (offers)
  → createBooking → ReservedTrip (confirmation)
```

## Discovery

- Geo corridor match via preview query params (lat/lng/addresses/vehicleTypeIndex).
- Offers for selected day chip only.
- Empty list → `EmptyRoutesCard` / request-route CTA (no fake trips).
- Price from `pricePerSeat` only (never invented from crossed-price labels).

## Seat selection

- Chosen on `RideOptionsSheet` before preview (`SelectShuttleArgs.seatCount`).
- UI caps: **4 / 13 / 24** (vehicle master), not marketing 3/8/45.
- Pre-submit: `SeatSelectionRules.validateRequestedSeats` vs offer `availableSeats`.
- Backend remains final validator (`SEAT_UNAVAILABLE` / `INVALID_SEAT_COUNT`).

## Booking

Request body only:

```json
{ "tripId", "seatCount", "paymentMethod" }
```

`paymentMethod`: `subscription` if active package snapshot, else `cash`.

Response typed as `CreateBookingResult` — confirmation uses backend `totalAmount`, `seatCount`, `referenceCode`.

Double-tap: `_bookingInFlight` disables offer taps while request runs.

## Errors (mapped)

| Code | Arabic UX |
|------|-----------|
| `SEAT_UNAVAILABLE` (409) | refresh preview + seat message |
| `TRIP_NOT_BOOKABLE` | trip not bookable + refresh |
| `INVALID_SEAT_COUNT` | insufficient seats |
| `SUBSCRIPTION_*` | existing package messages |
| 401 | pricing/session unavailable path |

## Auth

Existing `ApiClient` JWT + refresh. No Admin APIs from Rider.

## Limitations

- Discovery is geo-preview only (no text trip catalog).
- No dedicated pre-book trip-details route (offer card is the detail).
- Cancellation unchanged (existing trips flow).
- If zero Admin-launched trips match corridor → empty state is correct.

## Tests

`test/phase4a_rider_booking_test.dart` — parsing, empty offers, seat rules, confirmation totals, Arabic labels.
