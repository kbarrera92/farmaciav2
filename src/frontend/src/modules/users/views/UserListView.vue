<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { RouterLink } from 'vue-router'
import AsyncState from '@/components/AsyncState.vue'
import PageHeader from '@/components/PageHeader.vue'
import PaginationBar from '@/components/PaginationBar.vue'
import StatusBadge from '@/components/StatusBadge.vue'
import { useAuthStore } from '@/modules/auth/stores/authStore'
import { Permissions } from '@/modules/auth/types'
import { errorMessage } from '@/services/http'
import type { NamedReference, PagedResult } from '@/types/api'
import { formatDateTime } from '@/utils/format'
import { userService } from '../services/userService'
import type { UserListItem, UserQuery } from '../types'

const auth = useAuthStore()
const canManage = auth.can(Permissions.UsersManage)

const query = reactive<UserQuery>({ search: '', branchId: null, isActive: true, page: 1, pageSize: 20 })
const result = ref<PagedResult<UserListItem> | null>(null)
const branches = ref<NamedReference[]>([])
const loading = ref(false)
const error = ref<string | null>(null)

async function load(): Promise<void> {
  loading.value = true
  error.value = null
  try {
    result.value = await userService.list(query)
  } catch (e) {
    error.value = errorMessage(e)
  } finally {
    loading.value = false
  }
}

function applyFilters(): void {
  query.page = 1
  void load()
}

function changePage(page: number): void {
  query.page = page
  void load()
}

onMounted(async () => {
  void load()
  try {
    branches.value = await userService.branchOptions()
  } catch {
    branches.value = [] // the branch filter is optional
  }
})
</script>

<template>
  <section class="page">
    <PageHeader title="Usuarios" subtitle="Empleados con acceso al sistema.">
      <template #actions>
        <RouterLink v-if="canManage" :to="{ name: 'user-create' }" class="btn btn-primary">Nuevo usuario</RouterLink>
      </template>
    </PageHeader>

    <form class="card toolbar" @submit.prevent="applyFilters">
      <div class="field">
        <label for="search">Buscar</label>
        <input id="search" v-model="query.search" class="input" placeholder="Nombre o usuario" maxlength="100" />
      </div>
      <div class="field">
        <label for="branch">Sucursal</label>
        <select id="branch" v-model="query.branchId" class="select" @change="applyFilters">
          <option :value="null">Todas</option>
          <option v-for="b in branches" :key="b.id" :value="b.id">{{ b.name }}</option>
        </select>
      </div>
      <div class="field">
        <label for="status">Estado</label>
        <select id="status" v-model="query.isActive" class="select" @change="applyFilters">
          <option :value="null">Todos</option>
          <option :value="true">Activos</option>
          <option :value="false">Inactivos</option>
        </select>
      </div>
      <button type="submit" class="btn btn-secondary">Buscar</button>
    </form>

    <div class="card">
      <AsyncState
        :loading="loading"
        :error="error"
        :empty="result?.items.length === 0"
        empty-message="No se encontraron usuarios con esos filtros."
        @retry="load"
      >
        <template v-if="result">
          <div class="table-wrapper">
            <table class="table">
              <thead>
                <tr>
                  <th>Nombre</th>
                  <th>Usuario</th>
                  <th>Rol</th>
                  <th>Sucursal</th>
                  <th>Estado</th>
                  <th>Último ingreso</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                <tr v-for="user in result.items" :key="user.id">
                  <td>{{ user.fullName }}</td>
                  <td>{{ user.username }}</td>
                  <td>{{ user.role.name }}</td>
                  <td>{{ user.branch.name }}</td>
                  <td>
                    <StatusBadge :active="user.isActive" />
                    <span v-if="user.isLockedOut" class="badge badge-warning">Bloqueado</span>
                  </td>
                  <td>{{ formatDateTime(user.lastLoginAtUtc) }}</td>
                  <td>
                    <RouterLink :to="{ name: 'user-edit', params: { id: user.id } }">
                      {{ canManage ? 'Editar' : 'Ver' }}
                    </RouterLink>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
          <PaginationBar
            :page="result.page"
            :page-size="result.pageSize"
            :total-count="result.totalCount"
            @change="changePage"
          />
        </template>
      </AsyncState>
    </div>
  </section>
</template>

<style scoped>
.badge + .badge {
  margin-left: 0.35rem;
}
</style>
