<script setup lang="ts">
defineProps<{
  label: string;
  value: string | number;
  tone?: 'warn' | 'bad';
}>();
</script>

<template>
  <div class="card" :class="tone">
    <div class="label">{{ label }}</div>
    <div class="value">{{ value }}</div>
  </div>
</template>
