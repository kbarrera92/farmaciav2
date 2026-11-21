<script setup lang="ts">
import { computed } from 'vue'

const props = defineProps<{
  page: number
  pageSize: number
  totalCount: number
}>()

const emit = defineEmits<{ change: [page: number] }>()

const totalPages = computed(() => Math.max(1, Math.ceil(props.totalCount / props.pageSize)))
const firstItem = computed(() => (props.totalCount === 0 ? 0 : (props.page - 1) * props.pageSize + 1))
const lastItem = computed(() => Math.min(props.page * props.pageSize, props.totalCount))
</script>

<template>
  <nav class="pagination" aria-label="Paginación">
    <span class="muted small">{{ firstItem }}–{{ lastItem }} de {{ totalCount }}</span>
    <div class="actions">
      <button type="button" class="btn btn-secondary" :disabled="page <= 1" @click="emit('change', page - 1)">
        Anterior
      </button>
      <span class="small">Página {{ page }} de {{ totalPages }}</span>
      <button
        type="button"
        class="btn btn-secondary"
        :disabled="page >= totalPages"
        @click="emit('change', page + 1)"
      >
        Siguiente
      </button>
    </div>
  </nav>
</template>

<style scoped>
.pagination {
  display: flex;
  flex-wrap: wrap;
  gap: 0.75rem;
  align-items: center;
  justify-content: space-between;
  margin-top: 1rem;
}

.actions {
  align-items: center;
}
</style>
