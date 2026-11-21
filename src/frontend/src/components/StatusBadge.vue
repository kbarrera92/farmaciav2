<script setup lang="ts">
defineProps<{ active: boolean }>()
</script>

<template>
  <span class="badge" :class="active ? 'badge-success' : 'badge-muted'">
    {{ active ? 'Activo' : 'Inactivo' }}
  </span>
</template>
