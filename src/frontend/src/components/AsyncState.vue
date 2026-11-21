<script setup lang="ts">
/** Shows loading, error or empty states; renders the default slot when there is data. */
defineProps<{
  loading: boolean
  error?: string | null
  empty?: boolean
  emptyMessage?: string
}>()

defineEmits<{ retry: [] }>()
</script>

<template>
  <div v-if="loading" class="state" role="status">
    <span class="spinner" aria-hidden="true" />
    <span class="muted">Cargando…</span>
  </div>
  <div v-else-if="error" class="state">
    <p class="alert alert-error" role="alert">{{ error }}</p>
    <button type="button" class="btn btn-secondary" @click="$emit('retry')">Reintentar</button>
  </div>
  <div v-else-if="empty" class="state">
    <p class="muted">{{ emptyMessage ?? 'No hay registros para mostrar.' }}</p>
  </div>
  <slot v-else />
</template>

<style scoped>
.state {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.75rem;
  padding: 2.5rem 1rem;
  text-align: center;
}

.state p {
  margin: 0;
}

.spinner {
  width: 28px;
  height: 28px;
  border: 3px solid var(--color-primary-soft);
  border-top-color: var(--color-primary);
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}
</style>
