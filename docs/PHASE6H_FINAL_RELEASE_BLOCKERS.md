# PHASE 6H — FINAL RELEASE BLOCKERS

**Date:** 2026-08-22  
**Overall:** **PARTIAL**  
**Payment:** CASH ONLY  
**Phase 7:** NOT STARTED  
**Migration:** NO NEW MIGRATION  

| Product | Status |
|---------|--------|
| Ride | **LIVE** |
| Group | **LIVE** |
| Shuttle | **LIVE** |
| CASH ONLY | **PASS** |

---

## 1. Health implementation

**Status: PASS**

- `HealthController` `[Route("api/v1/health")]` remains the sole registration
- Duplicate `Program.cs` `MapGet("/api/v1/health")` remains removed (Phase 6F)
- Local `GET https://localhost:7222/api/v1/health` → **200** (verified earlier in session / prior check)
- No second health endpoint added

---

## 2. Hosted health result

**Status: PASS**

| Check | Result |
|-------|--------|
| Pre-deploy hosted | HTTP **500** |
| Deploy | Published FDD; FTP uploaded API DLLs (rename-swap where locked) + `web.config` |
| Recycle | Forced via `web.config` overwrite (IIS/ANCM restart) |
| Post-deploy `GET https://shuttlez-001-site1.itempurl.com/api/v1/health` | **HTTP 200** |
| Body | `status=healthy`, `service=Shuttlez.API` |

Smoke (read-only):

| Route | Result |
|-------|--------|
| `/api/v1/content/faq` | **200** |
| `/api/v1/rides/fare-options` | **401** |
| `/api/v1/groups/fare-options` | **401** |

---

## 3. FCM configuration status

**Status: PASS** (config readiness)

Operator-provided credential (not invented):

- Local: `secrets/firebase-adminsdk.json` (`type=service_account`, `project_id=shuttlez-88d29`)
- Matches Rider/Captain `google-services` project **`shuttlez-88d29`**
- Hosted (not committed): uploaded to `App_Data/firebase-adminsdk.json`
- Hosted `web.config` env (no secret values in repo):
  - `Firebase__ProjectId=shuttlez-88d29`
  - `Firebase__CredentialsPath=App_Data\firebase-adminsdk.json`
  - `GOOGLE_APPLICATION_CREDENTIALS=App_Data\firebase-adminsdk.json`
- Local Development: `ProjectId` + `CredentialsPath` → secrets file
- Repo `appsettings.json` Production defaults remain `null` (no secret committed)
- `secrets/` and `firebase-adminsdk*.json` gitignored

Verified existing support:

- Options bind: `Firebase:ProjectId` / `Firebase:CredentialsPath`
- Fallback: `GOOGLE_APPLICATION_CREDENTIALS`
- Soft-fail when missing/unloadable (`FirebaseApp` null → skip send)
- Invalid token deactivation path present (`Unregistered` / `InvalidArgument`)
- Event types unchanged:

`BOOKING_CONFIRMED`, `TRIP_STARTED`, `TRIP_COMPLETED`, `TRIP_CANCELLED`, `CAPTAIN_TRIP_ASSIGNED`, `CAPTAIN_TRIP_UNASSIGNED`

Admin SDK init check: **OK** (`project=shuttlez-88d29`)

---

## 4. FCM delivery verification status

**Status: BLOCKED / NOT RUN**

- No approved test device token provided
- No push sent to real users
- **Do not claim FCM delivery PASS**

---

## 5. Captain signing status

**Status: BLOCKED**

| File | Present |
|------|---------|
| `android/key.properties` | **No** |
| `android/upload-keystore.jks` | **No** |
| `key.properties.example` | Yes |
| `.gitignore` protects `key.properties` / `*.jks` | Yes |

**CAPTAIN RELEASE APK = BLOCKED** — SIGNING ENVIRONMENT  
No keystore/passwords invented.

---

## 6. Backend tests

**Status: PASS** — **208/208**

---

## 7. Admin build

**Status: PASS** — `ng build` → `dist/shuttlez-admin`

---

## 8. Rider tests/build

**Status: PASS**

- `flutter test` **49/49**
- `flutter build apk --debug` **PASS**

---

## 9. Captain analyze/build

| Item | Status |
|------|--------|
| `flutter analyze` | Pre-existing warnings only |
| `flutter build apk --debug` | **BLOCKED** (signing env required by Gradle even for debug in this project) |
| `flutter build apk --release` | **BLOCKED** (no `key.properties`) |

---

## 10. Ride regression

**Status: PASS** (hosted routes present; no write E2E re-run)

- Hosted fare-options → **401**
- No new Ride records created in 6H

---

## 11. Group regression

**Status: PASS** (hosted routes present; no write E2E re-run)

- Hosted fare-options → **401**
- No new Group records created in 6H

---

## 12. Shuttle regression

**Status: PASS**

- Hosted faq **200**
- No Shuttle architecture / booking changes in 6H
- No demand/trips/bookings created

---

## 13. CASH-only verification

**Status: PASS**

- `CashPaymentPolicy` remains CASH-only
- No Tahseel/card/wallet/online/gateway/callback added
- Phase 6H did not alter payment behavior

---

## 14. Production safety

**Status: PASS**

- No new migration
- No historical financial edits
- No fake users/fares/demand/devices
- No FCM to real users
- No Phase6E record mutation
- Firebase JSON uploaded to host `App_Data` only (not committed)

---

## 15. Remaining blockers

1. **FCM delivery** — needs approved test device/token for live send verification  
2. **Captain release signing** — needs `android/key.properties` + `upload-keystore.jks` from operator  

---

## 16. Exact operator actions

1. (Optional) Provide approved FCM test device token → verify one push, then mark delivery PASS  
2. Provide existing Captain keystore + passwords → create local `android/key.properties` (never commit) → `flutter build apk --release`  
3. Confirm hosted health stays **200** after any future deploys (recycle if DLL locks)

---

## 17. Final release-gate verdict

# PHASE 6H — FINAL RELEASE GATE PARTIAL

| Gate | Status |
|------|--------|
| Health (hosted HTTP 200) | **PASS** |
| FCM configuration | **PASS** |
| FCM delivery | **BLOCKED** |
| Captain Release APK | **BLOCKED** |

**Ride = LIVE**  
**Group = LIVE**  
**Shuttle = LIVE**  
**CASH ONLY = PASS**

**STOP.** Do not start Phase 7.
