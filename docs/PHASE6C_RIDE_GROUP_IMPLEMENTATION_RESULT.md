# PHASE 6C — Ride + Group MVP Implementation Result

**Date:** 2026-08-22  
**Overall:** **PASS** (MVP contracts implemented) with **PARTIAL** production readiness  
**Payment:** CASH ONLY — no Tahseel / online / card / wallet / gateway  
**Approved decisions:** Q1–Q6 applied (zone/flat catalogs, private Ride, charter Group, organizer pays, membership lock, no auto-dispatch)

---

## 1. Ride domain

**Status: PASS**

| Item | Detail |
|------|--------|
| Entity | `RideRequest` (`RideRequestsSet`) |
| Lifecycle | `Requested → Assigned → InProgress → Completed` (+ `Cancelled`) |
| Ownership | `RiderUserId` from JWT; financials server-owned |
| Not Shuttle | Does not create Trip / RouteDemand launch |

---

## 2. Group domain

**Status: PASS**

| Item | Detail |
|------|--------|
| Entities | `GroupRequest` + `GroupMember` |
| Lifecycle | `Draft → Confirmed → Assigned → InProgress → Completed` (+ `Cancelled`) |
| Not | `RouteDemandGroupState` (unchanged demand corridor) |
| Organizer | Creates, confirms CASH total, membership locked after confirm |

---

## 3. Ride pricing

**Status: PASS** (config-driven; **no seeded production fares**)

- `RideFareRule` — Admin zone/flat catalog (`FromZoneKey`/`ToZoneKey` optional; both null = city-wide flat)
- Resolver: exact OD → reverse OD → flat default → `RIDE_FARE_NOT_CONFIGURED`
- No distance math; Shuttle `PricingRule` untouched

---

## 4. Group pricing

**Status: PASS** (config-driven; **no seeded production fares**)

- `GroupFareRule` — charter flat + `MaxMembers`
- Same zone matching as Ride
- No automatic discount

---

## 5. CASH

**Status: PASS**

- `CashPaymentPolicy` reused; Ride/Group reject non-cash (incl. subscription path for these products)
- Ride: CASH confirmed on create  
- Group: CASH on organizer `POST …/confirm`  
- Copy: «الدفع نقدًا للكابتن»  
- No payment URL / Pay Now / Tahseel

---

## 6. APIs

**Status: PASS**

| Surface | Routes |
|---------|--------|
| Rider Ride | `GET/POST /api/v1/rides`, `quote`, `fare-options`, `me`, `{id}`, `{id}/cancel` |
| Rider Group | `GET/POST /api/v1/groups`, `quote`, `fare-options`, `me`, `{id}`, `join`, `leave`, `confirm`, `cancel` |
| Admin fares | `/api/v1/admin/ride-fare-rules`, `/api/v1/admin/group-fare-rules` |
| Admin ops | `/api/v1/admin/rides`, `/api/v1/admin/groups` + driver assign/unassign |
| Captain | `/api/v1/drivers/me/rides`, `/api/v1/drivers/me/groups` + start/complete |
| Deprecated | `POST /customer-trips` remains **410** |

---

## 7. Authorization

**Status: PASS**

- Rider: own Ride / own Group participation  
- Organizer: confirm/cancel group pre-start rules  
- Captain: assigned only  
- Admin: fares + assignment  
- Client cannot set userId / driverId / status / fare / commission / totals  

---

## 8. Captain lifecycle

**Status: PASS**

- Admin assign only (no auto-dispatch)  
- Conflict checks across active Ride/Group  
- Start/complete ownership + idempotency (mirrors Trip patterns)  

---

## 9. Group membership

**Status: PASS**

- Organizer auto-joins on create  
- Auth join before confirm; unique `(GroupRequestId, UserId)`  
- Capacity ≤ fare rule `MaxMembers`; `JoinedMemberCount` + serializable txn + advisory lock  
- `MembershipLocked` after CASH confirm  

---

## 10. Concurrency

**Status: PASS** (design + transactional join/leave)

- DB unique membership index  
- Serializable transaction + advisory lock on join/leave  
- No in-memory counters  

**Integration Testcontainers concurrency suite:** **NOT RUN** (unit/contract coverage only in this phase)

