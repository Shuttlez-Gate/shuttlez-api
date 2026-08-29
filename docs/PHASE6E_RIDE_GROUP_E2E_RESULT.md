# PHASE 6E — RIDE + GROUP LIVE E2E RESULT

**Date:** 2026-08-22  
**Overall verdict:** **PASS** (Ride + Group LIVE on current hosted environment; FCM + Captain APK remain blocked)  
**Payment:** CASH ONLY  

| Product | Verdict |
|---------|---------|
| Ride | **LIVE** |
| Group | **LIVE** |
| Shuttle | **LIVE** (contracts unchanged; hosted Shuttle not re-exercised write-path this run) |

---

## 1. Environment

| Item | Value | Status |
|------|-------|--------|
| Backend branch | `main` @ `f79194a` (+ local Phase 6C/6E) | PASS |
| Hosted API | `https://shuttlez-001-site1.itempurl.com` | PASS |
| DB | Neon `neondb` (current environment) | PASS |
| Firebase | `ProjectId` / `CredentialsPath` = null | BLOCKED for FCM |
| E2E target | **Hosted itempurl** (same Neon) | PASS |

---

## 2. Migration

**Status: PASS**

- Migration: `20260822075604_Phase6C_RideGroupMvp`
- `dotnet ef migrations list`: present **without** `(Pending)`
- Applied earlier in Phase 6E activation; not re-created
- No replacement migration created

---

## 3. API deployment

**Status: PASS**

- Operator confirmed backend upload; Cursor verified routes after upload
- Phase 6C controllers present on host
- Unauthenticated probes:

| Route | Result |
|-------|--------|
| `GET /api/v1/content/faq` | **200** |
| `GET /api/v1/rides/fare-options` | **401** (exists; auth required) |
| `GET /api/v1/groups/fare-options` | **401** |
| `GET /api/v1/admin/ride-fare-rules` | **401** |
| `GET /api/v1/admin/group-fare-rules` | **401** |

---

## 4. Health check

**Status: PARTIAL**

- `GET /api/v1/health` → **500** (pre-existing AmbiguousMatch / dual health registration)
- Does **not** block Ride/Group business routes (verified separately)

---

## 5. Ride API verification

**Status: PASS** (authenticated on hosted)

| Action | Result |
|--------|--------|
| Quote | PASS (server fare) |
| Create CASH | PASS |
| Card rejected | PASS |
| Admin assign | PASS |
| Captain start/complete + idempotency | PASS |
| Wrong captain blocked | PASS |
| Rider cannot call Admin assign | PASS |

Hosted Ride id (this run): `49ec5c1b-0128-47a3-b845-14b6fb817364` → **Completed**, total **99.00**, `paymentMethod=cash`

---

## 6. Group API verification

**Status: PASS** (authenticated on hosted)

| Action | Result |
|--------|--------|
| Create Draft | PASS |
| Join member | PASS |
| Capacity exceed blocked | PASS |
| Confirm CASH | PASS |
| Join after lock blocked | PASS |
| Admin assign | PASS |
| Captain start/complete | PASS |

Hosted Group id: `21bf5c32-4dcd-44fa-9932-b7a9a4890369` → **Completed**, total **360.00**, CASH, membership locked

---

## 7. Ride E2E

**Status: PASS**

Existing accounts only:

| Role | Phone |
|------|-------|
| Admin | `+201000000000` |
| Rider | `+201144340030` |
| Captain | `+201008879097` → Driver entity `419f36e6-61e7-42d2-807f-879557387891` |
| Wrong captain | `+201066969694` |

Flow observed: Requested → Admin assign → InProgress → Completed. No auto-dispatch. No client fare/driver/status control.

Run log: `docs/PHASE6E_HOSTED_E2E_RUN_LOG.txt`

---

## 8. Group E2E

**Status: PASS**

- Organizer = authenticated Rider
- Second member `+201012345678`
- Third join blocked at capacity=2
- Confirm CASH → Confirmed + locked → Admin assign → start → complete
- No invented members beyond existing accounts

---

## 9. CASH verification

**Status: PASS**

- Ride/Group `paymentMethod=cash`, `isCashConfirmed=true`
- `paymentMethod=card` rejected on Ride create
- Rider UI string: **الدفع نقدًا للكابتن** (`cashToCaptain`)
- No Tahseel / wallet / gateway added

