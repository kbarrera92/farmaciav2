<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { RouterLink } from 'vue-router'
import AsyncState from '@/components/AsyncState.vue'
import PageHeader from '@/components/PageHeader.vue'
import StatusBadge from '@/components/StatusBadge.vue'
import { useAuthStore } from '@/modules/auth/stores/authStore'
import { Permissions } from '@/modules/auth/types'
import { errorMessage } from '@/services/http'
import { roleService } from '../services/roleService'
import type { RoleListItem } from '../types'

const auth = useAuthStore()
const canManage = auth.can(Permissions.RolesManage)

const roles = ref<RoleListItem[]>([])
const loading = ref(false)
const error = ref<string | null>(null)

async function load(): Promise<void> {
  loading.value = true
  error.value = null
  try {
    roles.value = await roleService.list()
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
    <PageHeader title="Roles y permisos" subtitle="Cada rol define qué módulos y acciones puede usar un usuario.">
      <template #actions>
        <RouterLink v-if="canManage" :to="{ name: 'role-create' }" class="btn btn-primary">Nuevo rol</RouterLink>
      </template>
    </PageHeader>

    <div class="card">
      <AsyncState :loading="loading" :error="error" :empty="roles.length === 0" @retry="load">
        <div class="table-wrapper">
          <table class="table">
            <thead>
              <tr>
                <th>Rol</th>
                <th>Descripción</th>
                <th>Permisos</th>
                <th>Usuarios</th>
                <th>Estado</th>
                <th />
              </tr>
            </thead>
            <tbody>
              <tr v-for="role in roles" :key="role.id">
                <td>
                  {{ role.name }}
                  <span v-if="role.isSystem" class="badge badge-info">Sistema</span>
                </td>
                <td class="cell-wrap muted">{{ role.description ?? '—' }}</td>
                <td>{{ role.isSystem ? 'Todos' : role.permissionCount }}</td>
                <td>{{ role.userCount }}</td>
                <td><StatusBadge :active="role.isActive" /></td>
                <td>
                  <RouterLink :to="{ name: 'role-edit', params: { id: role.id } }">
                    {{ canManage && !role.isSystem ? 'Editar' : 'Ver' }}
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
.badge {
  margin-left: 0.4rem;
}
</style>
