# Phase 2.1 live verification

GeneratedUtc: 2026-08-20T14:37:29.0947382Z

## Migrations applied
- `20260613124239_InitialCreate`
- `20260613210949_AddTripIdToSupportTicket`
- `20260620091200_AddLandingMarketingTables`
- `20260704133030_AddRoutePolylineFields`
- `20260704133452_ApplyRoutePolylineSchema`
- `20260706190202_AddWaitlistRouteLink`
- `20260725133243_AddRouteRequestPreferredVehicleType`
- `20260811141458_AddDriverKycAndDocuments`
- `20260817080957_AddRouteDemandGroupStates`
- `20260818094529_AddSocialAuthProviders`
- `20260819133151_AddLegalDocumentEnglish`
- `20260819140102_AddUserActiveSubscription`

Missing vs codebase (not applied):
- `20260820122126_Phase3ShuttlePricingCommissionConcurrency` (CommissionRulesSet)
- `20260820131615_PhasePricingRulesAndEarnings` (PricingRulesSet)
- UsersSet schema drift: history lists `AddUserActiveSubscription` but column `SubscriptionActivatedAt` missing on include of full User

## Vehicle capacities (authoritative)
- **CarShuttle** (Shuttlez Car): **4** — VEHICLE_MASTER
- **MiniBus** (Microbus): **13** — VEHICLE_MASTER
- **Bus** (Bus): **24** — VEHICLE_MASTER

## Active RoutesSet (for matching)
Count: 17
- `2cc5f6b1-b328-4279-b0e0-e1a0209a8748` **6 أكتوبر - وسط البلد** → keys: `6 أكتوبر|وسط البلد`
- `0b4b1d84-8824-4e23-b48b-34678f5074f1` **Helwan - Nasr City** → keys: `helwan|nasr city`
- `94b01a12-10a7-41d8-8556-ee6400fc2584` **التجمع الخامس - مدينة نصر** → keys: `التجمع الخامس|مدينة نصر`
- `155afbc2-4013-452e-babb-4cafa5324036` **الدقي - التجمع الخامس** → keys: `التجمع الخامس|الدقي`
- `2d726bf8-63d5-4e5e-8283-2cf916ff6e5b` **الزقازيق - مدينة نصر** → keys: `الزقازيق|مدينة نصر`
- `6dfd6984-1044-4d9e-a8c4-4b3820a7b451` **الشروق - حلوان** → keys: `الشروق|حلوان`
- `e17f053a-2704-412e-803c-245cf8da4d7b` **الشروق - مدينة نصر** → keys: `الشروق|مدينة نصر`
- `6b9a9fc1-c157-483d-863b-bc4ebbb2d08b` **الشيخ زايد - مدينة نصر** → keys: `الشيخ زايد|مدينة نصر`
- `3ebc0a6e-67a3-4c14-bdfa-4274ed1133cb` **المعادي - مدينة نصر** → keys: `المعادي|مدينة نصر`
- `c2c6f4bb-2182-47c5-bb60-e31d9023a7c9` **المهندسين - مدينة نصر** → keys: `المهندسين|مدينة نصر`
- `773ab2e3-ef6e-4e3c-b8c1-0790bc3a61c3` **حلوان - مدينة نصر** → keys: `حلوان|مدينة نصر`
- `ef9fb862-7e42-41d8-a973-ab5a1b3b2433` **حلوان البلد - الحي الأول** → keys: `الحي الأول|حلوان البلد`
- `6fecba76-91c3-4a28-929d-fae317c23f57` **حلوان, القاهرة - مدينة نصر, القاهرة** → keys: `حلوان, القاهرة|مدينة نصر, القاهرة`
- `04bec6d5-f380-4f0d-91b4-78af198f36df` **شبرا الخيمة - وسط البلد** → keys: `شبرا الخيمة|وسط البلد`
- `7d6ddecc-1823-46ff-964a-25420ea2ccc4` **مدينة العبور - مدينة نصر** → keys: `مدينة العبور|مدينة نصر`
- `306465d5-fc86-4eb5-93db-ea7e82e36f9e` **مدينة بدر - مدينة نصر** → keys: `مدينة بدر|مدينة نصر`
- `14a4ac4e-3ca9-49e9-ae1f-4ba1470737ee` **مصر الجديدة - المعادي** → keys: `المعادي|مصر الجديدة`

## PricingRulesSet
**ABSENT / ERROR:** 42P01: relation "PricingRulesSet" does not exist

## Route Demand groups: 31

