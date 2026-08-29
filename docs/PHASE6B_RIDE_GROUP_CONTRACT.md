# PHASE 6B — Ride + Group Product Contracts

**Status:** **BLOCKED** (product decisions required before implementation)  
**Date:** 2026-08-22  
**Payment:** CASH ONLY — no Tahseel / online / card / wallet / gateway  
**Shuttle:** Untouched (Map → SelectShuttle → Preview → CASH → ReservedTrip)  
**Phase 6A:** COMPLETE / STOPPED — still authoritative for foundation STOP reasons  

This document defines the **intended** minimum contracts and marks every field that cannot be finalized without an explicit product decision.  
**No Ride/Group entities, APIs, migrations, or Rider «متاح» activation are shipped in Phase 6B.**

Companion decision log: [`PHASE6B_PRODUCT_DECISIONS.md`](./PHASE6B_PRODUCT_DECISIONS.md)  
Result report: [`PHASE6B_RIDE_GROUP_RESULT.md`](./PHASE6B_RIDE_GROUP_RESULT.md)

---

## 0. Discovery summary (source of truth)

| Concept | Existing? | Reuse for Ride/Group? |
|---------|-----------|------------------------|
| `Trip` + Admin READY launch | Yes | **No** as auto-created Ride Trip |
| `Booking` (seat × Trip snapshot) | Yes | **No** as Ride/Group product without discriminator + new money path |
| `PricingRule` (Route + VehicleType catalog) | Yes | **No** for arbitrary OD Ride / Group charter |
| `CommissionRule` / launch commission windows | Yes | Reusable **after** product fare exists |
| `CashPaymentPolicy` | Yes | Reusable |
| JWT ownership / Admin assign / Captain start-complete | Yes | Pattern reusable |
| Lat/Lng `Latitude`/`Longitude` + address DTOs | Yes | Reuse conventions |
| `RouteDemandGroupState` | Yes | **Never** as Group booking |
| `POST /customer-trips` | Soft-deprecated 410 | **Never** revive |
| Ride / Group FCM types | No | Do not invent until contracts approved |
| Distance / matrix / zone fare engine | No | Do not invent |

---

## 1. Ride resource (عربية خاصة / مشوار فوري)

### 1.1 Product meaning

**Ride** = one authenticated Rider requests a **private car** from pickup → destination.  
It is **not** Shuttle seat booking on a launched corridor Trip.

Marketing copy (Rider app only — not a financial contract):

- «احجز عربية خاصة لمشوارك»

### 1.2 Intended resource (not implemented)

Conceptual `RideRequest` (names TBD to match API conventions):

| Field | Owner | Notes |
|-------|-------|-------|
| Id | Server | |
| RiderUserId | Server from JWT | Never from client body |
| PickupLatitude / PickupLongitude | Client | Existing coordinate convention |
| PickupAddress | Client optional | |
| DestinationLatitude / DestinationLongitude | Client | |
| DestinationAddress | Client optional | |
| Status | Server | See lifecycle |
| PaymentMethod | Server-normalized | `cash` only at go-live |
| FareAmount | Server | **BLOCKED — no fare model** |
| CommissionRate / CommissionAmount / CaptainEarnings | Server | After fare exists |
| DriverId | Admin assign only | Null until assigned |
| CreatedAt / AssignedAt / StartedAt / CompletedAt / CancelledAt | Server | |

### 1.3 Pricing source

| Option | Supported today? | Phase 6B action |
|--------|------------------|-----------------|
| A. Existing Admin Ride fare catalog | **No** | N/A |
| B. New Admin `RidePricingRule` (base / per-km / min) | Schema inventable, **values + distance source missing** | **STOP** |
| Misuse Shuttle `PricingRule.OneWayPrice` for OD | Unsafe / wrong product | **Forbidden** |
| Hardcoded 75 / 120 / … | Forbidden | **Forbidden** |

**Pricing status: BLOCKED.**  
Exact missing decision: see `PHASE6B_PRODUCT_DECISIONS.md` → Q1, Q2.

Without an Admin-configurable Ride fare model **and** an approved distance/zone source (or fixed catalog keyed by something other than free OD), the server cannot calculate an authoritative fare.

### 1.4 Commission source

After fare exists: reuse resolution order from `PricingRuleResolver` / active `CommissionRule` / config fallback — **same pattern as Shuttle booking snapshots**.  
Do **not** invent percentages (10%, 0%, …).

**Commission status: BLOCKED until Ride fare exists.**

### 1.5 CASH behavior

When unblocked:

```json
POST /api/v1/rides/{rideId}/confirm   // or confirm-on-create — TBD
{ "paymentMethod": "cash" }
```

- Client must **not** send price / total / commission / captainEarnings  
- Server validates via `CashPaymentPolicy` (reject card/tahseel/wallet/…)  
- UX copy: «الدفع نقدًا للكابتن» — no Pay Now / payment URL  
- Financial snapshot written once at confirmation (immutable thereafter, like Booking)

**CASH status: Pattern READY; product path BLOCKED.**

### 1.6 Lifecycle (minimum)

```
Requested → Assigned → InProgress → Completed
                ↘ Cancelled (rules TBD)
```

| Transition | Actor | Rule |
|------------|-------|------|
| Create → Requested | Rider | Auth JWT; no fare override |
| → Assigned | Admin | `PUT …/driver`; no auto-dispatch |
| → InProgress | Assigned Captain | Ownership + start action (mirror Trip) |
| → Completed | Assigned Captain | Ownership + complete; idempotent |
| → Cancelled | Rider and/or Admin | **Rules TBD — do not invent** |

