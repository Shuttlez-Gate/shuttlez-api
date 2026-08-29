# PHASE 6A — Ride + Group Backend Foundation

**Status:** **BLOCKED** (requirements gate) — safe soft-deprecation of `customer-trips` only.  
**Date:** 2026-08-22  
**Payment:** CASH ONLY. No Tahseel / online / card / wallet.  
**Shuttle:** Untouched.

## Critical STOP (triggered)

| # | Condition | Result |
|---|-----------|--------|
| 1 | Ride pricing cannot be derived safely | **STOP** — `PricingRule` is Route+VehicleType **catalog**, not OD/distance fare |
| 2 | Group business rules undefined | **STOP** — no size/pricing/members/cancellation product spec |
| 3 | Booking cannot represent Ride safely as product | **STOP** — Booking is Trip seat booking; no Ride/Group discriminator |
| 4 | Group ≠ RouteDemandGroupState | Confirmed — demand aggregation only |
| 5 | Auto-dispatch not supported | Confirmed — Admin assign only |
| 6 | Production migration | **NOT CREATED** / **NOT APPLIED** |

Inventing fares, group caps, or Ride APIs would violate Phase 6A rules.

---

## 1. Ride

**Status: BLOCKED**

| Area | Finding |
|------|---------|
| Domain | No Ride entity / product kind |
| API | Do **not** use `/customer-trips`. Soft-deprecated → `CUSTOMER_TRIPS_DEPRECATED` (410) |
| Pricing | Needs Admin-configurable Ride fare model (e.g. distance or matrix) — **missing** |
| CASH | Can reuse `CashPaymentPolicy` **after** a real Ride money path exists |
| Booking | Existing `POST /bookings` is Shuttle seat booking against launched Trip |
| Lifecycle | Could mirror Captain start/complete **after** Ride resource exists |
| Authorization | JWT + ownership patterns reusable later |
| Tests | Deprecation + stop-condition contract tests added |

### Product decisions required before implementation

1. On-demand private car vs scheduled request → Trip  
2. Fare model (distance / zone / fixed catalog) + Admin configuration tables  
3. Capacity / vehicle type for Ride  
4. Persist as request awaiting **Admin** assign (no auto-dispatch) vs other  
5. Whether Booking reuses Shuttle tables or new RideBooking entity  
6. FCM event names (report before inventing)

Conceptual future APIs (not implemented): quote → create request → me/list → start/complete — names TBD per project conventions.

---

## 2. Group

**Status: BLOCKED**

| Area | Finding |
|------|---------|
| Domain | **None** for group booking |
| API | **None** |
| Pricing | Undefined |
| CASH | Undefined |
| Members / seats / lifecycle | Undefined |
| Authorization | N/A |

`RouteDemandGroupState` must remain Admin demand corridor state.

### Product decisions required

1. Max passengers / vehicle  
2. Organizer + member model  
3. Pricing + commission  
4. CASH confirmation flow  
5. Seat consumption vs private charter  
6. Captain assignment  
7. Relation to Trip/Booking  

---

## 3. customer-trips

| Action | Detail |
|--------|--------|
| Soft-deprecated | Handler throws `CUSTOMER_TRIPS_DEPRECATED` (HTTP 410) |
| Remains registered | Controller route kept for discoverability |
| Migrated | No |
| Flutter | Endpoint constant only; no live caller |

---

## 4. Database

- Migrations created: **No**  
- Migrations applied: **No**  
- Production touched: **No**  

---

## 5. Shuttle regression

Must remain green (dotnet test). No launch/booking/CASH/seat/Captain changes intended beyond customer-trips deprecation.

---

## Rider readiness

| Product | Ready for UI «متاح»? |
|---------|----------------------|
| Ride | **BLOCKED** |
| Group | **BLOCKED** |

Keep Coming Soon until Phase 6B+ delivers approved contracts.

---

## Explicit non-goals

No Rider UI activation · No Tahseel · No online payment · No auto-dispatch · No auto-booking · No automatic Captain assignment · No new pricing constants · No fake data  
