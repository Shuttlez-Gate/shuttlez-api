# Phase 5 — Staging / Operator E2E Checklist (CASH ONLY)

**Do not run this as automated production writes.**  
Execute only on **staging** (or a dedicated operator session) with real accounts.

Status of this document when checked in code only: **NOT RUN** (operator must execute).

**Last automated probe (2026-08-21 final gate):**  
**LIVE E2E = BLOCKED** — DemandVerify: **READY = 0** (33 groups; closest حلوان↔مدينة نصر = NOT_READY / BELOW_MINIMUM, confirmed 2 / min 8).  
Full audit: `PHASE5_DEMAND_READINESS_AUDIT.md`. Readiness report: `PHASE5_PRODUCTION_READINESS.md`.  
Do **not** invent demand or lower thresholds.

## Prerequisites

- [ ] Firebase credentials configured for staging:
  - `Firebase:ProjectId`
  - `Firebase:CredentialsPath` **or** `GOOGLE_APPLICATION_CREDENTIALS`
- [ ] At least one **READY** demand corridor exists (do **not** invent fake demand)
- [ ] Approved Captain account + Rider account + Admin account
- [ ] Rider + Captain apps installed with FCM device registration after login

If no READY route exists → **BLOCKED** (do not create fake production demand).

## Flow (20 steps)

1. Admin has an existing READY route  
2. Admin launches with `scheduledAt` → `POST /api/v1/admin/route-demand/launch?routeKey=`  
3. Backend creates exactly one Trip  
4. Admin optionally assigns approved Captain → `PUT /api/v1/admin/trips/{tripId}/driver`  
5. Trip becomes `DriverAssigned`  
6. Rider discovers the launched Trip  
7. Rider opens preview → `GET /api/v1/bookings/preview`  
8. Rider selects seats  
9. Rider confirms **CASH** booking → `POST /api/v1/bookings` `{ tripId, seatCount, paymentMethod: "cash" }`  
10. Backend creates Booking (server snapshots price/commission)  
11. `AvailableSeats` decreases atomically  
12. Rider sees confirmation; FCM `BOOKING_CONFIRMED` if configured («الدفع نقدًا للكابتن» — not “paid online”)  
13. Captain sees assigned Trip → `GET /api/v1/drivers/me/trips`  
14. Captain starts Trip → `POST .../start`  
15. Trip → `InProgress`  
16. Rider `TRIP_STARTED` if FCM configured  
17. Captain completes → `POST .../complete`  
18. Trip → `Completed`  
19. Rider `TRIP_COMPLETED` if FCM configured  
20. Trip cannot be booked after Completed  

## Verify in DB / Admin

| Check | Expected |
|--------|----------|
| Trip status | InProgress → Completed |
| Booking status | Confirmed |
| `paymentMethod` | `cash` |
| Available seats | Decreased by seat count |
| TotalAmount / commission / captain earnings | Server snapshot (immutable) |
| Notifications | Best-effort; booking still Confirmed if FCM fails |

## Explicit non-goals

- No Tahseel / card / wallet / online payment  
- No fake users, routes, trips, or bookings created by automation  
- No historical financial recalculation  
- No automatic Captain assignment / booking / trip creation  
