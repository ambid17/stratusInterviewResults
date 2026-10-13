#!/usr/bin/env node
/**
 * Regenerates src/Data/sketches.seed.json.
 *
 * Deterministic: same output every run. ~150 field sketches across 8 packages
 * and 6 crews, with the status history the shop would have accumulated.
 *
 *   node scripts/generate-seed.mjs
 */

import { writeFileSync, mkdirSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const repoRoot = join(dirname(fileURLToPath(import.meta.url)), '..');
const outPath = join(repoRoot, 'src', 'Data', 'sketches.seed.json');

const SKETCH_COUNT = 150;
const ANCHOR = Date.parse('2026-08-01T16:00:00Z');
const DAY = 86_400_000;

// --- deterministic PRNG -----------------------------------------------------
function mulberry32(seed) {
  return function rand() {
    seed |= 0;
    seed = (seed + 0x6d2b79f5) | 0;
    let t = Math.imul(seed ^ (seed >>> 15), 1 | seed);
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}
const rand = mulberry32(20260807);
const pick = (arr) => arr[Math.floor(rand() * arr.length)];
const between = (lo, hi) => lo + rand() * (hi - lo);
const intBetween = (lo, hi) => Math.floor(between(lo, hi + 1));
const round2 = (n) => Math.round(n * 100) / 100;

// --- reference data ---------------------------------------------------------
const PACKAGES = [
  'PKG-CHW-L02', 'PKG-CHW-L03', 'PKG-HHW-L02', 'PKG-DUCT-L04',
  'PKG-MED-GAS-L01', 'PKG-CHW-L05', 'PKG-DUCT-L06', 'PKG-ELEC-L03',
];
const CREWS = ['CREW-01', 'CREW-02', 'CREW-03', 'CREW-04', 'CREW-05', 'CREW-06'];
const CLIENT_VERSIONS = ['field-1.4.0', 'field-1.4.1', 'field-1.4.2'];
const LEVEL_ELEVATIONS = [0, 13.5, 27, 40.5, 54];

const DISCIPLINES = [
  { name: 'Piping', materials: ['Carbon Steel', 'Copper Type L', 'CPVC', 'Grooved Carbon Steel'], diameters: [1, 1.5, 2, 3, 4, 6, 8] },
  { name: 'SheetMetal', materials: ['Galvanized 26ga', 'Galvanized 24ga', 'Stainless 304'], diameters: [8, 10, 12, 14, 18, 24] },
  { name: 'Electrical', materials: ['EMT', 'Rigid', 'PVC Sch40'], diameters: [0.75, 1, 1.25, 2] },
];

const REJECT_NOTES = [
  'Route conflicts with structural steel at grid C-4. Re-sketch below the beam.',
  'Diameter does not match the approved schedule for this package.',
  'Missing takeoff at the branch. Please add the tee node.',
  'Duplicate of an already-approved run in this package.',
];

// --- geometry ---------------------------------------------------------------
function buildNodes(hasRiser) {
  const level = pick(LEVEL_ELEVATIONS);
  let x = round2(between(0, 240));
  let z = round2(between(0, 180));
  let y = round2(level + between(8, 11)); // hung off the deck

  const nodes = [{ x, y, z }];
  const turns = intBetween(1, 4);
  let horizontal = rand() < 0.5;

  for (let i = 0; i < turns; i++) {
    const run = round2(between(6, 44));
    if (horizontal) {
      x = round2(x + (rand() < 0.5 ? run : -run));
    } else {
      z = round2(z + (rand() < 0.5 ? run : -run));
    }
    horizontal = !horizontal;
    nodes.push({ x, y, z });
  }

  if (hasRiser) {
    // A vertical drop or riser to the level above/below. Purely a change in y.
    const rise = round2(between(7, 16)) * (rand() < 0.65 ? -1 : 1);
    y = round2(y + rise);
    nodes.push({ x, y, z });

    // Usually the run continues after the riser.
    if (rand() < 0.6) {
      const run = round2(between(6, 30));
      if (horizontal) {
        x = round2(x + (rand() < 0.5 ? run : -run));
      } else {
        z = round2(z + (rand() < 0.5 ? run : -run));
      }
      nodes.push({ x, y, z });
    }
  }

  return nodes;
}

/** Full 3D length. This is what the shop bills off. */
function length3d(nodes) {
  let total = 0;
  for (let i = 1; i < nodes.length; i++) {
    const a = nodes[i - 1];
    const b = nodes[i];
    total += Math.hypot(b.x - a.x, b.y - a.y, b.z - a.z);
  }
  return total;
}

/** Plan-view length, i.e. what the field client reported at capture time. */
function lengthPlan(nodes) {
  let total = 0;
  for (let i = 1; i < nodes.length; i++) {
    const a = nodes[i - 1];
    const b = nodes[i];
    total += Math.hypot(b.x - a.x, b.z - a.z);
  }
  return total;
}

// --- status ----------------------------------------------------------------
function pickStatus() {
  const r = rand();
  if (r < 0.40) return 'Published';
  if (r < 0.55) return 'UnderReview';
  if (r < 0.93) return 'Approved';
  return 'Rejected';
}

function buildHistory(status, capturedAt, publishedAt, crewId) {
  const history = [
    { from: 'Draft', to: 'Published', atUtc: new Date(publishedAt).toISOString(), actor: crewId },
  ];

  if (status === 'Published') return history;

  const reviewAt = publishedAt + between(0.5, 30) * 3_600_000;
  history.push({ from: 'Published', to: 'UnderReview', atUtc: new Date(reviewAt).toISOString(), actor: 'shop-planner' });
  if (status === 'UnderReview') return history;

  const decidedAt = reviewAt + between(0.25, 20) * 3_600_000;
  history.push({ from: 'UnderReview', to: status, atUtc: new Date(decidedAt).toISOString(), actor: 'shop-planner' });
  return history;
}

// --- build ------------------------------------------------------------------
const sketches = [];
for (let i = 0; i < SKETCH_COUNT; i++) {
  const discipline = pick(DISCIPLINES);
  const crewId = pick(CREWS);

  // A little over a fifth of real field work changes elevation.
  const hasRiser = rand() < 0.22;
  const nodes = buildNodes(hasRiser);

  const capturedAt = ANCHOR - between(0, 21) * DAY;
  const publishedAt = capturedAt + between(1, 90) * 60_000;
  const status = pickStatus();

  sketches.push({
    id: `SK-${String(1000 + i)}`,
    packageId: pick(PACKAGES),
    crewId,
    discipline: discipline.name,
    diameterInches: pick(discipline.diameters),
    material: pick(discipline.materials),
    nodes,
    status,
    capturedAtUtc: new Date(capturedAt).toISOString(),
    publishedAtUtc: new Date(publishedAt).toISOString(),
    clientLengthFeet: round2(lengthPlan(nodes)),
    clientVersion: pick(CLIENT_VERSIONS),
    reviewNotes: status === 'Rejected' ? pick(REJECT_NOTES) : null,
    history: buildHistory(status, capturedAt, publishedAt, crewId),
  });
}

sketches.sort((a, b) => a.id.localeCompare(b.id));

mkdirSync(dirname(outPath), { recursive: true });
writeFileSync(outPath, `${JSON.stringify(sketches, null, 2)}\n`, 'utf8');

// --- summary ----------------------------------------------------------------
const withRise = sketches.filter((s) => length3d(s.nodes) - lengthPlan(s.nodes) > 0.01);
const totalPlan = sketches.reduce((sum, s) => sum + lengthPlan(s.nodes), 0);
const total3d = sketches.reduce((sum, s) => sum + length3d(s.nodes), 0);

console.log(`Wrote ${sketches.length} sketches to ${outPath}`);
console.log(`  runs with elevation change: ${withRise.length}`);
console.log(`  total plan length : ${totalPlan.toFixed(1)} ft`);
console.log(`  total 3d length   : ${total3d.toFixed(1)} ft`);
