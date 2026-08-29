# Phase 2.3 read-only verification

GeneratedUtc: 2026-08-22T06:18:52.9007174Z

**No automatic mapping was performed.**

## Applied Phase migrations
- `20260820122126_Phase3ShuttlePricingCommissionConcurrency`
- `20260820131615_PhasePricingRulesAndEarnings`
- `20260820143356_Phase22_RouteDemandMappedRoute`
- `20260821074715_Phase4C_UserDevices`

## Capacities
- CarShuttle: 4 (VEHICLE_MASTER)
- MiniBus: 13 (VEHICLE_MASTER)
- Bus: 24 (VEHICLE_MASTER)

## Pricing active rules: 2
## Commission active rules: 1

## Explicit MappedRouteId states: 0

## Demand groups: 33

| Metric | Count |
|--------|-------|
| Total | 33 |
| PricingLinked | 1 |
| MISSING_ROUTE_LINK | 32 |
| EXPLICIT maps (readiness) | 0 |
| EXACT_KEY | 1 |
| MANUAL_OVERRIDE | 0 |
| Pricing available | 1 |
| NO_PRICING | 0 |
| READY | 0 |
| ALMOST_READY | 0 |
| NOT_READY | 1 |
| MISSING_CONFIGURATION | 0 |
| UNKNOWN | 32 |
| Capacity conflicts | 32 |

## Top corridors (read-only)
| Route | Demand | Unique | Confirmed | Link | Status | Reason | OW | Cap | Conflict |
|-------|--------|--------|-----------|------|--------|--------|----|-----|----------|
| حلوان، القاهرة ↔ مصر الجديدة، القاهرة | 8 | 7 | 0 | — | UNKNOWN | MISSING_ROUTE_LINK | null | 13 | no |
| التجمع الخامس، القاهرة ↔ حلوان، القاهرة | 3 | 3 | 0 | — | UNKNOWN | MISSING_ROUTE_LINK | null | 4 | YES |
| الحي السابع، القاهرة ↔ حلوان، القاهرة | 3 | 3 | 0 | — | UNKNOWN | MISSING_ROUTE_LINK | null | 4 | YES |
| Abu Qir، Alexandria ↔ أبنوب، Assiut | 2 | 1 | 0 | — | UNKNOWN | MISSING_ROUTE_LINK | null | 4 | YES |
| التجمع الأول، القاهرة ↔ حلوان، القاهرة | 2 | 2 | 0 | — | UNKNOWN | MISSING_ROUTE_LINK | null | 4 | YES |
| الدقي، الجيزة ↔ حلوان، القاهرة | 2 | 2 | 0 | — | UNKNOWN | MISSING_ROUTE_LINK | null | 4 | YES |
| الشروق، القاهرة ↔ الشروق، القاهرة | 2 | 2 | 0 | — | UNKNOWN | MISSING_ROUTE_LINK | null | 4 | YES |
| المعادي، القاهرة ↔ مصر الجديدة، القاهرة | 2 | 2 | 0 | — | UNKNOWN | MISSING_ROUTE_LINK | null | 4 | YES |
| حلوان, القاهرة ↔ مدينة نصر, القاهرة | 2 | 1 | 2 | EXACT_KEY | NOT_READY | BELOW_MINIMUM | 75.00 | 13 | YES |
| ????? ???, ??????? ↔ ?????, ??????? | 1 | 1 | 1 | — | UNKNOWN | MISSING_ROUTE_LINK | null | 4 | YES |
| 15 مايو، القاهرة ↔ الحي السادس، القاهرة | 1 | 1 | 0 | — | UNKNOWN | MISSING_ROUTE_LINK | null | 4 | YES |
| Abu Qir، Alexandria ↔ ساحل سليم، Assiut | 1 | 1 | 0 | — | UNKNOWN | MISSING_ROUTE_LINK | null | 4 | YES |
| Agami، Alexandria ↔ إدفو، Aswan | 1 | 1 | 0 | — | UNKNOWN | MISSING_ROUTE_LINK | null | 4 | YES |
| أبو حمص، Beheira ↔ أبو سمبل، Aswan | 1 | 1 | 0 | — | UNKNOWN | MISSING_ROUTE_LINK | null | 4 | YES |
| أسيوط، أسيوط ↔ الخليفة، القاهرة | 1 | 1 | 0 | — | UNKNOWN | MISSING_ROUTE_LINK | null | 4 | YES |
| التبين, القاهرة ↔ التجمع الأول, القاهرة | 1 | 1 | 1 | — | UNKNOWN | MISSING_ROUTE_LINK | null | 4 | YES |
| التجمع الثالث، القاهرة ↔ العجوزة، الجيزة | 1 | 1 | 0 | — | UNKNOWN | MISSING_ROUTE_LINK | null | 4 | YES |
| التجمع الخامس، القاهرة ↔ الحي السابع، القاهرة | 1 | 1 | 0 | — | UNKNOWN | MISSING_ROUTE_LINK | null | 4 | YES |
| التجمع الخامس، القاهرة ↔ الحي السادس، الجيزة | 1 | 1 | 0 | — | UNKNOWN | MISSING_ROUTE_LINK | null | 4 | YES |
| التجمع الخامس، القاهرة ↔ المعادي، القاهرة | 1 | 1 | 0 | — | UNKNOWN | MISSING_ROUTE_LINK | null | 4 | YES |
| الحى السادس, القاهرة ↔ حلوان البحرية, القاهرة | 1 | 1 | 0 | — | UNKNOWN | MISSING_ROUTE_LINK | null | 13 | YES |
| الحي الأول, القاهرة ↔ المطرية, القاهرة | 1 | 1 | 0 | — | UNKNOWN | MISSING_ROUTE_LINK | null | 4 | YES |
| الحي الثامن, القاهرة ↔ حلوان القبلية, القاهرة | 1 | 1 | 0 | — | UNKNOWN | MISSING_ROUTE_LINK | null | 13 | YES |
| الحي الثامن، القاهرة ↔ حلوان، القاهرة | 1 | 1 | 0 | — | UNKNOWN | MISSING_ROUTE_LINK | null | 4 | YES |
| الحي السابع، القاهرة ↔ المعادي، القاهرة | 1 | 1 | 0 | — | UNKNOWN | MISSING_ROUTE_LINK | null | 4 | YES |
| الحي السادس، القاهرة ↔ حلوان، القاهرة | 1 | 1 | 0 | — | UNKNOWN | MISSING_ROUTE_LINK | null | 4 | YES |
| الرحاب، القاهرة ↔ حلوان، القاهرة | 1 | 1 | 0 | — | UNKNOWN | MISSING_ROUTE_LINK | null | 4 | YES |
| القرية الذكية, الجيزة ↔ بولاق, القاهرة | 1 | 1 | 1 | — | UNKNOWN | MISSING_ROUTE_LINK | null | 4 | YES |
| المطرية، القاهرة ↔ حلوان، القاهرة | 1 | 1 | 0 | — | UNKNOWN | MISSING_ROUTE_LINK | null | 4 | YES |
| النزهة، القاهرة ↔ الهرم، الجيزة | 1 | 1 | 0 | — | UNKNOWN | MISSING_ROUTE_LINK | null | 4 | YES |
| النزهة، القاهرة ↔ حلوان، القاهرة | 1 | 1 | 0 | — | UNKNOWN | MISSING_ROUTE_LINK | null | 4 | YES |

