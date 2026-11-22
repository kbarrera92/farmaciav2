<script setup lang="ts">
import type { AuditInfo } from '@/types/api'
import { formatDateTime } from '@/utils/format'

/** Who and when created and last modified a record. */
defineProps<{ audit: AuditInfo }>()
</script>

<template>
  <p class="audit muted small">
    <span>Creado por {{ audit.createdBy ?? 'Sistema' }} · {{ formatDateTime(audit.createdAtUtc) }}</span>
    <span>Modificado por {{ audit.updatedBy ?? 'Sistema' }} · {{ formatDateTime(audit.updatedAtUtc) }}</span>
  </p>
</template>

<style scoped>
.audit {
  display: flex;
  flex-wrap: wrap;
  gap: 0.25rem 1.5rem;
  margin: 1rem 0 0;
}
</style>
