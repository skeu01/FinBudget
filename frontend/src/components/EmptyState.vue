<script setup lang="ts">
import { ArrowUpRight } from '@lucide/vue'
defineProps<{ title: string; description: string }>()
</script>

<template>
  <div class="empty-state">
    <div class="empty-state__icon"><ArrowUpRight :size="19" /></div>
    <strong>{{ title }}</strong>
    <p>{{ description }}</p>
  </div>
</template>
