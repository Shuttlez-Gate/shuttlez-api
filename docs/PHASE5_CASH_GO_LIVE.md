# Phase 5 — Production Go-Live + CASH Payment

## Scope

Prepare staging/production operation with **CASH only**.  
No Tahseel, card, wallet, or online payment gateway.

## Payment contract

Client `POST /api/v1/bookings` body:

```json
{ "tripId": "...", "seatCount": 1, "paymentMethod": "cash" }
```

Server:

- Rejects unsupported methods (`PAYMENT_METHOD_NOT_SUPPORTED`)
- Normalizes cash variants → `cash`
- Allows existing `subscription` package credit (not an online gateway)
- Snapshots price / commission / captain earnings server-side
- Response includes authoritative `totalAmount`, `paymentMethod`, `seatCount`, `referenceCode`

## FCM

- Credentials: `Firebase:ProjectId`, `Firebase:CredentialsPath`, or `GOOGLE_APPLICATION_CREDENTIALS`
- Soft-fail: business ops succeed if FCM unavailable (`FCM_NOT_CONFIGURED`)
- `BOOKING_CONFIRMED` cash body: «تم تأكيد الحجز — الدفع نقدًا للكابتن»

## No DB migration

Existing booking snapshot + `UserDevicesSet` schema is sufficient.

## Staging E2E

See `PHASE5_STAGING_E2E_CHECKLIST.md`.  
Do **not** auto-write production. If no READY corridor → BLOCKED.

## Operator prerequisites (required for live FCM / Captain APK)

1. Configure Firebase secrets on the API host (`Firebase:ProjectId` + credentials path or `GOOGLE_APPLICATION_CREDENTIALS`).
2. Provide Captain signing via local `android/key.properties` (never commit secrets) — see `key.properties.example`.
3. Redeploy API after Firebase config; confirm soft-fail still works if credentials missing.

## Explicit

CURRENT PAYMENT = CASH ONLY  
TAHSEEL = NOT IMPLEMENTED  
ONLINE PAYMENT = NOT IMPLEMENTED
