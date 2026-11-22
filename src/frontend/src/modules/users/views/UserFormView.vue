<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { RouterLink, useRouter } from 'vue-router'
import AsyncState from '@/components/AsyncState.vue'
import AuditInfo from '@/components/AuditInfo.vue'
import PageHeader from '@/components/PageHeader.vue'
import StatusBadge from '@/components/StatusBadge.vue'
import { useAuthStore } from '@/modules/auth/stores/authStore'
import { Permissions } from '@/modules/auth/types'
import { ApiError, errorMessage } from '@/services/http'
import type { NamedReference } from '@/types/api'
import { formatDateTime } from '@/utils/format'
import { userService } from '../services/userService'
import type { UserDetail } from '../types'

const props = defineProps<{ id?: number }>()

const auth = useAuthStore()
const router = useRouter()

const isNew = computed(() => props.id === undefined)
const canManage = auth.can(Permissions.UsersManage)
const isSelf = computed(() => props.id === auth.user?.id)

const form = reactive({
  fullName: '',
  username: '',
  password: '',
  roleId: null as number | null,
  branchId: null as number | null,
})
const user = ref<UserDetail | null>(null)
const roles = ref<NamedReference[]>([])
const branches = ref<NamedReference[]>([])

const loading = ref(false)
const loadError = ref<string | null>(null)
const saving = ref(false)
const error = ref<string | null>(null)
const success = ref<string | null>(null)
const fieldErrors = ref<Record<string, string | undefined>>({})

const resetPassword = ref('')
const resetError = ref<string | null>(null)
const resetting = ref(false)

const title = computed(() => (isNew.value ? 'Nuevo usuario' : (user.value?.fullName ?? 'Usuario')))

async function load(): Promise<void> {
  loading.value = true
  loadError.value = null
  try {
    const [detail, roleOptions, branchOptions] = await Promise.all([
      isNew.value ? Promise.resolve(null) : userService.get(props.id!),
      canManage ? userService.roleOptions() : Promise.resolve([]),
      userService.branchOptions(),
    ])
    roles.value = roleOptions
    branches.value = branchOptions
    if (detail) setUser(detail)
  } catch (e) {
    loadError.value = errorMessage(e)
  } finally {
    loading.value = false
  }
}

function setUser(detail: UserDetail): void {
  user.value = detail
  form.fullName = detail.fullName
  form.username = detail.username
  form.roleId = detail.role.id
  form.branchId = detail.branch.id
  // Keep the current role/branch selectable even if it is no longer active.
  if (!roles.value.some((r) => r.id === detail.role.id)) roles.value.push(detail.role)
  if (!branches.value.some((b) => b.id === detail.branch.id)) branches.value.push(detail.branch)
}

function applyApiError(e: unknown): void {
  error.value = errorMessage(e)
  if (e instanceof ApiError && e.status === 400) {
    fieldErrors.value = {
      fullName: e.fieldError('fullName'),
      username: e.fieldError('username'),
      password: e.fieldError('password'),
      roleId: e.fieldError('roleId'),
      branchId: e.fieldError('branchId'),
    }
  }
}

async function save(): Promise<void> {
  error.value = null
  success.value = null
  fieldErrors.value = {}
  saving.value = true
  try {
    const request = { fullName: form.fullName, username: form.username, roleId: form.roleId, branchId: form.branchId }
    if (isNew.value) {
      const created = await userService.create({ ...request, password: form.password })
      await router.replace({ name: 'user-edit', params: { id: created.id } })
      return
    }
    setUser(await userService.update(props.id!, request))
    success.value = 'Cambios guardados.'
  } catch (e) {
    applyApiError(e)
  } finally {
    saving.value = false
  }
}

async function toggleStatus(): Promise<void> {
  if (!user.value) return
  const activate = !user.value.isActive
  const message = activate
    ? `¿Activar al usuario ${user.value.username}?`
    : `¿Desactivar al usuario ${user.value.username}? Su sesión se cerrará y no podrá ingresar.`
  if (!window.confirm(message)) return

  error.value = null
  success.value = null
  try {
    setUser(await userService.setStatus(user.value.id, activate))
    success.value = activate ? 'Usuario activado.' : 'Usuario desactivado.'
  } catch (e) {
    error.value = errorMessage(e)
  }
}

async function submitReset(): Promise<void> {
  if (!user.value) return
  resetError.value = null
  success.value = null
  resetting.value = true
  try {
    await userService.resetPassword(user.value.id, resetPassword.value)
    resetPassword.value = ''
    setUser(await userService.get(user.value.id))
    success.value = 'Contraseña restablecida. El usuario deberá cambiarla al ingresar.'
  } catch (e) {
    resetError.value = e instanceof ApiError ? (e.fieldError('newPassword') ?? e.message) : errorMessage(e)
  } finally {
    resetting.value = false
  }
}

onMounted(load)
</script>

