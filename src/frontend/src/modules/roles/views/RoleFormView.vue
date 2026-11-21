<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { RouterLink, useRouter } from 'vue-router'
import AsyncState from '@/components/AsyncState.vue'
import PageHeader from '@/components/PageHeader.vue'
import StatusBadge from '@/components/StatusBadge.vue'
import { useAuthStore } from '@/modules/auth/stores/authStore'
import { Permissions } from '@/modules/auth/types'
import { ApiError, errorMessage } from '@/services/http'
import { roleService } from '../services/roleService'
import type { PermissionGroup, RoleDetail } from '../types'

const props = defineProps<{ id?: number }>()

const auth = useAuthStore()
const router = useRouter()

const isNew = computed(() => props.id === undefined)
const role = ref<RoleDetail | null>(null)
const catalog = ref<PermissionGroup[]>([])
const form = reactive({ name: '', description: '', permissions: new Set<string>() })

const readOnly = computed(() => !auth.can(Permissions.RolesManage) || (role.value?.isSystem ?? false))

const loading = ref(false)
const loadError = ref<string | null>(null)
const saving = ref(false)
const error = ref<string | null>(null)
const success = ref<string | null>(null)
const fieldErrors = ref<Record<string, string | undefined>>({})

async function load(): Promise<void> {
  loading.value = true
  loadError.value = null
  try {
    const [permissionCatalog, detail] = await Promise.all([
      roleService.permissionCatalog(),
      isNew.value ? Promise.resolve(null) : roleService.get(props.id!),
    ])
    catalog.value = permissionCatalog
    if (detail) setRole(detail)
  } catch (e) {
    loadError.value = errorMessage(e)
  } finally {
    loading.value = false
  }
}

function setRole(detail: RoleDetail): void {
  role.value = detail
  form.name = detail.name
  form.description = detail.description ?? ''
  form.permissions = new Set(detail.permissions)
}

function togglePermission(code: string): void {
  if (form.permissions.has(code)) form.permissions.delete(code)
  else form.permissions.add(code)
}

function groupSelected(group: PermissionGroup): boolean {
  return group.permissions.every((p) => form.permissions.has(p.code))
}

function toggleGroup(group: PermissionGroup): void {
  const select = !groupSelected(group)
  for (const p of group.permissions) {
    if (select) form.permissions.add(p.code)
    else form.permissions.delete(p.code)
  }
}

async function save(): Promise<void> {
  error.value = null
  success.value = null
  fieldErrors.value = {}
  saving.value = true
  try {
    const request = {
      name: form.name,
      description: form.description.trim() || null,
      permissions: [...form.permissions],
    }
    if (isNew.value) {
      const created = await roleService.create(request)
      await router.replace({ name: 'role-edit', params: { id: created.id } })
      return
    }
    setRole(await roleService.update(props.id!, request))
    success.value = 'Cambios guardados. Se aplican de inmediato a los usuarios con este rol.'
  } catch (e) {
    error.value = errorMessage(e)
    if (e instanceof ApiError && e.status === 400) {
      fieldErrors.value = { name: e.fieldError('name'), description: e.fieldError('description') }
    }
  } finally {
    saving.value = false
  }
}

async function toggleStatus(): Promise<void> {
  if (!role.value) return
  const activate = !role.value.isActive
  if (!window.confirm(activate ? `¿Activar el rol ${role.value.name}?` : `¿Desactivar el rol ${role.value.name}?`)) return
  error.value = null
  success.value = null
  try {
    setRole(await roleService.setStatus(role.value.id, activate))
    success.value = activate ? 'Rol activado.' : 'Rol desactivado.'
  } catch (e) {
    error.value = errorMessage(e)
  }
}

async function remove(): Promise<void> {
  if (!role.value || !window.confirm(`¿Eliminar el rol ${role.value.name}? Esta acción no se puede deshacer.`)) return
  error.value = null
  try {
    await roleService.remove(role.value.id)
    await router.replace({ name: 'roles' })
  } catch (e) {
    error.value = errorMessage(e)
  }
}

