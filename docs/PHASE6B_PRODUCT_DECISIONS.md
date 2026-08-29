# PHASE 6B — Product Decisions Required (STOP Gate)

**Date:** 2026-08-22  
**Rule:** Do not invent answers. Implementation of Ride/Group financial APIs is **blocked** until these are decided in writing.

Related: [`PHASE6B_RIDE_GROUP_CONTRACT.md`](./PHASE6B_RIDE_GROUP_CONTRACT.md)

---

## Q1 — Ride fare model

| | |
|--|--|
| **QUESTION** | How does the server compute an authoritative Ride fare for arbitrary pickup → destination? |
| **CURRENT EVIDENCE** | `PricingRule` is Route + `VehicleType` catalog (`OneWayPrice`, packages). No `BaseFare`, `PricePerKm`, zone matrix, or Ride catalog. No trusted distance API integrated for billing. Phase 3 pricing doc excludes On-Demand Ride. |
| **OPTIONS** | **A.** Fixed Admin Ride catalog (e.g. zone↔zone or city flat rates) with no distance math. **B.** Admin `RidePricingRule` (base + per-km + minimum) **plus** approved distance source (existing Maps provider with billing SLA, or precomputed matrix). **C.** Reuse Shuttle `PricingRule` only when OD maps to an existing Route — otherwise reject (not true private OD). |
| **RECOMMENDED OPTION** | **A** for fastest safe CASH go-live (Admin-owned tables, no distance invention); or **B** only after distance source is named and approved. Reject silent hardcoding. |
| **BLOCKING IMPACT** | Without Q1: cannot implement quote, create, confirm, financial snapshot, or Rider «متاح». |

---

## Q2 — Ride product shape & capacity

| | |
|--|--|
| **QUESTION** | Is Ride (1) on-demand private request awaiting Admin Captain assign, or (2) scheduled request that becomes a Trip under Admin launch rules? What vehicle capacity applies? |
| **CURRENT EVIDENCE** | No Ride entity. Trip capacity comes from vehicle at Shuttle launch. Auto Trip create via `customer-trips` is deprecated (hardcoded seats 14). Admin assign exists for Trips only. Rider copy implies private car. |
| **OPTIONS** | **A.** Persist `RideRequest` until Admin assigns Captain (no Trip). **B.** Admin converts approved request → Trip under existing launch discipline (still no auto). **C.** Hybrid. Capacity: from Captain’s vehicle at assign time vs fixed private-car seats from vehicle master. |
| **RECOMMENDED OPTION** | **A** + capacity from assigned driver’s vehicle (or vehicle master at assign) — aligns with “no auto Trip,” Admin assign, CASH snapshot at confirm. |
| **BLOCKING IMPACT** | Schema, APIs, and Captain list endpoints differ; inventing capacity numbers is forbidden. |

---

## Q3 — Group fare model

| | |
|--|--|
| **QUESTION** | How is Group priced (charter total vs per-seat; discounts; vehicle type)? |
| **CURRENT EVIDENCE** | No Group booking entity. `RouteDemandGroupState` is demand corridor state only. No group discount / charter fields. Marketing: full vehicle for a group. |
| **OPTIONS** | **A.** Admin Group charter catalog by vehicle type (flat total). **B.** Per-seat using a dedicated Group rule (not Shuttle corridor rule unless product says so). **C.** Defer Group product; keep «قريبًا». |
| **RECOMMENDED OPTION** | **A** if Group = full vehicle; else **C** until product writes numbers/rules into Admin config. |
| **BLOCKING IMPACT** | No Group financial API without Q3. |

---

## Q4 — Group CASH payer

| | |
|--|--|
| **QUESTION** | Who pays cash — organizer for the whole group, or each member? |
| **CURRENT EVIDENCE** | Shuttle: each Rider books own seats CASH. No Group payment semantics documented. |
| **OPTIONS** | **A.** Organizer confirms once; total CASH to Captain. **B.** Each member confirms own share CASH. **C.** Organizer books all seats under one user (members are informational only). |
| **RECOMMENDED OPTION** | **A** or **C** for MVP (one financial snapshot); **B** needs multi-booking + concurrency. |
| **BLOCKING IMPACT** | Confirm API shape, authorization, and duplicate-payment protection depend on Q4. |

---

## Q5 — Group membership & limits

| | |
|--|--|
| **QUESTION** | Who creates/joins/removes; auth required; min/max members or seats; lock after confirm? |
| **CURRENT EVIDENCE** | Rider copy only. No max/min in backend or docs. Vehicle seat masters exist for Shuttle (4/13/24 class) but are not approved as Group limits. |
| **OPTIONS** | Cap by vehicle type seats; fixed product max (e.g. N); invite-only vs code join; organizer-only cancel; etc. |
| **RECOMMENDED OPTION** | Cap by selected vehicle’s seat capacity from vehicle master; members must be authenticated; organizer creates; lock membership after CASH confirm. **Values must be Admin/vehicle-derived, never hardcoded in code.** |
| **BLOCKING IMPACT** | Without Q5: cannot implement members APIs or concurrency tests safely. |

---

## Q6 — Group vs Trip / seats

| | |
|--|--|
| **QUESTION** | Does Group consume seats on an Admin-launched Shuttle Trip, or is it a private charter request (like Ride)? |
| **CURRENT EVIDENCE** | Atomic seat helpers exist for Trip bookings. Group marketing sounds like charter. Mixing with Ready-launch Shuttle without product intent risks architecture conflict. |
| **OPTIONS** | **A.** Private charter `GroupRequest` (no Trip seats). **B.** Multi-rider booking against launched Trip (extends Shuttle, not a new product). **C.** Admin converts GroupRequest → Trip then books seats. |
| **RECOMMENDED OPTION** | **A** for “عربية كاملة لمجموعتك”; keep Shuttle seat flow separate. |
| **BLOCKING IMPACT** | Wrong choice either invents a conflicting domain or incorrectly reuses RouteDemand. |

---

## Q7 — Cancellation & FCM (secondary)

| | |
|--|--|
| **QUESTION** | Who may cancel Ride/Group at which statuses? Which FCM event names/recipients? |
| **CURRENT EVIDENCE** | Trip/Booking FCM types exist; no Ride/Group types. Cancellation policy for private products undefined. |
| **OPTIONS** | Mirror Trip cancel patterns vs Rider-only before Assigned vs Admin-only after Assigned; approve event name list or defer FCM. |
| **RECOMMENDED OPTION** | Defer FCM until money path ships; cancel = Rider before Assigned + Admin anytime (product confirm). |
| **BLOCKING IMPACT** | Soft — can ship money path without new FCM if product accepts silent ops; do not invent type strings. |

---

## Operator action checklist

1. Answer **Q1–Q6** in writing (product owner).  
2. If Q1 = B: name distance provider and billing trust rules.  
3. Seed **Admin-configurable** fare tables (dev/staging only) — never hardcode in handlers.  
4. Approve Phase **6C** implementation plan from contract doc.  
5. Only then: migrations (report before prod apply), APIs, tests, then Rider «متاح».
