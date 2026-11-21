<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { RouterLink } from 'vue-router'
import AsyncState from '@/components/AsyncState.vue'
import PageHeader from '@/components/PageHeader.vue'
import StatusBadge from '@/components/StatusBadge.vue'
import { useAuthStore } from '@/modules/auth/stores/authStore'
import { Permissions } from '@/modules/auth/types'
import { errorMessage } from '@/services/http'
import { branchService } from '../services/branchService'
import type { Branch } from '../types'

const auth = useAuthStore()
const canManage = auth.can(Permissions.BranchesManage)

const branches = ref<Branch[]>([])
const includeInactive = ref(false)
const loading = ref(false)
const error = ref<string | null>(null)

async function load(): Promise<void> {
  loading.value = true
  error.value = null
  try {
    branches.value = await branchService.list(includeInactive.value)
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
    <PageHeader title="Sucursales" subtitle="Catálogo de sucursales donde trabajan los usuarios.">
      <template #actions>
        <RouterLink v-if="canManage" :to="{ name: 'branch-create' }" class="btn btn-primary">Nueva sucursal</RouterLink>
      </template>
    </PageHeader>

    <div class="card">
      <label class="toggle">
        <input v-model="includeInactive" type="checkbox" @change="load" />
        Mostrar sucursales inactivas
      </label>

      <AsyncState :loading="loading" :error="error" :empty="branches.length === 0" @retry="load">
        <div class="table-wrapper">
          <table class="table">
            <thead>
              <tr>
                <th>Nombre</th>
                <th>Dirección</th>
                <th>Teléfono</th>
                <th>Usuarios activos</th>
                <th>Estado</th>
                <th />
              </tr>
            </thead>
            <tbody>
              <tr v-for="branch in branches" :key="branch.id">
                <td>{{ branch.name }}</td>
                <td class="cell-wrap">{{ branch.address }}</td>
                <td>{{ branch.phone ?? '—' }}</td>
                <td>{{ branch.activeUserCount }}</td>
                <td><StatusBadge :active="branch.isActive" /></td>
                <td>
                  <RouterLink :to="{ name: 'branch-edit', params: { id: branch.id } }">
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