<template>
  <section class="page">
    <PageHeader :title="title" :subtitle="isNew ? 'Registrar un nuevo empleado.' : `Usuario ${user?.username ?? ''}`">
      <template #actions>
        <RouterLink :to="{ name: 'users' }" class="btn btn-secondary">Volver</RouterLink>
      </template>
    </PageHeader>

    <AsyncState :loading="loading" :error="loadError" @retry="load">
      <p v-if="success" class="alert alert-success" role="status">{{ success }}</p>
      <p v-if="error" class="alert alert-error" role="alert">{{ error }}</p>

      <form class="card" novalidate @submit.prevent="save">
        <h2 class="card-title">Datos del usuario</h2>
        <fieldset :disabled="!canManage || saving" class="form-grid">
          <div class="field">
            <label for="fullName">Nombre completo</label>
            <input id="fullName" v-model="form.fullName" class="input" maxlength="100" :aria-invalid="!!fieldErrors.fullName" />
            <p v-if="fieldErrors.fullName" class="field-error">{{ fieldErrors.fullName }}</p>
          </div>

          <div class="field">
            <label for="username">Usuario</label>
            <input
              id="username"
              v-model="form.username"
              class="input"
              maxlength="50"
              autocomplete="off"
              :aria-invalid="!!fieldErrors.username"
            />
            <p v-if="fieldErrors.username" class="field-error">{{ fieldErrors.username }}</p>
            <p v-else class="field-hint">Letras, números, punto, guion o guion bajo.</p>
          </div>

          <div class="field">
            <label for="role">Rol</label>
            <select id="role" v-model="form.roleId" class="select" :aria-invalid="!!fieldErrors.roleId">
              <option :value="null" disabled>Seleccione…</option>
              <option v-for="r in roles" :key="r.id" :value="r.id">{{ r.name }}</option>
            </select>
            <p v-if="fieldErrors.roleId" class="field-error">{{ fieldErrors.roleId }}</p>
          </div>

          <div class="field">
            <label for="branch">Sucursal</label>
            <select id="branch" v-model="form.branchId" class="select" :aria-invalid="!!fieldErrors.branchId">
              <option :value="null" disabled>Seleccione…</option>
              <option v-for="b in branches" :key="b.id" :value="b.id">{{ b.name }}</option>
            </select>
            <p v-if="fieldErrors.branchId" class="field-error">{{ fieldErrors.branchId }}</p>
          </div>

          <div v-if="isNew" class="field">
            <label for="password">Contraseña temporal</label>
            <input
              id="password"
              v-model="form.password"
              class="input"
              type="password"
              autocomplete="new-password"
              :aria-invalid="!!fieldErrors.password"
            />
            <p v-if="fieldErrors.password" class="field-error">{{ fieldErrors.password }}</p>
            <p v-else class="field-hint">El usuario deberá cambiarla en su primer ingreso.</p>
          </div>
        </fieldset>

        <div v-if="canManage" class="actions form-actions">
          <button type="submit" class="btn btn-primary" :disabled="saving">
            {{ saving ? 'Guardando…' : isNew ? 'Crear usuario' : 'Guardar cambios' }}
          </button>
        </div>
      </form>

      <template v-if="user">
        <div class="card">
          <h2 class="card-title">Estado y acceso</h2>
          <dl class="details">
            <div><dt>Estado</dt><dd><StatusBadge :active="user.isActive" /></dd></div>
            <div v-if="user.isLockedOut"><dt>Bloqueo</dt><dd><span class="badge badge-warning">Bloqueado temporalmente</span></dd></div>
            <div><dt>Cambio de contraseña pendiente</dt><dd>{{ user.mustChangePassword ? 'Sí' : 'No' }}</dd></div>
            <div><dt>Último ingreso</dt><dd>{{ formatDateTime(user.lastLoginAtUtc) }}</dd></div>
          </dl>
          <AuditInfo :audit="user.audit" />
          <div v-if="canManage && !isSelf" class="actions">
            <button type="button" :class="user.isActive ? 'btn btn-danger' : 'btn btn-secondary'" @click="toggleStatus">
              {{ user.isActive ? 'Desactivar usuario' : 'Activar usuario' }}
            </button>
          </div>
        </div>

        <form v-if="canManage" class="card" novalidate @submit.prevent="submitReset">
          <h2 class="card-title">Restablecer contraseña</h2>
          <p class="muted small">
            Asigna una contraseña temporal, desbloquea la cuenta y cierra las sesiones abiertas del usuario.
          </p>
          <div class="reset-row">
            <div class="field">
              <label for="resetPassword">Contraseña temporal</label>
              <input
                id="resetPassword"
                v-model="resetPassword"
                class="input"
                type="password"
                autocomplete="new-password"
                :aria-invalid="!!resetError"
              />
              <p v-if="resetError" class="field-error">{{ resetError }}</p>
            </div>
            <button type="submit" class="btn btn-secondary" :disabled="resetting || !resetPassword">
              {{ resetting ? 'Restableciendo…' : 'Restablecer' }}
            </button>
          </div>
        </form>
      </template>
    </AsyncState>
  </section>
</template>

<style scoped>
fieldset {
  border: none;
  margin: 0;
  padding: 0;
  min-width: 0;
}

.form-actions {
  margin-top: 1.25rem;
}

.details {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 0.75rem 1rem;
  margin: 0 0 1rem;
}

.details dt {
  font-size: 0.8rem;
  color: var(--color-text-muted);
}

.details dd {
  margin: 0.15rem 0 0;
}

.reset-row {
  display: flex;
  flex-wrap: wrap;
  gap: 0.75rem;
  align-items: flex-start;
  margin-top: 0.75rem;
}

.reset-row .field {
  flex: 1 1 240px;
}

.reset-row .btn {
  margin-top: 1.6rem;
}
</style>