| # | Route | Demand | Unique | Confirmed | RecVehicle | BandCap | AuthCap | Src | Conflict | Status | Reason | Linked | OW | RT | Occ% |
|---|-------|--------|--------|-----------|------------|---------|---------|-----|----------|--------|--------|--------|----|----|------|
| 1 | حلوان، القاهرة ↔ مصر الجديدة، القاهرة | 8 | 7 | 0 | Microbus | 13 | 13 | VEHICLE_MASTER | no | UNKNOWN | MISSING_ROUTE_LINK | no | null | null | 0 |
| 2 | التجمع الخامس، القاهرة ↔ حلوان، القاهرة | 3 | 3 | 0 | Shuttlez Car | 3 | 4 | VEHICLE_MASTER | YES | UNKNOWN | MISSING_ROUTE_LINK | no | null | null | 0 |
| 3 | الحي السابع، القاهرة ↔ حلوان، القاهرة | 3 | 3 | 0 | Shuttlez Car | 3 | 4 | VEHICLE_MASTER | YES | UNKNOWN | MISSING_ROUTE_LINK | no | null | null | 0 |
| 4 | Abu Qir، Alexandria ↔ أبنوب، Assiut | 2 | 1 | 0 | Shuttlez Car | 3 | 4 | VEHICLE_MASTER | YES | UNKNOWN | MISSING_ROUTE_LINK | no | null | null | 0 |
| 5 | التجمع الأول، القاهرة ↔ حلوان، القاهرة | 2 | 2 | 0 | Shuttlez Car | 3 | 4 | VEHICLE_MASTER | YES | UNKNOWN | MISSING_ROUTE_LINK | no | null | null | 0 |
| 6 | الدقي، الجيزة ↔ حلوان، القاهرة | 2 | 2 | 0 | Shuttlez Car | 3 | 4 | VEHICLE_MASTER | YES | UNKNOWN | MISSING_ROUTE_LINK | no | null | null | 0 |
| 7 | الشروق، القاهرة ↔ الشروق، القاهرة | 2 | 2 | 0 | Shuttlez Car | 3 | 4 | VEHICLE_MASTER | YES | UNKNOWN | MISSING_ROUTE_LINK | no | null | null | 0 |
| 8 | المعادي، القاهرة ↔ مصر الجديدة، القاهرة | 2 | 2 | 0 | Shuttlez Car | 3 | 4 | VEHICLE_MASTER | YES | UNKNOWN | MISSING_ROUTE_LINK | no | null | null | 0 |
| 9 | حلوان, القاهرة ↔ مدينة نصر, القاهرة | 2 | 1 | 2 | Shuttlez Car | 3 | 13 | VEHICLE_MASTER | YES | NO_PRICING | NO_PRICING | yes | null | null | 15.38 |
| 10 | ????? ???, ??????? ↔ ?????, ??????? | 1 | 1 | 1 | Shuttlez Car | 3 | 4 | VEHICLE_MASTER | YES | UNKNOWN | MISSING_ROUTE_LINK | no | null | null | 25 |
| 11 | 15 مايو، القاهرة ↔ الحي السادس، القاهرة | 1 | 1 | 0 | Shuttlez Car | 3 | 4 | VEHICLE_MASTER | YES | UNKNOWN | MISSING_ROUTE_LINK | no | null | null | 0 |
| 12 | Abu Qir، Alexandria ↔ ساحل سليم، Assiut | 1 | 1 | 0 | Shuttlez Car | 3 | 4 | VEHICLE_MASTER | YES | UNKNOWN | MISSING_ROUTE_LINK | no | null | null | 0 |
| 13 | Agami، Alexandria ↔ إدفو، Aswan | 1 | 1 | 0 | Shuttlez Car | 3 | 4 | VEHICLE_MASTER | YES | UNKNOWN | MISSING_ROUTE_LINK | no | null | null | 0 |
| 14 | أبو حمص، Beheira ↔ أبو سمبل، Aswan | 1 | 1 | 0 | Shuttlez Car | 3 | 4 | VEHICLE_MASTER | YES | UNKNOWN | MISSING_ROUTE_LINK | no | null | null | 0 |
| 15 | أسيوط، أسيوط ↔ الخليفة، القاهرة | 1 | 1 | 0 | Shuttlez Car | 3 | 4 | VEHICLE_MASTER | YES | UNKNOWN | MISSING_ROUTE_LINK | no | null | null | 0 |
| 16 | التبين, القاهرة ↔ التجمع الأول, القاهرة | 1 | 1 | 1 | Shuttlez Car | 3 | 4 | VEHICLE_MASTER | YES | UNKNOWN | MISSING_ROUTE_LINK | no | null | null | 25 |
| 17 | التجمع الثالث، القاهرة ↔ العجوزة، الجيزة | 1 | 1 | 0 | Shuttlez Car | 3 | 4 | VEHICLE_MASTER | YES | UNKNOWN | MISSING_ROUTE_LINK | no | null | null | 0 |
| 18 | التجمع الخامس، القاهرة ↔ الحي السابع، القاهرة | 1 | 1 | 0 | Shuttlez Car | 3 | 4 | VEHICLE_MASTER | YES | UNKNOWN | MISSING_ROUTE_LINK | no | null | null | 0 |
| 19 | التجمع الخامس، القاهرة ↔ الحي السادس، الجيزة | 1 | 1 | 0 | Shuttlez Car | 3 | 4 | VEHICLE_MASTER | YES | UNKNOWN | MISSING_ROUTE_LINK | no | null | null | 0 |
| 20 | الحى السادس, القاهرة ↔ حلوان البحرية, القاهرة | 1 | 1 | 0 | Shuttlez Car | 3 | 13 | VEHICLE_MASTER | YES | UNKNOWN | MISSING_ROUTE_LINK | no | null | null | 0 |
| 21 | الحي الأول, القاهرة ↔ المطرية, القاهرة | 1 | 1 | 0 | Shuttlez Car | 3 | 4 | VEHICLE_MASTER | YES | UNKNOWN | MISSING_ROUTE_LINK | no | null | null | 0 |
| 22 | الحي الثامن, القاهرة ↔ حلوان القبلية, القاهرة | 1 | 1 | 0 | Shuttlez Car | 3 | 13 | VEHICLE_MASTER | YES | UNKNOWN | MISSING_ROUTE_LINK | no | null | null | 0 |
| 23 | الحي الثامن، القاهرة ↔ حلوان، القاهرة | 1 | 1 | 0 | Shuttlez Car | 3 | 4 | VEHICLE_MASTER | YES | UNKNOWN | MISSING_ROUTE_LINK | no | null | null | 0 |
| 24 | الحي السابع، القاهرة ↔ المعادي، القاهرة | 1 | 1 | 0 | Shuttlez Car | 3 | 4 | VEHICLE_MASTER | YES | UNKNOWN | MISSING_ROUTE_LINK | no | null | null | 0 |
| 25 | الحي السادس، القاهرة ↔ حلوان، القاهرة | 1 | 1 | 0 | Shuttlez Car | 3 | 4 | VEHICLE_MASTER | YES | UNKNOWN | MISSING_ROUTE_LINK | no | null | null | 0 |
| 26 | الرحاب، القاهرة ↔ حلوان، القاهرة | 1 | 1 | 0 | Shuttlez Car | 3 | 4 | VEHICLE_MASTER | YES | UNKNOWN | MISSING_ROUTE_LINK | no | null | null | 0 |
| 27 | القرية الذكية, الجيزة ↔ بولاق, القاهرة | 1 | 1 | 1 | Shuttlez Car | 3 | 4 | VEHICLE_MASTER | YES | UNKNOWN | MISSING_ROUTE_LINK | no | null | null | 25 |
| 28 | المطرية، القاهرة ↔ حلوان، القاهرة | 1 | 1 | 0 | Shuttlez Car | 3 | 4 | VEHICLE_MASTER | YES | UNKNOWN | MISSING_ROUTE_LINK | no | null | null | 0 |
| 29 | النزهة، القاهرة ↔ الهرم، الجيزة | 1 | 1 | 0 | Shuttlez Car | 3 | 4 | VEHICLE_MASTER | YES | UNKNOWN | MISSING_ROUTE_LINK | no | null | null | 0 |
| 30 | النزهة، القاهرة ↔ حلوان، القاهرة | 1 | 1 | 0 | Shuttlez Car | 3 | 4 | VEHICLE_MASTER | YES | UNKNOWN | MISSING_ROUTE_LINK | no | null | null | 0 |
| 31 | حلوان، القاهرة ↔ كرداسة، الجيزة | 1 | 1 | 0 | Shuttlez Car | 3 | 4 | VEHICLE_MASTER | YES | UNKNOWN | MISSING_ROUTE_LINK | no | null | null | 0 |

## Summary counts
- Groups: 31
- Capacity conflicts: 30
- MISSING_ROUTE_LINK: 30
- NO_PRICING: 1
- PricingLinked: 1