onMounted(load)
</script>

<template>
  <section class="page">
    <PageHeader :title="isNew ? 'Nuevo rol' : (role?.name ?? 'Rol')" subtitle="Seleccione los permisos que tendrá el rol.">
      <template #actions>
        <RouterLink :to="{ name: 'roles' }" class="btn btn-secondary">Volver</RouterLink>
      </template>
    </PageHeader>

    <AsyncState :loading="loading" :error="loadError" @retry="load">
      <p v-if="role?.isSystem" class="alert alert-info">
        Rol de sistema: tiene todos los permisos y no se puede modificar.
      </p>
      <p v-if="success" class="alert alert-success" role="status">{{ success }}</p>
      <p v-if="error" class="alert alert-error" role="alert">{{ error }}</p>

      <form class="page" novalidate @submit.prevent="save">
        <fieldset :disabled="readOnly || saving" class="card">
          <h2 class="card-title">Datos del rol</h2>
          <div class="form-grid">
            <div class="field">
              <label for="name">Nombre</label>
              <input id="name" v-model="form.name" class="input" maxlength="50" :aria-invalid="!!fieldErrors.name" />
              <p v-if="fieldErrors.name" class="field-error">{{ fieldErrors.name }}</p>
            </div>
            <div class="field">
              <label for="description">Descripción</label>
              <input
                id="description"
                v-model="form.description"
                class="input"
                maxlength="200"
                :aria-invalid="!!fieldErrors.description"
              />
              <p v-if="fieldErrors.description" class="field-error">{{ fieldErrors.description }}</p>
            </div>
          </div>
        </fieldset>

        <fieldset :disabled="readOnly || saving" class="card">
          <h2 class="card-title">Permisos</h2>
          <div class="groups">
            <section v-for="group in catalog" :key="group.module" class="group">
              <header class="group-header">
                <h3>{{ group.module }}</h3>
                <button v-if="!readOnly" type="button" class="btn-link small" @click="toggleGroup(group)">
                  {{ groupSelected(group) ? 'Quitar todos' : 'Seleccionar todos' }}
                </button>
              </header>
              <label v-for="p in group.permissions" :key="p.code" class="permission">
                <input
                  type="checkbox"
                  :checked="role?.isSystem || form.permissions.has(p.code)"
                  @change="togglePermission(p.code)"
                />
                <span>
                  <strong>{{ p.name }}</strong>
                  <span v-if="p.description" class="muted small">{{ p.description }}</span>
                </span>
              </label>
            </section>
          </div>
        </fieldset>

        <div v-if="!readOnly" class="actions">
          <button v-if="role" type="button" class="btn btn-danger" @click="remove">Eliminar</button>
          <button v-if="role" type="button" class="btn btn-secondary" @click="toggleStatus">
            {{ role.isActive ? 'Desactivar' : 'Activar' }}
          </button>
          <button type="submit" class="btn btn-primary" :disabled="saving">
            {{ saving ? 'Guardando…' : isNew ? 'Crear rol' : 'Guardar cambios' }}
          </button>
        </div>
        <p v-if="role" class="muted small">Estado: <StatusBadge :active="role.isActive" /></p>
      </form>
    </AsyncState>
  </section>
</template>

<style scoped>
fieldset {
  margin: 0;
  min-width: 0;
}

.groups {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(260px, 1fr));
  gap: 1.25rem;
}

.group {
  border: 1px solid var(--color-border);
  border-radius: var(--radius);
  padding: 0.9rem 1rem;
}

.group-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 0.5rem;
}

.group-header h3 {
  font-size: 0.95rem;
}

.permission {
  display: flex;
  gap: 0.6rem;
  align-items: flex-start;
  padding: 0.45rem 0;
  cursor: pointer;
}

.permission input {
  margin-top: 0.25rem;
  accent-color: var(--color-primary);
}

.permission > span {
  display: flex;
  flex-direction: column;
}

.permission strong {
  font-weight: 600;
  font-size: 0.9rem;
}
</style>