## Phase 5 — full demand audit (all groups, read-only)
| routeKey | origin | destination | confirmed | minRequired | status | mapped | reason |
|----------|--------|-------------|-----------|-------------|--------|--------|--------|
| `حلوان, القاهرة/مدينة نصر, القاهرة` | حلوان, القاهرة | مدينة نصر, القاهرة | 2 | 8 | NOT_READY | EXACT_KEY | BELOW_MINIMUM |
| `????? ???, ???????/?????, ???????` | ????? ???, ??????? | ?????, ??????? | 1 | — | UNKNOWN | UNMAPPED | MISSING_ROUTE_LINK |
| `التبين, القاهرة/التجمع الأول, القاهرة` | التبين, القاهرة | التجمع الأول, القاهرة | 1 | — | UNKNOWN | UNMAPPED | MISSING_ROUTE_LINK |
| `القرية الذكية, الجيزة/بولاق, القاهرة` | القرية الذكية, الجيزة | بولاق, القاهرة | 1 | — | UNKNOWN | UNMAPPED | MISSING_ROUTE_LINK |
| `حلوان، القاهرة/مصر الجديدة، القاهرة` | حلوان، القاهرة | مصر الجديدة، القاهرة | 0 | — | UNKNOWN | UNMAPPED | MISSING_ROUTE_LINK |
| `التجمع الخامس، القاهرة/حلوان، القاهرة` | التجمع الخامس، القاهرة | حلوان، القاهرة | 0 | — | UNKNOWN | UNMAPPED | MISSING_ROUTE_LINK |
| `الحي السابع، القاهرة/حلوان، القاهرة` | الحي السابع، القاهرة | حلوان، القاهرة | 0 | — | UNKNOWN | UNMAPPED | MISSING_ROUTE_LINK |
| `abu qir، alexandria/أبنوب، assiut` | Abu Qir، Alexandria | أبنوب، Assiut | 0 | — | UNKNOWN | UNMAPPED | MISSING_ROUTE_LINK |
| `التجمع الأول، القاهرة/حلوان، القاهرة` | التجمع الأول، القاهرة | حلوان، القاهرة | 0 | — | UNKNOWN | UNMAPPED | MISSING_ROUTE_LINK |
| `الدقي، الجيزة/حلوان، القاهرة` | الدقي، الجيزة | حلوان، القاهرة | 0 | — | UNKNOWN | UNMAPPED | MISSING_ROUTE_LINK |
| `الشروق، القاهرة/الشروق، القاهرة` | الشروق، القاهرة | الشروق، القاهرة | 0 | — | UNKNOWN | UNMAPPED | MISSING_ROUTE_LINK |
| `المعادي، القاهرة/مصر الجديدة، القاهرة` | المعادي، القاهرة | مصر الجديدة، القاهرة | 0 | — | UNKNOWN | UNMAPPED | MISSING_ROUTE_LINK |
| `15 مايو، القاهرة/الحي السادس، القاهرة` | 15 مايو، القاهرة | الحي السادس، القاهرة | 0 | — | UNKNOWN | UNMAPPED | MISSING_ROUTE_LINK |
| `abu qir، alexandria/ساحل سليم، assiut` | Abu Qir، Alexandria | ساحل سليم، Assiut | 0 | — | UNKNOWN | UNMAPPED | MISSING_ROUTE_LINK |
| `agami، alexandria/إدفو، aswan` | Agami، Alexandria | إدفو، Aswan | 0 | — | UNKNOWN | UNMAPPED | MISSING_ROUTE_LINK |
| `أبو حمص، beheira/أبو سمبل، aswan` | أبو حمص، Beheira | أبو سمبل، Aswan | 0 | — | UNKNOWN | UNMAPPED | MISSING_ROUTE_LINK |
| `أسيوط، أسيوط/الخليفة، القاهرة` | أسيوط، أسيوط | الخليفة، القاهرة | 0 | — | UNKNOWN | UNMAPPED | MISSING_ROUTE_LINK |
| `التجمع الثالث، القاهرة/العجوزة، الجيزة` | التجمع الثالث، القاهرة | العجوزة، الجيزة | 0 | — | UNKNOWN | UNMAPPED | MISSING_ROUTE_LINK |
| `التجمع الخامس، القاهرة/الحي السابع، القاهرة` | التجمع الخامس، القاهرة | الحي السابع، القاهرة | 0 | — | UNKNOWN | UNMAPPED | MISSING_ROUTE_LINK |
| `التجمع الخامس، القاهرة/الحي السادس، الجيزة` | التجمع الخامس، القاهرة | الحي السادس، الجيزة | 0 | — | UNKNOWN | UNMAPPED | MISSING_ROUTE_LINK |
| `التجمع الخامس، القاهرة/المعادي، القاهرة` | التجمع الخامس، القاهرة | المعادي، القاهرة | 0 | — | UNKNOWN | UNMAPPED | MISSING_ROUTE_LINK |
| `الحى السادس, القاهرة/حلوان البحرية, القاهرة` | الحى السادس, القاهرة | حلوان البحرية, القاهرة | 0 | — | UNKNOWN | UNMAPPED | MISSING_ROUTE_LINK |
| `الحي الأول, القاهرة/المطرية, القاهرة` | الحي الأول, القاهرة | المطرية, القاهرة | 0 | — | UNKNOWN | UNMAPPED | MISSING_ROUTE_LINK |
| `الحي الثامن, القاهرة/حلوان القبلية, القاهرة` | الحي الثامن, القاهرة | حلوان القبلية, القاهرة | 0 | — | UNKNOWN | UNMAPPED | MISSING_ROUTE_LINK |
| `الحي الثامن، القاهرة/حلوان، القاهرة` | الحي الثامن، القاهرة | حلوان، القاهرة | 0 | — | UNKNOWN | UNMAPPED | MISSING_ROUTE_LINK |
| `الحي السابع، القاهرة/المعادي، القاهرة` | الحي السابع، القاهرة | المعادي، القاهرة | 0 | — | UNKNOWN | UNMAPPED | MISSING_ROUTE_LINK |
| `الحي السادس، القاهرة/حلوان، القاهرة` | الحي السادس، القاهرة | حلوان، القاهرة | 0 | — | UNKNOWN | UNMAPPED | MISSING_ROUTE_LINK |
| `الرحاب، القاهرة/حلوان، القاهرة` | الرحاب، القاهرة | حلوان، القاهرة | 0 | — | UNKNOWN | UNMAPPED | MISSING_ROUTE_LINK |
| `المطرية، القاهرة/حلوان، القاهرة` | المطرية، القاهرة | حلوان، القاهرة | 0 | — | UNKNOWN | UNMAPPED | MISSING_ROUTE_LINK |
| `النزهة، القاهرة/الهرم، الجيزة` | النزهة، القاهرة | الهرم، الجيزة | 0 | — | UNKNOWN | UNMAPPED | MISSING_ROUTE_LINK |
| `النزهة، القاهرة/حلوان، القاهرة` | النزهة، القاهرة | حلوان، القاهرة | 0 | — | UNKNOWN | UNMAPPED | MISSING_ROUTE_LINK |
| `حلوان، القاهرة/كرداسة، الجيزة` | حلوان، القاهرة | كرداسة، الجيزة | 0 | — | UNKNOWN | UNMAPPED | MISSING_ROUTE_LINK |
| `شبرا، القاهرة/غرب سوميد، الجيزة` | شبرا، القاهرة | غرب سوميد، الجيزة | 0 | — | UNKNOWN | UNMAPPED | MISSING_ROUTE_LINK |
