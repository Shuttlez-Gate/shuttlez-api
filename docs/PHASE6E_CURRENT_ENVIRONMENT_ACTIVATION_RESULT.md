# PHASE 6E — CURRENT ENVIRONMENT ACTIVATION RESULT

**Date:** 2026-08-22  
**Overall:** **PARTIAL**  
**Payment:** CASH ONLY  

| Product | Current-env activation |
|---------|------------------------|
| Ride (Neon + Phase 6C code) | **LIVE** (E2E executed) |
| Group (Neon + Phase 6C code) | **LIVE** (E2E executed) |
| Shuttle | **LIVE** on Neon contracts; **hosted itempurl currently 503** (deploy recovery pending) |
| Hosted itempurl Ride/Group routes | **BLOCKED** until IIS/app-pool recycle completes cleanly |

---

## 1. Environment

| Item | Value |
|------|-------|
| Git | `main` @ `f79194a` (+ local Phase 6C/6E changes) |
| Rider/Admin base URL | `https://shuttlez-001-site1.itempurl.com` |
| DB | Neon `neondb` (same current-env connection) |
| E2E execution host | Local process `http://127.0.0.1:5088` running Phase 6C+txn-fix against **same Neon** |
| Firebase | Still null → FCM not configured |

---

## 2. Migration

**Status: PASS**

- Applied: `20260822075604_Phase6C_RideGroupMvp`
- Verified tables present: `RideRequestsSet`, `RideFareRulesSet`, `GroupRequestsSet`, `GroupMembersSet`, `GroupFareRulesSet`
- No historical Shuttle/Booking financial rows modified

---

## 3. API deployment

**Status: PARTIAL / BLOCKED (hosted)**

| Step | Result |
|------|--------|
| Package built | PASS — FDD `publish/shuttlez-hosting-fdd` + SC attempt |
| FTP upload | PARTIAL — many DLLs uploaded; `Shuttlez.API.dll` briefly became **0 bytes** during locked overwrite; later restored to **148480** bytes |
| Hosted health after deploy | **503** |
| Root cause | Mixing self-contained hostfxr into site folder → `.NET location = site1` → “No frameworks were found”; IIS file locks (550) during overwrite |
| Mitigation attempted | Disabled `hostfxr.dll` / related natives; restored FDD binaries + `processPath=dotnet` web.config |
| Hosted recovery | **Still 503** at end of session — **SmarterASP app-pool Restart required** |

**Do not claim hosted Ride/Group LIVE.**

---

## 4. Ride endpoints

Against Neon via local Phase 6C API:

| Endpoint | Result |
|----------|--------|
| `GET /api/v1/rides/quote` | PASS |
| `POST /api/v1/rides` | PASS |
| `GET /api/v1/rides/{id}` | PASS |
| `PUT /api/v1/admin/rides/{id}/driver` | PASS (after txn fix) |
| `POST /api/v1/drivers/me/rides/{id}/start\|complete` | PASS |

Hosted itempurl: **503** (not verified LIVE).

---

## 5. Group endpoints

| Endpoint | Result |
|----------|--------|
| `POST /api/v1/groups` | PASS |
| `POST …/join` | PASS |
| Capacity exceed | PASS (blocked) |
| `POST …/confirm` CASH | PASS |
| Join after lock | PASS (blocked) |
| Admin assign / Captain start-complete | PASS |

---

## 6. Fare configuration

**Status: PASS** (Admin API on Neon)

| Rule | Id | Amount | Source |
|------|-----|--------|--------|
| Ride flat | `c5f7fd96-0901-490b-8409-2e60b5576814` | **90.00** | Reused existing active Shuttle `PricingRulesSet` CarShuttle Cairo `OneWayPrice` |
| Group charter | `5db362d7-a8a1-4a62-b46e-4d081a40087e` | **360.00**, MaxMembers=4 | Admin-configured as `4 ×` that same catalog OneWayPrice |

No distance/surge invented. Not hardcoded in handlers.

---

## 7. Ride E2E

**Status: PASS** (real authenticated flow on Neon)

Accounts used (existing only):

- Admin `+201000000000`
- Rider `+201144340030`
- Captain `+201008879097` (Approved)
- Wrong captain `+201066969694` (blocked)

Observed:

1. Quote fare=90 cash hint  
2. Create Requested, CASH confirmed, total=90, commission=0, captainEarnings=90, driverId empty  
3. Card payment rejected  
4. Admin assign → Assigned  
5. Captain start → InProgress (idempotent retry OK)  
6. Complete → Completed (idempotent OK)  
7. Rider sees Completed  
8. Wrong captain start blocked  
9. Snapshot immutable after Admin created another fare at 99 (completed ride remained 90)

Ride id example: `cc29d7da-8ca1-488a-a6dd-46f15c2798bd`

---

## 8. Group E2E

**Status: PASS** (real authenticated flow on Neon)

