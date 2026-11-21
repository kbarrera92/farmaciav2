<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { RouterLink, useRouter } from 'vue-router'
import AsyncState from '@/components/AsyncState.vue'
import PageHeader from '@/components/PageHeader.vue'
import StatusBadge from '@/components/StatusBadge.vue'
import { useAuthStore } from '@/modules/auth/stores/authStore'
import { Permissions } from '@/modules/auth/types'
import { ApiError, errorMessage } from '@/services/http'
import { branchService } from '../services/branchService'
import type { Branch } from '../types'

const props = defineProps<{ id?: number }>()

const auth = useAuthStore()
const router = useRouter()

const isNew = computed(() => props.id === undefined)
const canManage = auth.can(Permissions.BranchesManage)

const branch = ref<Branch | null>(null)
const form = reactive({ name: '', address: '', phone: '' })

const loading = ref(false)
const loadError = ref<string | null>(null)
const saving = ref(false)
const error = ref<string | null>(null)
const success = ref<string | null>(null)
const fieldErrors = ref<Record<string, string | undefined>>({})

async function load(): Promise<void> {
  if (isNew.value) return
  loading.value = true
  loadError.value = null
  try {
    setBranch(await branchService.get(props.id!))
  } catch (e) {
    loadError.value = errorMessage(e)
  } finally {
    loading.value = false
  }
}

function setBranch(detail: Branch): void {
  branch.value = detail
  form.name = detail.name
  form.address = detail.address
  form.phone = detail.phone ?? ''
}

async function save(): Promise<void> {
  error.value = null
  success.value = null
  fieldErrors.value = {}
  saving.value = true
  try {
    const request = { name: form.name, address: form.address, phone: form.phone.trim() || null }
    if (isNew.value) {
      const created = await branchService.create(request)
      await router.replace({ name: 'branch-edit', params: { id: created.id } })
      return
    }
    setBranch(await branchService.update(props.id!, request))
    success.value = 'Cambios guardados.'
  } catch (e) {
    error.value = errorMessage(e)
    if (e instanceof ApiError && e.status === 400) {
      fieldErrors.value = {
        name: e.fieldError('name'),
        address: e.fieldError('address'),
        phone: e.fieldError('phone'),
      }
    }
  } finally {
    saving.value = false
  }
}

async function toggleStatus(): Promise<void> {
  if (!branch.value) return
  const activate = !branch.value.isActive
  if (!window.confirm(activate ? `¿Activar la sucursal ${branch.value.name}?` : `¿Desactivar la sucursal ${branch.value.name}?`)) return
  error.value = null
  success.value = null
  try {
    setBranch(await branchService.setStatus(branch.value.id, activate))
    success.value = activate ? 'Sucursal activada.' : 'Sucursal desactivada.'
  } catch (e) {
    error.value = errorMessage(e)
  }
}

onMounted(load)
</script>

<template>
  <section class="page">
    <PageHeader :title="isNew ? 'Nueva sucursal' : (branch?.name ?? 'Sucursal')">
      <template #actions>
        <RouterLink :to="{ name: 'branches' }" class="btn btn-secondary">Volver</RouterLink>
      </template>
    </PageHeader>

    <AsyncState :loading="loading" :error="loadError" @retry="load">
      <p v-if="success" class="alert alert-success" role="status">{{ success }}</p>
      <p v-if="error" class="alert alert-error" role="alert">{{ error }}</p>

      <form class="card" novalidate @submit.prevent="save">
        <fieldset :disabled="!canManage || saving" class="form-grid">
          <div class="field">
            <label for="name">Nombre</label>
            <input id="name" v-model="form.name" class="input" maxlength="100" :aria-invalid="!!fieldErrors.name" />
            <p v-if="fieldErrors.name" class="field-error">{{ fieldErrors.name }}</p>
          </div>
          <div class="field">
            <label for="address">Dirección</label>
            <input id="address" v-model="form.address" class="input" maxlength="150" :aria-invalid="!!fieldErrors.address" />
            <p v-if="fieldErrors.address" class="field-error">{{ fieldErrors.address }}</p>
          </div>
          <div class="field">
            <label for="phone">Teléfono</label>
            <input id="phone" v-model="form.phone" class="input" maxlength="50" :aria-invalid="!!fieldErrors.phone" />
            <p v-if="fieldErrors.phone" class="field-error">{{ fieldErrors.phone }}</p>
          </div>
        </fieldset>

        <div class="footer">
          <span v-if="branch" class="muted small">
            <StatusBadge :active="branch.isActive" /> · {{ branch.activeUserCount }} usuario(s) activo(s)
          </span>
          <div v-if="canManage" class="actions">
            <button v-if="branch" type="button" class="btn btn-secondary" @click="toggleStatus">
              {{ branch.isActive ? 'Desactivar' : 'Activar' }}
            </button>
            <button type="submit" class="btn btn-primary" :disabled="saving">
              {{ saving ? 'Guardando…' : isNew ? 'Crear sucursal' : 'Guardar cambios' }}
            </button>
          </div>
        </div>
      </form>
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

.footer {
  display: flex;
  flex-wrap: wrap;
  gap: 1rem;
  align-items: center;
  justify-content: space-between;
  margin-top: 1.25rem;
}

.footer .actions {
  margin-left: auto;
}
</style>
