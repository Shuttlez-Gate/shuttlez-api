# PHASE 6F — RELEASE HARDENING RESULT

**Date:** 2026-08-22  
**Overall:** **PARTIAL**  
**Payment:** CASH ONLY  
**Phase 7:** NOT STARTED  

| Product | Status |
|---------|--------|
| Ride | LIVE (UX hardened) |
| Group | LIVE (UX hardened) |
| Shuttle | LIVE (unchanged booking path; CASH bar clarified) |

---

## 1. Rider Ride UX

**Status: PASS**

- Entry via Transport Mode; badge **متاح**
- Routes to `RideBookingPage` — **not** `ComingSoonPage`
- Fare from API quote; CASH copy **الدفع نقدًا للكابتن**
- Submit → status page with polling
- Loading / empty (missing map points) / error boxes present

---

## 2. Rider Group UX

**Status: PASS**

- Badge **متاح**; real `GroupBookingPage`
- Capacity + membership lock label shown when `membershipLocked`
- Server total + CASH confirm
- Organizer draft → confirm flow intact
- Error mapping for capacity / lock / fare

---

## 3. Rider Shuttle regression

**Status: PASS** (code audit; no write re-E2E)

- Map → Select Shuttle → Preview → CASH → ReservedTrip unchanged
- Shuttle payment strip: wallet icon replaced with **cash** icon + **الدفع نقدًا للكابتن**

---

## 4. CASH UX

**Status: PASS**

- Ride / Group / Shuttle communicate cash-to-captain
- No Visa / Mastercard / Tahseel / Pay Now / gateway / redirect added
- Remaining `wallet.png` asset file still exists on disk but is **not** used on the Shuttle continue bar after this hardening

---

## 5. Rider API contracts

**Status: PASS**

- Create payloads: geo + addresses + zones + `paymentMethod=cash` only
- No client `totalAmount` / commission / captainEarnings / driverId / status on write
- Server response remains authoritative for fares

---

## 6. Error / loading / empty states

**Status: PASS**

- Shared mapper: `private_product_error_mapper.dart`
- Maps: fare not configured, capacity, membership lock, unauthorized, unavailable, lifecycle conflicts, captain unavailable
- Unknown errors → generic user message (no stack / SQL / JWT)

---

## 7. Admin

**Status: PASS**

- `ng build` PASS → `dist/shuttlez-admin`
- Existing Ride/Group fare + assignment screens present
- No auto-assignment introduced
- No Admin redesign

---

## 8. Captain

| Item | Status |
|------|--------|
| Assigned Ride/Group APIs wired in app | **PASS** (new minimal screen) |
| Start / Complete (assigned only; server enforces ownership) | **PASS** (API contract + UI calls) |
| Shuttle home unchanged | **PASS** |
| `flutter analyze` (changed paths) | **PASS** (no issues) |
| Full-project analyze | Pre-existing warnings only |
| APK | **BLOCKED** — `android/key.properties` missing |

Entry: Home → **مشاوير ومجموعات مسندة** → list/start/complete via `/api/v1/drivers/me/rides|groups`.

---

## 9. Backend health

**Status: PARTIAL**

| Layer | Result |
|-------|--------|
| Root cause | Duplicate registration: `app.MapGet("/api/v1/health")` **and** `HealthController` → AmbiguousMatchException → **500** |
| Code fix | Removed `MapGet` duplicate; keep `HealthController` only |
| Unit guard | `HealthEndpointRegistrationTests` (2 tests) |
| Hosted | FTP overwrite of `Shuttlez.API.dll` often **550 locked**; rename-swap uploaded; **`GET /health` still 500** until app-pool recycle loads new assembly |
| Ride/Group routes | Still **401** unauthenticated (alive) |

**Do not claim hosted health PASS.**

---

## 10. FCM

**Status: BLOCKED**

- `Firebase:ProjectId` / `CredentialsPath` = null
- Soft-fail path unchanged (business ops must not depend on push)
- Live delivery **not** claimed

---

## 11. Security

**Status: PASS** (contract / prior E2E; no new write E2E this phase)

- JWT required on Ride/Group/Admin/Captain private routes
- No client financial/status/driver overrides added
- Captain UI does not send price/commission/driverId/status

---

## 12. Tests

| Suite | Status |
|-------|--------|
| Backend unit | **PASS** 208/208 (+2 health registration tests) |
| Rider flutter test | **PASS** 49/49 (+1 error mapper test) |
| Hosted write E2E | **NOT RUN** (Phase 6F: read-only / no unnecessary writes) |

---

## 13. Builds

| Target | Status |
|--------|--------|
| `dotnet build` / `dotnet test` | **PASS** |
| Rider debug APK | **PASS** |
| Admin `ng build` | **PASS** |
| Captain APK | **BLOCKED** (signing environment) |

---

## 14. Live regression

**Status: PASS** (read-only)

- No Phase 6E Ride/Group rows deleted/modified
- No new production write E2E executed in 6F
- Hosted faq **200**, rides fare-options **401**

---

## 15. Files changed

**Backend**
- `src/Shuttlez.API/Program.cs` — remove duplicate health MapGet
- `tests/.../HealthEndpointRegistrationTests.cs` — new

**Rider**
- `ride_options_sheet.dart` — cash icon + cashToCaptain
- `app_l10n.dart` — error/lock strings
- `private_product_error_mapper.dart` — new
- `ride_booking_page.dart`, `group_booking_page.dart`, `group_entities.dart` — errors + membershipLocked
- `test/private_product_error_mapper_test.dart` — new

**Captain**
- `assigned_private/**` — new minimal assigned Ride/Group UX
- `api_endpoints.dart`, `service_locator.dart`, `app_routes.dart`, `app_router.dart`, `home_tab_page.dart`

---

## 16. Known limitations

1. Hosted `/api/v1/health` may remain 500 until operator **recycles app pool** after DLL swap  
2. ComingSoonPage + “قريبًا” strings retained for unused/legacy tests — **not** used by Ride/Group navigation  
3. Captain APK cannot be produced without signing keys  
4. FCM still unconfigured  

---

## 17. Remaining blockers

1. **FCM = BLOCKED** — provide Firebase credentials on host  
2. **CAPTAIN APK = BLOCKED** — REASON = SIGNING ENVIRONMENT (`android/key.properties`)  
3. **Hosted health = PARTIAL** — recycle SmarterASP app pool after Phase 6F API DLL  

---

## 18. Required operator actions

1. SmarterASP → **Restart / Recycle** site1 app pool  
2. Verify: `GET /api/v1/health` → **200** with healthy payload  
3. (Optional) Configure Firebase Admin credentials for FCM  
4. (Optional) Add Captain `android/key.properties`  
5. (Optional) Redeploy Admin to Firebase Hosting if UI changes desired: `npm run deploy`  

---

## 19. Final verdict

**RELEASE HARDENING = PARTIAL**

- Rider Ride/Group/Shuttle CASH UX + contracts + errors: **PASS**  
- Admin build + screens: **PASS**  
- Captain assigned Ride/Group UX wired: **PASS** (APK **BLOCKED**)  
- Health code fix: **PASS** locally; hosted verification **PARTIAL** pending recycle  
- FCM: **BLOCKED**  

**STOP.** Phase 7 not started. CASH ONLY. No Tahseel / online payment / auto-dispatch.