- Organizer create Draft capacity=2  
- Member `+201012345678` joined → joined=2  
- Third user join blocked (capacity)  
- Confirm CASH → Confirmed, locked, total=360  
- Join after lock blocked  
- Admin assign → Captain start → complete → Completed  

Group id: `c64ef777-5d04-4f2b-9d2d-22299ac9d717`

---

## 9. CASH verification

**Status: PASS**

- Ride/Group paymentMethod=`cash`  
- Non-CASH (`card`) rejected  
- No Tahseel/wallet/gateway introduced  

---

## 10. Rider verification

**Status: PASS** (code + tests)

- Hub badges متاح; routes to real Ride/Group pages  
- CASH copy present  
- `flutter test` 48/48  

Hosted API UX against itempurl: **BLOCKED** while host returns 503.

---

## 11. Captain verification

| Item | Status |
|------|--------|
| API lifecycle E2E | **PASS** |
| Ownership / wrong captain | **PASS** |
| Captain Flutter APK | **BLOCKED** — missing `android/key.properties` |
| `flutter analyze` | Pre-existing warnings only |

---

## 12. Admin verification

**Status: PASS** (OTP login + fare CRUD + assign on Neon via Phase 6C API)

---

## 13. FCM

**Status: BLOCKED** — Firebase credentials null. Business ops succeeded without FCM (soft-fail path). Live delivery **NOT** claimed.

---

## 14. Security

**Status: PASS** (exercised)

- JWT required  
- Admin fare/assign with Admin token  
- Rider ownership reads  
- Wrong Captain blocked  
- Client cannot set fare/driver/status (contract + live create)  

---

## 15. Concurrency

**Status: PASS** (exercised)

- Group capacity atomic reject  
- Membership lock after confirm  
- Start/complete idempotency  

**Bug fixed during E2E:** `ExecuteInSerializableTransactionAsync` now wraps `CreateExecutionStrategy()` so Npgsql retry strategy no longer 500s on assign/join.

---

## 16. Shuttle regression

| Item | Status |
|------|--------|
| Unit/contract suite | **PASS** 206/206 |
| Hosted Shuttle API | **BLOCKED** currently (itempurl 503) |
| Architecture changes to Shuttle pricing/launch | None intended |

---

## 17. Tests

| Suite | Result |
|-------|--------|
| Backend unit | **PASS** 206/206 |
| Rider flutter test | **PASS** 48/48 |
| Live Ride/Group E2E | **PASS** (Neon via local Phase 6C host) |
| Hosted itempurl E2E | **NOT RUN** (503) |

---

## 18. Builds

| Target | Result |
|--------|--------|
| `dotnet build` / `dotnet test` | **PASS** |
| Rider debug APK | **PASS** (session) |
| Admin `ng build` | **PASS** |
| Captain APK | **BLOCKED** (signing) |

---

## 19. Database writes performed

| Write | Detail |
|-------|--------|
| EF migration | `Phase6C_RideGroupMvp` applied |
| RideFareRule | 1 active flat 90.00 (+ later inactive/updated attempt 99 for immutability check) |
| GroupFareRule | 1 active charter 360.00 max=4 |
| RideRequests | Completed + Cancelled E2E rows |
| GroupRequest + Members | Completed E2E group with 2 members |
| OTP/refresh tokens | Normal auth side-effects for existing phones |

No fake users/Captains/demand invented.

---

## 20. Remaining blockers

1. **Hosted itempurl still HTTP 503** — operator must **Restart / Recycle app pool** in SmarterASP, then confirm `faq`/`rides/fare-options` (expect 200/401).  
2. If still failing: upload full FDD package from `publish/shuttlez-hosting-fdd` after stop, keep `web.config` `processPath=dotnet`, ensure **no** `hostfxr.dll` in site root.  
3. FCM credentials still missing.  
4. Captain APK signing file missing.  
5. Ambiguous `/api/v1/health` match existed pre-deploy on old host logs (separate cleanup).

---

## 21. Production readiness

| Claim | Status |
|-------|--------|
| Neon schema + fares + Ride/Group money path | **READY** |
| Authenticated Ride/Group E2E proven | **PASS** |
| Public itempurl serving Phase 6C | **NOT READY** (503) |
| FCM live | **NOT READY** |
| Overall go-live via Rider default base URL | **PARTIAL** until hosted recycle |

---

## 22. Exact next action

1. SmarterASP panel → **Restart site / recycle app pool** for `shuttlez-001` / site1.  
2. Verify:  
   - `GET /api/v1/content/faq` → 200  
   - `GET /api/v1/rides/fare-options` → **401** (auth) not 404/503  
3. Then re-run one hosted Ride quote with Rider JWT.  
4. Optional: configure Firebase for FCM.  

Until step 1–2 succeed: Rider app pointing at itempurl cannot complete Ride/Group calls even though Neon + Phase 6C E2E already **PASS**.

---

## HARD RULES HELD

CASH ONLY · No Tahseel · No auto-dispatch · No invented users · No historical financial edits · No new product features beyond Phase 6C (plus necessary txn strategy fix)
