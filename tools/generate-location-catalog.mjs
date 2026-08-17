const fs = require('fs');
const path = require('path');

const srcPath =
  'F:/aa_MOC/Flutter_Projects/shuttlez-land-claude/src/app/core/data/cairo-locations.data.ts';
const src = fs.readFileSync(srcPath, 'utf8');

const start = src.indexOf('export const EGYPT_LOCATION_OVERRIDES');
if (start < 0) throw new Error('EGYPT_LOCATION_OVERRIDES not found');
const eq = src.indexOf('=', start);
let i = src.indexOf('[', eq);
let depth = 0;
let end = -1;
for (let p = i; p < src.length; p++) {
  const c = src[p];
  if (c === '[') depth++;
  else if (c === ']') {
    depth--;
    if (depth === 0) {
      end = p + 1;
      break;
    }
  }
}
let literal = src.slice(i, end);
literal = literal.replace(/(\w+)\s*:/g, '"$1":').replace(/,(\s*[}\]])/g, '$1');
const data = Function(`"use strict"; return (${literal});`)();

function build(lang) {
  const cities = [];
  const regionsByCity = {};
  const areasByRegion = {};
  for (const g of data) {
    const gov = lang === 'en' ? g.governorateEn : g.governorateAr;
    cities.push(gov);
    regionsByCity[gov] = g.cities.map((c) => (lang === 'en' ? c.cityEn : c.cityAr));
    for (const c of g.cities) {
      if (!c.areas || !c.areas.length) continue;
      const cityName = lang === 'en' ? c.cityEn : c.cityAr;
      areasByRegion[cityName] = c.areas.map((a) =>
        lang === 'en' ? a.areaEn : a.areaAr,
      );
    }
  }
  return { cities, regionsByCity, areasByRegion };
}

const outDir =
  'F:/aa_MOC/Flutter_Projects/shuttlez-cursor-api/src/Shuttlez.Application/Landing/Data';
const ar = build('ar');
const en = build('en');
fs.writeFileSync(
  path.join(outDir, 'egypt-location-catalog.json'),
  JSON.stringify(ar, null, 2),
  'utf8',
);
fs.writeFileSync(
  path.join(outDir, 'egypt-location-catalog.en.json'),
  JSON.stringify(en, null, 2),
  'utf8',
);
console.log(
  JSON.stringify({
    arCities: ar.cities.length,
    arRegions: Object.values(ar.regionsByCity).reduce((n, a) => n + a.length, 0),
    arAreaKeys: Object.keys(ar.areasByRegion).length,
    enCities: en.cities.length,
  }),
);