Captain must **not** submit `driverId`, `status`, or prices.

### 1.7 Captain assignment

- **Admin only** (mirror `PUT/DELETE api/v1/admin/trips/{id}/driver`)  
- Eligibility: Driver `IsActive` + `VerificationStatus == Approved` + schedule conflict checks if shared clock  
- **No automatic assignment**

### 1.8 Cancellation

**BLOCKED** — no approved who/when/refund-for-CASH rules.  
Document only: cancellation must be explicit endpoints; seats N/A for private Ride (1 vehicle).

### 1.9 Authorization

| Actor | Create | Read | Confirm CASH | Assign | Start/Complete | Cancel |
|-------|--------|------|--------------|--------|----------------|--------|
| Rider | Own | Own | Own | No | No | Own if rules allow |
| Captain | No | Assigned only | No | No | Assigned only | No |
| Admin | Ops | All | Ops | Yes | Ops if needed | Ops |

### 1.10 APIs (illustrative — not implemented)

Follow existing `/api/v1/...` + MediatR + `ApiResponse<T>` conventions. Names TBD:

| Surface | Intent |
|---------|--------|
| `POST …/rides` or quote+confirm | Create request; server fare |
| `GET …/rides/me` | Rider list |
| `GET …/rides/{id}` | Rider own / Admin / assigned Captain |
| `POST …/rides/{id}/cancel` | If rules approved |
| `GET …/admin/rides` | Admin queue |
| `PUT/DELETE …/admin/rides/{id}/driver` | Assign / unassign |
| `GET …/drivers/me/rides` | Captain assigned |
| `POST …/drivers/me/rides/{id}/start\|complete` | Lifecycle |

**Do not** use `/customer-trips`.

### 1.11 Notifications

Proposed only (not approved / not implemented):

- `RIDE_CREATED`, `RIDE_ASSIGNED`, `RIDE_STARTED`, `RIDE_COMPLETED`, `RIDE_CANCELLED`

Existing FCM types remain Shuttle/Trip/Booking only (`CAPTAIN_TRIP_*`, `BOOKING_CONFIRMED`, `TRIP_*`).  
**Do not add types until product approves names + recipients.**  
FCM: post-commit, soft-fail, never roll back money path.

### 1.12 Database

If Ride proceeds later: new table(s) likely (`RideRequests` + optional `RidePricingRules`).  
**Phase 6B:** migration **not created**, **not applied**, production **not touched**.

---

## 2. Group resource (حجز مجموعة)

### 2.1 Product meaning

**Group** = multiple riders traveling together under **one group booking/request**, typically a **full vehicle for a group** (Rider marketing: «احجز عربية كاملة لمجموعتك»).

**Not** `RouteDemandGroupState` (Admin demand corridor aggregation).  
**Not** notification UI “groups”.

### 2.2 Intended domain (not implemented)

Conceptual split only:

| Entity | Purpose |
|--------|---------|
| `GroupRequest` | Organizer, OD, seat/vehicle intent, status, financial snapshot, optional DriverId |
| `GroupMember` | Membership under a group (auth user + seat share TBD) |

**All sizing, pricing, pay-who, join rules: BLOCKED.**

### 2.3 Pricing source

No Group fare catalog, discount, or charter rule exists.  
Must not reuse RouteDemand or Shuttle `OneWayPrice` as “group price” without product approval.

**Pricing status: BLOCKED** → Q3 in decisions doc.

### 2.4 CASH / who pays

Unknown whether:

- **A.** Organizer pays total cash once, or  
- **B.** Each member pays individually  

**BLOCKED** → Q4.

### 2.5 Members / seats

Unknown:

- Max / min members or seats  
- Join by invite vs open  
- Auth required to join  
- Who removes members  
- Whether seats lock after confirm  
- Whether Group consumes Shuttle Trip seats or is private charter  

**BLOCKED** → Q5, Q6.

### 2.6 Lifecycle / Captain

Same pattern as Ride once domain exists:

```
Requested → Assigned → InProgress → Completed (+ Cancelled TBD)
```

Admin assign only; Captain start/complete with ownership; **no second lifecycle engine** if a dedicated Group trip resource can mirror Trip rules.

### 2.7 Authorization / concurrency

- Organizer + members scoped by JWT  
- Atomic membership/capacity if shared seats (reuse booking seat transaction patterns — **after** capacity rules exist)  
- Duplicate join / double-cancel protection required when implemented  

### 2.8 APIs / notifications / DB

Same rule as Ride: define after decisions; do not invent endpoints or FCM types now.  
Migration: **none** in Phase 6B.

---

## 3. Shared non-goals (hard)

- Tahseel / online / card / wallet / SDK / webhooks  
- Automatic dispatch / booking / Captain assignment  
- Reviving `customer-trips`  
- Changing Shuttle launch, booking, pricing, commission, seats, Captain, FCM  
- Hardcoded fares / capacities / commission %  
- Historical financial edits  
- Rider UI «متاح» until contracts are real  

---

## 4. Implementation gate

| Product | Contract doc | Code | Rider UI |
|---------|--------------|------|----------|
| Ride | Defined + **pricing BLOCKED** | **Not started** | Keep «قريبًا» |
| Group | Defined + **rules BLOCKED** | **Not started** | Keep «قريبًا» |
| Shuttle | Unchanged | Unchanged | «متاح» |

**Phase 6B coding STOP:** do not implement Ride/Group money paths until decisions Q1–Q6 are answered in writing by product/operator.
