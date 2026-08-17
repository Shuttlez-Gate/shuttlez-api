const fs = require('fs');
const path = require('path');

const srcPath =
  'F:/aa_MOC/Flutter_Projects/shuttlez-land-claude/src/app/core/data/cairo-locations.data.ts';
const src = fs.readFileSync(srcPath, 'utf8');

function extractExportConst(name) {
  const marker = `export const ${name}`;
  const start = src.indexOf(marker);
  if (start < 0) throw new Error(`${name} not found`);
  const eq = src.indexOf('=', start);
  const brace = src.indexOf('{', eq);
  const bracket = src.indexOf('[', eq);
  const openIdx =
    brace < 0 ? bracket : bracket < 0 ? brace : Math.min(brace, bracket);
  const openChar = src[openIdx];
  const closeChar = openChar === '{' ? '}' : ']';
  let depth = 0;
  let end = -1;
  for (let p = openIdx; p < src.length; p++) {
    const c = src[p];
    if (c === openChar) depth++;
    else if (c === closeChar) {
      depth--;
      if (depth === 0) {
        end = p + 1;
        break;
      }
    }
  }
  return src.slice(openIdx, end);
}

function toJsonish(literal) {
  return literal.replace(/(\w+)\s*:/g, '"$1":').replace(/,(\s*[}\]])/g, '$1');
}

const names = [...src.matchAll(/export const ([A-Z0-9_]+_GOVERNORATE)/g)].map(
  (m) => m[1],
);

const decls = names
  .map((name) => `const ${name} = ${toJsonish(extractExportConst(name))};`)
  .join('\n');

const egyptLiteral = toJsonish(extractExportConst('EGYPT_LOCATION_OVERRIDES'));
const data = Function(
  `"use strict";\n${decls}\nreturn (${egyptLiteral});`,
)();

function build(lang) {
  const cities = [];
  const regionsByCity = {};
  const areasByRegion = {};
  for (const g of data) {
    const gov = lang === 'en' ? g.governorateEn : g.governorateAr;
    cities.push(gov);
    regionsByCity[gov] = g.cities.map((c) =>
      lang === 'en' ? c.cityEn : c.cityAr,
    );
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
  JSON.stringify(
    {
      governors: names.length,
      arCities: ar.cities.length,
      arRegions: Object.values(ar.regionsByCity).reduce(
        (n, a) => n + a.length,
        0,
      ),
      arAreaKeys: Object.keys(ar.areasByRegion).length,
      sampleCairo: ar.regionsByCity['القاهرة']?.slice(0, 6),
      enCities: en.cities.length,
    },
    null,
    2,
  ),
);
