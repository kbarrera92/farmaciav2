<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { RouterLink } from 'vue-router'
import AsyncState from '@/components/AsyncState.vue'
import PageHeader from '@/components/PageHeader.vue'
import StatusBadge from '@/components/StatusBadge.vue'
import { useAuthStore } from '@/modules/auth/stores/authStore'
import { Permissions } from '@/modules/auth/types'
import { errorMessage } from '@/services/http'
import { supplierService } from '../services/supplierService'
import type { Supplier } from '../types'

const auth = useAuthStore()
const canManage = auth.can(Permissions.CatalogManage)

const suppliers = ref<Supplier[]>([])
const includeInactive = ref(false)
const loading = ref(false)
const error = ref<string | null>(null)

async function load(): Promise<void> {
  loading.value = true
  error.value = null
  try {
    suppliers.value = await supplierService.list(includeInactive.value)
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
    <PageHeader title="Proveedores" subtitle="Proveedores de los productos del catálogo.">
      <template #actions>
        <RouterLink v-if="canManage" :to="{ name: 'supplier-create' }" class="btn btn-primary">
          Nuevo proveedor
        </RouterLink>
      </template>
    </PageHeader>

    <div class="card">
      <label class="toggle">
        <input v-model="includeInactive" type="checkbox" @change="load" />
        Mostrar proveedores inactivos
      </label>

      <AsyncState
        :loading="loading"
        :error="error"
        :empty="suppliers.length === 0"
        empty-message="No hay proveedores registrados."
        @retry="load"
      >
        <div class="table-wrapper">
          <table class="table">
            <thead>
              <tr>
                <th>Nombre</th>
                <th>NIT</th>
                <th>Teléfono</th>
                <th>Contacto</th>
                <th>Estado</th>
                <th />
              </tr>
            </thead>
            <tbody>
              <tr v-for="supplier in suppliers" :key="supplier.id">
                <td>{{ supplier.name }}</td>
                <td>{{ supplier.taxId ?? '—' }}</td>
                <td>{{ supplier.phone ?? '—' }}</td>
                <td>{{ supplier.contactName ?? '—' }}</td>
                <td><StatusBadge :active="supplier.isActive" /></td>
                <td>
                  <RouterLink :to="{ name: 'supplier-edit', params: { id: supplier.id } }">
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