---

## 11. Notifications

**Status: PARTIAL / NOT APPLICABLE**

- No new Ride/Group FCM event types invented  
- Existing Trip/Booking FCM infrastructure unchanged  
- Live FCM delivery: **NOT claimed** (credentials not verified)

---

## 12. Database / migrations

| Item | Result |
|------|--------|
| Migration created | **Yes** — `20260822075604_Phase6C_RideGroupMvp` |
| Tables | `RideFareRulesSet`, `GroupFareRulesSet`, `RideRequestsSet`, `GroupRequestsSet`, `GroupMembersSet` |
| Indexes | Status, owner, driver; unique `(GroupRequestId, UserId)` |
| FKs | Rider/Organizer → Users; Driver optional; FareRule optional |
| Applied to production | **No** |
| Production seeded | **No** |
| Historical financials | **Untouched** |

**Operator action:** apply migration on **dev/staging** explicitly before go-live:

```bash
cd src/Shuttlez.API
dotnet ef database update --project ../Shuttlez.Infrastructure --startup-project .
```

Then Admin must create at least one active Ride flat fare and one Group charter fare (no invented values in code).

---

## 13. Tests

| Suite | Classification | Result |
|-------|----------------|--------|
| Backend unit | Unit + Contract | **PASS** — **206** / 206 (was 189; +Phase 6C contract tests) |
| Rider flutter test | Widget/unit | **PASS** — **48** / 48 |
| Staging E2E write | E2E | **NOT RUN** |
| Live FCM device | E2E | **NOT RUN** |

---

## 14. Builds

| Target | Result |
|--------|--------|
| `dotnet build` | **PASS** (0 errors; pre-existing AdminRouteDemand warnings) |
| `dotnet test` | **PASS** 206 |
| Rider `flutter test` | **PASS** 48 |
| Rider `flutter build apk --debug` | **PASS** (prior/session build) |
| Admin `ng build` | **PASS** → `dist/shuttlez-admin` |
| Captain `flutter analyze` | Pre-existing 3 infos/warnings only |

---

## 15. Rider UI

**Status: PASS** (MVP)

| Product | Badge | Flow |
|---------|-------|------|
| Shuttle | متاح | Unchanged Map → SelectShuttle → Preview → CASH → ReservedTrip |
| Ride | **متاح** | Map args → quote (server) → تأكيد — الدفع نقدًا للكابتن → status |
| Group | **متاح** | Create draft → members → confirm CASH → status |

Coming Soon **removed** for Ride/Group hub cards.  
Fare displayed only from backend quote; `RIDE_FARE_NOT_CONFIGURED` / `GROUP_FARE_NOT_CONFIGURED` shown if Admin catalog empty.

---

## 16. Shuttle regression

**Status: PASS**

- No changes to launch, Shuttle pricing, booking, seats, Captain Trip lifecycle, or FCM Trip types  
- Existing Shuttle unit tests remain green within the 206 suite  

---

## 17. Remaining blockers

1. **Migration not applied** to staging/production — required before live Ride/Group writes.  
2. **Admin must configure** at least one active Ride fare + one Group fare (no code seeds).  
3. **Staging E2E** Ride/Group CASH + Admin assign + Captain start/complete — **NOT RUN**.  
4. **FCM** Ride/Group events not added — ops may be silent to devices until a later phase.  
5. Captain app UI for Ride/Group lists — **API ready**; Captain Flutter screens may still be Shuttle-trip oriented (**PARTIAL** product UX).  

---

## 18. Production readiness

| Claim | Status |
|-------|--------|
| Code/contracts ready for staging | **PASS** after migration + Admin fare config |
| Production DB migrated | **BLOCKED** until explicit operator apply |
| Production fares seeded | **NOT APPLICABLE** (must be Admin-entered real values) |
| Live FCM verified | **NOT RUN** |
| Staging E2E verified | **NOT RUN** |
| Overall production go-live | **PARTIAL** — do not claim full production readiness |

---

## Explicit non-goals held

CASH ONLY · No Tahseel · No online payment · No auto-dispatch · No auto-booking · No auto Captain assignment · No fake pricing · No historical financial edits · Shuttle architecture untouched · `customer-trips` remains deprecated
