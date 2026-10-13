<script setup lang="ts">
import type { SketchSummary } from '../lib/types';

defineProps<{ rows: SketchSummary[] }>();

function age(iso: string | null): string {
  if (!iso) return '—';
  const hours = (Date.now() - Date.parse(iso)) / 3_600_000;
  if (hours < 1) return `${Math.round(hours * 60)}m`;
  if (hours < 48) return `${Math.round(hours)}h`;
  return `${Math.round(hours / 24)}d`;
}
</script>

<template>
  <table>
    <thead>
      <tr>
        <th>Sketch</th>
        <th>Package</th>
        <th>Crew</th>
        <th>Discipline</th>
        <th class="num">Dia (in)</th>
        <th class="num">Length (ft)</th>
        <th class="num">Field said</th>
        <th class="num">Rise (ft)</th>
        <th>Status</th>
        <th class="num">Age</th>
      </tr>
    </thead>
    <tbody>
      <tr v-for="row in rows" :key="row.id">
        <td>{{ row.id }}</td>
        <td>{{ row.packageId }}</td>
        <td>{{ row.crewId }}</td>
        <td>{{ row.discipline }}</td>
        <td class="num">{{ row.diameterInches }}</td>
        <td class="num">{{ row.lengthFeet.toFixed(1) }}</td>
        <td class="num" :class="{ flag: row.lengthMismatch }">{{ row.clientLengthFeet.toFixed(1) }}</td>
        <td class="num">{{ row.elevationChangeFeet.toFixed(1) }}</td>
        <td><span class="pill" :class="row.status">{{ row.status }}</span></td>
        <td class="num">{{ age(row.publishedAtUtc ?? row.capturedAtUtc) }}</td>
      </tr>
      <tr v-if="rows.length === 0">
        <td colspan="10" class="muted">Nothing here.</td>
      </tr>
    </tbody>
  </table>
</template>
