<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { RouterLink } from 'vue-router'
import AsyncState from '@/components/AsyncState.vue'
import PageHeader from '@/components/PageHeader.vue'
import StatusBadge from '@/components/StatusBadge.vue'
import { useAuthStore } from '@/modules/auth/stores/authStore'
import { Permissions } from '@/modules/auth/types'
import { errorMessage } from '@/services/http'
import { formatDateTime } from '@/utils/format'
import { categoryService } from '../services/categoryService'
import type { Category } from '../types'

const auth = useAuthStore()
const canManage = auth.can(Permissions.CatalogManage)

const categories = ref<Category[]>([])
const includeInactive = ref(false)
const loading = ref(false)
const error = ref<string | null>(null)

async function load(): Promise<void> {
  loading.value = true
  error.value = null
  try {
    categories.value = await categoryService.list(includeInactive.value)
  } catch (e) {
    error.value = errorMessage(e)
  } finally {
    loading.value = false
  }
}

onMounted(load)
</script>

<template>
  <section class="page">
    <PageHeader title="Categorías" subtitle="Categorías para clasificar los productos del catálogo.">
      <template #actions>
        <RouterLink v-if="canManage" :to="{ name: 'category-create' }" class="btn btn-primary">
          Nueva categoría
        </RouterLink>
      </template>
    </PageHeader>

    <div class="card">
      <label class="toggle">
        <input v-model="includeInactive" type="checkbox" @change="load" />
        Mostrar categorías inactivas
      </label>

      <AsyncState
        :loading="loading"
        :error="error"
        :empty="categories.length === 0"
        empty-message="No hay categorías registradas."
        @retry="load"
      >
        <div class="table-wrapper">
          <table class="table">
            <thead>
              <tr>
                <th>Nombre</th>
                <th>Estado</th>
                <th>Última modificación</th>
                <th />
              </tr>
            </thead>
            <tbody>
              <tr v-for="category in categories" :key="category.id">
                <td>{{ category.name }}</td>
                <td><StatusBadge :active="category.isActive" /></td>
                <td class="muted small">
                  {{ formatDateTime(category.audit.updatedAtUtc) }} · {{ category.audit.updatedBy ?? 'Sistema' }}
                </td>
                <td>
                  <RouterLink :to="{ name: 'category-edit', params: { id: category.id } }">
                    {{ canManage ? 'Editar' : 'Ver' }}
                  </RouterLink>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </AsyncState>
    </div>
  </section>
</template>

<style scoped>
.toggle {
  display: inline-flex;
  gap: 0.5rem;
  align-items: center;
  margin-bottom: 1rem;
  font-size: 0.9rem;
  cursor: pointer;
}

.toggle input {
  accent-color: var(--color-primary);
}
</style>
