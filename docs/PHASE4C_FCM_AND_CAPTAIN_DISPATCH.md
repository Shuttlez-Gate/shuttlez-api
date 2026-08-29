# Phase 4C — FCM Notifications + Admin Captain Dispatch

## Principle

Captain lifecycle does not create Trips or Bookings.  
Admin manually assigns Captains. Assignment commits first; FCM is best-effort after commit.

CURRENT PAYMENT = CASH ONLY  
TAHSEEL = NOT IMPLEMENTED  
ONLINE PAYMENT = NOT IMPLEMENTED  
AUTOMATIC DISPATCH = NOT IMPLEMENTED

## Device tokens

- Table: `UserDevicesSet` (migration `Phase4C_UserDevices`)
- `POST /api/v1/notifications/devices` `{ token, platform }`
- `DELETE /api/v1/notifications/devices/{token}`
- JWT user ownership; upsert; deactivate invalid FCM tokens

## Push abstraction

- `IPushNotificationService` + `FirebasePushNotificationService`
- Soft-fail when Firebase credentials missing
- Never rolls back business transactions

## Events

| Type | When |
|------|------|
| CAPTAIN_TRIP_ASSIGNED | Admin assign |
| CAPTAIN_TRIP_UNASSIGNED | Admin unassign / replace |
| TRIP_CANCELLED | Admin trip delete/cancel |
| BOOKING_CONFIRMED | After CASH booking commit |
| TRIP_STARTED | After Captain start |
| TRIP_COMPLETED | After Captain complete |

## Captain dispatch

- `PUT /api/v1/admin/trips/{tripId}/driver` `{ driverId }`
- `DELETE /api/v1/admin/trips/{tripId}/driver`
- Validates active + Approved driver
- Same `ScheduledAt` conflict rejection
- Status → `DriverAssigned` / back to `Scheduled`
- Serializable txn + advisory lock

## Conflict limitation

No trip duration model — conflict = same `ScheduledAt` only.

## Flutter

Rider + Captain: `firebase_messaging`, register after auth, unregister on logout.
