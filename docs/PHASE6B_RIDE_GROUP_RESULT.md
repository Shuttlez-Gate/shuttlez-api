# PHASE 6B — Ride + Group Result

**Date:** 2026-08-22  
**Overall:** **BLOCKED** (product decisions) — contracts documented; **no** Ride/Group APIs, entities, migrations, or Rider «متاح» activation.  
**Payment:** CASH ONLY.  
**Phase 6A:** Remains STOPPED / complete. Soft-deprecation of `customer-trips` unchanged.

Documents:

- [`PHASE6B_RIDE_GROUP_CONTRACT.md`](./PHASE6B_RIDE_GROUP_CONTRACT.md)
- [`PHASE6B_PRODUCT_DECISIONS.md`](./PHASE6B_PRODUCT_DECISIONS.md)

---

## 1. Requirements decisions discovered

| ID | Topic | Status |
|----|-------|--------|
| Q1 | Ride fare model | **BLOCKED** — no OD/zone/base+km Admin catalog |
| Q2 | Ride shape & capacity | **BLOCKED** — entity vs Trip; capacity source |
| Q3 | Group fare model | **BLOCKED** |
| Q4 | Group who pays CASH | **BLOCKED** (organizer vs each member) |
| Q5 | Group membership / limits | **BLOCKED** |
| Q6 | Group vs Trip seats | **BLOCKED** |
| Q7 | Cancel + FCM names | Secondary; do not invent |

Recommended options are recorded in the decisions doc — **not implemented**.

---

## 2. Ride contract

| Area | Status |
|------|--------|
| Domain definition (intent) | **PARTIAL** (documented) |
| Implementation | **BLOCKED** |
| API | **BLOCKED** |
| Pricing | **BLOCKED** |
| Commission | **BLOCKED** (depends on fare) |
| CASH pattern | **PASS** (reuse `CashPaymentPolicy` when unblocked) |
| Lifecycle (intent) | **PARTIAL** (Requested→Assigned→InProgress→Completed) |
| Authorization (intent) | **PARTIAL** (JWT / Admin assign / Captain ownership) |
| Captain assignment | **PASS** as rule: Admin only, no auto-dispatch |
| Tests (Ride product) | **NOT APPLICABLE** (no implementation) |

---

## 3. Group contract

| Area | Status |
|------|--------|
| Domain definition (intent) | **PARTIAL** (documented; ≠ RouteDemandGroupState) |
| Implementation | **BLOCKED** |
| API / members / seats / pricing / CASH payer | **BLOCKED** |
| Tests (Group product) | **NOT APPLICABLE** |

---

## 4. APIs

| API set | Status |
|---------|--------|
| New Ride endpoints | **NOT APPLICABLE** (not built) |
| New Group endpoints | **NOT APPLICABLE** (not built) |
| `POST /customer-trips` | Remains soft-deprecated **410** `CUSTOMER_TRIPS_DEPRECATED` |
| Shuttle booking / Admin launch / Captain Trip | Untouched |

---

## 5. Pricing

**BLOCKED** for Ride and Group. Shuttle `PricingRule` unchanged and must not be misused for free OD.

---

## 6. Commission

**BLOCKED** for Ride/Group money path. Existing `CommissionRule` / launch windows remain Shuttle-ready for reuse later.

---

## 7. CASH

| Item | Status |
|------|--------|
| Policy reusable | **PASS** |
| Ride/Group confirm path | **BLOCKED** |
| Tahseel / online / card / wallet | **NOT APPLICABLE** (explicitly not built) |

---

## 8. Authorization

Patterns documented for future implementation. No new endpoints to authorize.

---

## 9. Lifecycle

Minimum Ride/Group lifecycle documented. Not coded.

---

## 10. Concurrency

**NOT APPLICABLE** until Group capacity rules exist. Shuttle seat atomics untouched.

---

## 11. Notifications

No new FCM types added. Proposed names listed as decisions only.

---

## 12. Database

| Item | Result |
|------|--------|
| Migrations created | **No** |
| Migrations applied | **No** |
| Production touched | **No** |

---

## 13. Tests

| Suite | Result |
|-------|--------|
| Backend unit (`Shuttlez.UnitTests`) | **PASS** — 189 / 189 |
| New Ride/Group Phase 6B product tests | **NOT APPLICABLE** |
| Rider `flutter test` | **PASS** — 48 / 48 |
| Classification | unit/contract docs only; no fake E2E |

---

## 14. Builds

| Target | Result |
|--------|--------|
| `dotnet build` | **PASS** (0 errors) |
| `dotnet test` | **PASS** (189) |
| Rider `flutter analyze` | **PASS** with pre-existing infos/warnings (40 issues, no Phase 6B regressions) |
| Rider `flutter test` | **PASS** (48) |
| Rider `flutter build apk --debug` | **PASS** → `build/app/outputs/flutter-apk/app-debug.apk` |
| Admin `ng build` | **PASS** → `dist/shuttlez-admin` |
| Captain `flutter analyze` | **PASS** with 3 pre-existing infos/warnings (exit code noise on PowerShell) |

---

## 15. Rider activation status

| Product | Badge | Backend | Decision |
|---------|-------|---------|----------|
| Shuttle | متاح | Live | Keep |
| Ride | قريبًا | No safe contract | **KEEP Coming Soon** |
| Group | قريبًا | No safe contract | **KEEP Coming Soon** |

**Do not flip badges.**

---

## 16. Shuttle regression

**PASS** — no Shuttle architecture changes in Phase 6B. Backend suite green (launch, booking, CASH, commission snapshots, seats, Captain, FCM soft-fail paths covered by existing tests).

---

## 17. Remaining blockers

1. Product answers to **Q1–Q6** (especially Ride fare + Group payer/limits).  
2. Admin-configurable fare data (dev/staging) once model chosen.  
3. Explicit Phase **6C** implementation after decisions — then migrations reported before prod apply.  
4. FCM event approval (can follow money path).

---

## 18. Exact next operator / product decision

**Answer Q1 first in writing:**

> How does the server compute Ride fare for pickup → destination without hardcoded amounts?

Until that is answered (and Group Q3–Q6 if Group is in scope for the same release), **Phase 6C must not start coding money paths.**

Recommended MVP path (not binding until product signs off):

1. Ride: Admin zone/flat catalog (**Q1-A**) + `RideRequest` awaiting Admin assign (**Q2-A**) + CASH confirm.  
2. Group: defer (**Q3-C**) or charter catalog by vehicle type (**Q3-A**) + organizer pays (**Q4-A**) + private charter (**Q6-A**).

---

## Explicit non-goals held

No Tahseel · No online payment · No auto-dispatch · No auto-booking · No auto Captain assignment · No Shuttle redesign · No historical financial edits · No production migration · No Rider UI activation · No revived `customer-trips`
