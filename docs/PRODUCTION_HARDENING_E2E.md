# Production Hardening + E2E Validation

## Migration

Migration id: `20260821074715_Phase4C_UserDevices`  
Creates `UserDevicesSet` with FK to `UsersSet`, indexes on Token, UserId, unique (UserId, Token).

**Production auto-migrate:** NOT enabled.  
`Program.cs` runs `Database.MigrateAsync()` **only when `IsDevelopment()`**.

Operator must apply explicitly on production:

```bash
dotnet ef database update --project src/Shuttlez.Infrastructure --startup-project src/Shuttlez.API
```

(or your approved hosting deploy pipeline)

## Firebase configuration

Section: `Firebase`  
Keys: `ProjectId`, `CredentialsPath`  
Fallback: env `GOOGLE_APPLICATION_CREDENTIALS`

Current repo `appsettings.json`: **no Firebase section** (credentials must be supplied by environment/secrets — never commit JSON keys).

When credentials missing: FCM soft-fails (`FCM_NOT_CONFIGURED`), business APIs still succeed.

## Structured logs (no tokens/secrets)

DEVICE_REGISTERED, DEVICE_UNREGISTERED, CAPTAIN_ASSIGNED, CAPTAIN_UNASSIGNED,  
BOOKING_CONFIRMED_NOTIFICATION, TRIP_STARTED_NOTIFICATION, TRIP_COMPLETED_NOTIFICATION,  
TRIP_CANCELLED_NOTIFICATION, FCM_SEND_FAILED, FCM_TOKEN_INVALID, FCM_NOT_CONFIGURED

## Payment

CURRENT PAYMENT = CASH ONLY  
TAHSEEL = NOT IMPLEMENTED  
ONLINE PAYMENT = NOT IMPLEMENTED