---

## 10. Pricing

**Status: PASS**

- Reused **existing** Admin active fare rules (no invented amounts)
- Active Ride flat rules present (server quote resolved **99.00** from active flat catalog)
- Active Group charter **360.00**, maxMembers=4
- Client does not authoritatively set fare

---

## 11. Commission

**Status: PASS**

- Observed Ride/Group `commissionAmount=0.00`, captain earnings = total (Phase 6C snapshot path)
- Client cannot override commission

---

## 12. Membership/capacity

**Status: PASS**

- Atomic capacity reject exercised
- Membership lock after confirm exercised

---

## 13. Captain lifecycle

**Status: PASS** (API)

- Assigned Ride/Group only
- Start/complete + idempotency
- Wrong Captain start blocked

---

## 14. Authorization

**Status: PASS**

- JWT required (401 without token)
- Admin fare/assign Admin-only (Rider blocked)
- Rider ownership reads
- Captain assignment ownership

---

## 15. FCM

**Status: BLOCKED**

- `Firebase:ProjectId` / `CredentialsPath` = null
- Live delivery **not** verified
- Business ops completed without FCM (soft-fail path)

---

## 16. Rider

**Status: PASS**

- Hub badges **متاح** for Ride/Group/Shuttle
- Real booking pages (tests: not Coming Soon)
- CASH copy present
- `flutter test` **48/48**
- `flutter build apk --debug` **PASS**

---

## 17. Admin

**Status: PASS**

- Fare rules + assign exercised via Admin API on hosted
- `ng build` **PASS** → `dist/shuttlez-admin`

---

## 18. Captain

| Item | Status |
|------|--------|
| Hosted lifecycle APIs | **PASS** |
| `flutter analyze` | Pre-existing infos/warnings only |
| APK build | **BLOCKED** — missing `android/key.properties` (signing environment) |

---

## 19. Tests

| Suite | Status |
|-------|--------|
| Backend unit | **PASS** 206/206 |
| Rider flutter test | **PASS** 48/48 |
| Hosted Ride/Group E2E | **PASS** |

---

## 20. Builds

| Target | Status |
|--------|--------|
| `dotnet build` / `dotnet test` | **PASS** |
| Rider debug APK | **PASS** |
| Admin `ng build` | **PASS** |
| Captain APK | **BLOCKED** (signing) |

---

## 21. Database safety

**Status: PASS**

- Phase6C migration applied once (not pending)
- No historical Shuttle financial snapshot edits
- No fake users/Captains/demand seeded
- No auto-assignment / auto-booking
- Writes this hosted E2E run: Ride Completed + Cancelled; Group Completed + 2 members; OTP side-effects for existing phones only

---

## 22. Known limitations

1. `/api/v1/health` still **500** (AmbiguousMatch) — separate cleanup
2. FCM credentials missing
3. Captain APK signing missing
4. `drivers/me.id` is **UserId**; Admin assign requires **Driver entity Id** (resolved via `GET /api/v1/admin/drivers`)

---

## 23. Remaining blockers

1. **FCM LIVE DELIVERY = BLOCKED** — configure Firebase credentials  
2. **CAPTAIN BUILD = BLOCKED** — provide `android/key.properties`  
3. Optional: fix dual `/api/v1/health` registration  

---

## 24. Exact operator actions

1. (Optional) Configure Firebase Admin credentials on host for FCM  
2. (Optional) Add Captain `android/key.properties` for APK  
3. (Optional) Fix AmbiguousMatch on `/api/v1/health`  
4. Deploy Admin to Firebase Hosting if UI parity desired:  
   `cd shuttlez-cursor-admin && npm run deploy`

---

## 25. Final verdict

**RIDE = LIVE**  
**GROUP = LIVE**  
**SHUTTLE = LIVE** (unchanged contracts; not redesigned)

Authenticated hosted E2E **PASS**.  
FCM **BLOCKED**. Captain APK **BLOCKED** (signing only).

---

## HARD RULES HELD

CASH ONLY · No Tahseel · No auto-dispatch · No invented users/fares · No historical financial edits · No Phase 7 started
