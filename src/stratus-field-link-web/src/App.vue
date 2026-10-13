<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import StatCard from './components/StatCard.vue';
import SketchTable from './components/SketchTable.vue';
import { fetchDashboard, fetchSketches } from './lib/api';
import type { Dashboard, SketchSummary } from './lib/types';

const dashboard = ref<Dashboard | null>(null);
const sketches = ref<SketchSummary[]>([]);
const error = ref<string | null>(null);
const loading = ref(true);

const statusFilter = ref<string>('');
const crewFilter = ref<string>('');

const crews = computed(() => dashboard.value?.countsByCrew.map((c) => c.key) ?? []);
const statuses = computed(() => dashboard.value?.countsByStatus.map((c) => c.key) ?? []);

async function load() {
  loading.value = true;
  error.value = null;
  try {
    const [d, s] = await Promise.all([
      fetchDashboard(),
      fetchSketches({ status: statusFilter.value || undefined, crewId: crewFilter.value || undefined, limit: '250' }),
    ]);
    dashboard.value = d;
    sketches.value = s;
  } catch (e) {
    error.value = e instanceof Error ? e.message : String(e);
  } finally {
    loading.value = false;
  }
}

onMounted(load);
</script>

<template>
  <div class="app">
    <h1>Stratus Field Link — Shop Review</h1>
    <p class="sub">Runs published from the field, waiting on the shop.</p>

    <div v-if="error" class="panel error">
      Could not reach the API: {{ error }}<br />
      Is <code>dotnet run</code> up on http://localhost:5072?
    </div>

    <template v-if="dashboard">
      <div class="grid">
        <StatCard label="Sketches" :value="dashboard.totalSketches" />
        <StatCard label="Awaiting review" :value="dashboard.needsReviewCount" />
        <StatCard label="Past 24h SLA" :value="dashboard.staleReviewCount" tone="warn" />
        <StatCard label="Length mismatch" :value="dashboard.lengthMismatchCount" tone="bad" />
        <StatCard label="Total footage" :value="`${dashboard.totalPublishedFeet.toFixed(0)} ft`" />
      </div>

      <div class="panel">
        <h2>Oldest awaiting review</h2>
        <SketchTable :rows="dashboard.oldestAwaitingReview" />
      </div>

      <div class="panel">
        <h2>Throughput</h2>
        <div class="todo">
          <strong>Not built yet.</strong>
          The shop wants to know how much work the crews are publishing per day and whether
          review is keeping up. Nobody has defined what that number is. See
          <code>DashboardEndpoints.cs</code>.
        </div>
      </div>

      <div class="panel">
        <h2>All sketches</h2>
        <div class="toolbar">
          <select v-model="statusFilter" @change="load">
            <option value="">All statuses</option>
            <option v-for="s in statuses" :key="s" :value="s">{{ s }}</option>
          </select>
          <select v-model="crewFilter" @change="load">
            <option value="">All crews</option>
            <option v-for="c in crews" :key="c" :value="c">{{ c }}</option>
          </select>
          <button type="button" @click="load">Refresh</button>
          <span v-if="loading" class="muted">loading…</span>
          <span class="muted">{{ sketches.length }} shown</span>
        </div>
        <SketchTable :rows="sketches" />
      </div>
    </template>
  </div>
</template>
