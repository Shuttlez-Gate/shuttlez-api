# Phase 5 — Production Readiness Report (CASH ONLY)

**Updated:** 2026-08-21 (final live readiness gate)  
Verification / hardening only. No Tahseel. No online payment. No Phase 6.

## Classification legend

| Status | Meaning |
|--------|---------|
| PASS | Verified in code/tests / read-only live probe |
| PARTIAL | Contract/rule level; full live write E2E not completed |
| BLOCKED | Missing env/secrets or genuine READY corridor |
| NOT RUN | Requires operator action on real READY data |
| NOT APPLICABLE | Out of scope |

## Summary matrix

| # | Area | Status |
|---|------|--------|
| 1 | Demand / readiness (live read-only) | **BLOCKED** for E2E — READY=0 (see `PHASE5_DEMAND_READINESS_AUDIT.md`) |
| 2 | APIs (preview / bookings / launch / assign / captain lifecycle / devices) | **PASS** (implemented) |
| 3 | CASH payment contract | **PASS** |
| 4 | Pricing (server launch snapshot + immutability) | **PASS** |
| 5 | Commission (server booking snapshot) | **PASS** |
| 6 | Seat safety (atomic helpers) | **PASS** (code) / **PARTIAL** (live write not run) |
| 7 | Concurrency (Serializable + advisory lock + seat race tests) | **PASS** / **PARTIAL** |
| 8 | Authorization (JWT booking, AdminOnly, Captain ownership) | **PASS** (code/tests) |
| 9 | FCM soft-fail | **PASS** |
| 9b | FCM live delivery | **BLOCKED** (no Firebase credentials) |
| 10 | Rider Shuttle flow | **PASS** (Shuttle live; Ride/Group Coming Soon kept) |
| 11 | Captain app (analyze/tests) | **PASS**; APK **BLOCKED** (signing) |
| 12 | Admin launch/assign UI + APIs | **PASS** (code); live launch **NOT RUN** |
| 13 | Automated tests | **PASS** (Backend 184; Admin 23; Rider/Captain suites) |
| 14 | Builds | Backend/Admin/Rider debug APK **PASS**; Captain APK **BLOCKED** |
| 15 | Live E2E (launch→book→lifecycle) | **BLOCKED** — no genuine READY corridor |
| 16 | Production/data safety | **PASS** — no invent demand; no historical changes; no forced Trip/Booking |
| 17 | Migration | **NOT APPLICABLE** — none required; Phase4C already applied |

## 1. Demand / readiness (current Neon, read-only)

Probe via `tools/DemandVerify` → `docs/PHASE5_DEMAND_READINESS_AUDIT.md`.

| Metric | Count |
|--------|-------|
| Demand groups | 33 |
| **READY** | **0** |
| ALMOST_READY | 0 |
| NOT_READY | 1 |
| UNKNOWN (mostly `MISSING_ROUTE_LINK`) | 32 |
| Explicit MappedRouteId states | 0 |

### READY corridors

**None.**

### Closest real corridor

| Field | Value |
|-------|--------|
| Label | حلوان، القاهرة ↔ مدينة نصر، القاهرة |
| Confirmed | 2 |
| MinimumLaunchRiders | **8** |
| Status | **NOT_READY** |
| Reason | **BELOW_MINIMUM** |
| Mapping | EXACT_KEY (linked); Pricing OW 75; Cap 13 |
| Blocker type | **insufficient confirmed passengers** (not threshold change) |

### Other corridors

32 groups: **MISSING_ROUTE_LINK** → blocker = **missing route mapping** (not invented).

### Operator unblock for live E2E

1. Map corridors explicitly **or** rely on exact key where valid.  
2. Collect real confirmed demand until `Confirmed >= MinimumLaunchRiders` (e.g. Helwan↔Nasr City needs **6 more** confirmed vs min 8).  
3. Do **not** lower thresholds or seed fake passengers.  
4. When READY≥1, run `PHASE5_STAGING_E2E_CHECKLIST.md`.

## 2–8. Technical gates (no live writes)

- Launch: `RouteDemandLaunchEligibility` rejects non-READY; `RouteDemandLaunchService` uses Serializable + `pg_advisory_xact_lock`; price/capacity/commission server-side.  
- Booking: `CashPaymentPolicy`; request DTO = tripId/seatCount/paymentMethod only; `POST` `[Authorize]`.  
- Seats: `TryDecrementTripSeatsAsync` / `TryIncrementTripSeatsAsync`; bookable statuses only.  
- Trip price: `TripPriceSnapshot` immutable after launch.  
- FCM: soft-fail post-commit; credentials empty → `FCM_NOT_CONFIGURED`.

## Rider / Captain / Admin

- Rider: Map → SelectShuttle → Preview → CASH → ReservedTrip → Trips. Shuttle **متاح**; Ride/Group **قريبًا**. Empty = «لا توجد رحلات متاحة حاليًا».  
- Captain: lifecycle APIs wired; APK blocked by **release signingConfig requiring `android/key.properties` at Gradle configure time** (debug also fails).  
- Admin: inherits `[AdminOnly]`; CASH chip on bookings; launch/assign endpoints present.

## Live E2E

**BLOCKED** — READY=0. No Trip/Booking/Captain assignment created by this gate.

## Remaining blockers

1. Genuine **READY** corridor (real demand).  
2. Firebase credentials for live FCM.  
3. Captain `android/key.properties` + keystore (never commit).

## Exact operator actions

1. Grow confirmed passengers / map routes until Admin shows READY.  
2. Set Firebase secrets on API host; redeploy.  
3. Create local Captain signing files from `key.properties.example`.  
4. Execute full CASH E2E checklist once READY exists.

## Next phase recommendation

**STOP.** Do not start Phase 6 / Tahseel / online payment / Group / Ride / auto-dispatch.

---

CASH ONLY · Tahseel NOT IMPLEMENTED · Online payment NOT IMPLEMENTED · No fake demand · **Phase 5 complete — STOP**
