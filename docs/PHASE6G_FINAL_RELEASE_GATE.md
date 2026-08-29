# PHASE 6G — FINAL RELEASE GATE

**Date:** 2026-08-22  
**Host:** `https://shuttlez-001-site1.itempurl.com`  
**Payment:** CASH ONLY  
**Phase 7:** NOT STARTED  

---

## Verdict summary

| Gate | Status |
|------|--------|
| **CORE RELEASE GATE** | **PARTIAL** |
| Ride | **LIVE / PASS** |
| Group | **LIVE / PASS** |
| Shuttle | **LIVE / PASS** |
| CASH ONLY | **PASS** |
| Hosted Health HTTP 200 | **BLOCKED** (still **500**) |
| FCM live delivery | **BLOCKED** |
| Captain Release APK | **BLOCKED** |

**CORE RELEASE GATE = PASS** requires hosted `GET /api/v1/health` → **200**.  
Observed: **500**. Therefore CORE GATE remains **PARTIAL** even though Ride/Group/Shuttle product surfaces and local suites are green.

---

## 1. Hosted health

**Status: BLOCKED**

| Check | Result |
|-------|--------|
| `GET /api/v1/health` | **HTTP 500** (empty body) |
| Code-side duplicate MapGet | Already removed in Phase 6F (`HealthController` only) |
| Expected operator action | **App Pool recycle / Restart** on SmarterASP site1 |

Health is **PASS only if hosted returns 200**. It does not.

---

## 2. Ride = LIVE

**Status: PASS**

| Check | Result |
|-------|--------|
| Hosted `GET /api/v1/rides/fare-options` | **401** (route exists; auth required — not 404) |
| Rider routes to `RideBookingPage` | Confirmed (not Coming Soon) |
| Hub badge | **متاح** |
| Phase 6E authenticated E2E | Prior PASS (not re-run write E2E in 6G) |

---

## 3. Group = LIVE

**Status: PASS**

| Check | Result |
|-------|--------|
| Hosted `GET /api/v1/groups/fare-options` | **401** (exists; not 404) |
| Rider routes to `GroupBookingPage` | Confirmed |
| Hub badge | **متاح** |
| Phase 6E authenticated E2E | Prior PASS |

---

## 4. Shuttle = LIVE

**Status: PASS**

| Check | Result |
|-------|--------|
| Hosted `GET /api/v1/content/faq` | **200** |
| Hosted `GET /api/v1/bookings` | **405** (endpoint present; method not allowed for GET) |
| Rider base URL | `https://shuttlez-001-site1.itempurl.com` |
| Map → Select Shuttle → Preview → CASH path | Unchanged (no redesign in 6G) |

No new Shuttle write bookings created.

---

## 5. CASH ONLY

**Status: PASS**

- Ride/Group use `paymentMethod: 'cash'` and `cashToCaptain` (**الدفع نقدًا للكابتن**)
- Shuttle continue bar uses cash icon + cash-to-captain copy
- No Tahseel / card / wallet / gateway / online payment introduced in this gate

---

## 6. Coming Soon routing

**Status: PASS**

- `app_router.dart`: `/ride` → `RideBookingPage`; `/group` → `GroupBookingPage`
- `ComingSoonPage` is **not** used for Ride/Group navigation
- Legacy Coming Soon copy may remain unused for other cases — not blocking LIVE products

---

## 7. Final regression (already-passing suites)

**Status: PASS**

| Suite | Result |
|-------|--------|
| Backend `dotnet test` | **208/208 PASS** |
| Rider `flutter test` | **49/49 PASS** |
| Admin `ng build` | **PASS** |
| Rider `flutter build apk --debug` | **PASS** |

No test suites rewritten. No new migration. No Phase6E records modified.

---

## 8. FCM

**Status: BLOCKED**

| Item | Value |
|------|-------|
| `Firebase:ProjectId` | null |
| `Firebase:CredentialsPath` | null |
| Soft-fail when credentials missing | Present |
| Real device delivery verified | **NO** |

**FCM = BLOCKED.** Do not claim delivery PASS.

---

## 9. Captain signing

**Status: BLOCKED**

| Item | Result |
|------|--------|
| `android/key.properties` | **Missing** (`False`) |
| Example file | Present |
| Captain Release APK | **BLOCKED** |

**REASON = SIGNING ENVIRONMENT**  
No keys generated or invented.

---

## 10. Database / production safety (this gate)

**Status: PASS**

- No new production bookings
- No modification of Phase 6E Ride/Group records
- No fake users / fares / demand / Captains
- No new migration

---

## Required operator actions

1. **Restart / Recycle** SmarterASP app pool for `shuttlez-001` / site1  
2. Re-check: `GET https://shuttlez-001-site1.itempurl.com/api/v1/health` → expect **200**  
3. When health is 200, CORE RELEASE GATE can be re-marked **PASS** without reopening product work  
4. Optional: configure Firebase credentials for FCM  
5. Optional: provide Captain `android/key.properties` for release APK  

---

## Final statement

**CORE RELEASE GATE = PARTIAL** — blocked solely by hosted health still returning **500** (awaiting app-pool recycle).

Independently:

- **FCM = BLOCKED**
- **Captain Release APK = BLOCKED**

Products:

- **RIDE = LIVE**
- **GROUP = LIVE**
- **SHUTTLE = LIVE**
- **CASH ONLY = PASS**

**STOP.** Do not start Phase 7.
