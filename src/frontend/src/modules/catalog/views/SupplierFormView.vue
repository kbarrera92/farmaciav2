<script setup lang="ts">
import { computed, nextTick, onMounted, reactive, ref } from 'vue'
import { RouterLink, useRoute, useRouter } from 'vue-router'
import AsyncState from '@/components/AsyncState.vue'
import AuditInfo from '@/components/AuditInfo.vue'
import PageHeader from '@/components/PageHeader.vue'
import StatusBadge from '@/components/StatusBadge.vue'
import { useAuthStore } from '@/modules/auth/stores/authStore'
import { Permissions } from '@/modules/auth/types'
import { ApiError, errorMessage } from '@/services/http'
import { supplierService } from '../services/supplierService'
import type { SaveSupplierRequest, Supplier } from '../types'

const props = defineProps<{ id?: number }>()

const auth = useAuthStore()
const router = useRouter()
const route = useRoute()

const isNew = computed(() => props.id === undefined)
const canManage = auth.can(Permissions.CatalogManage)

const supplier = ref<Supplier | null>(null)
const form = reactive({
  name: '',
  taxId: '',
  address: '',
  phone: '',
  email: '',
  contactName: '',
  contactPhone: '',
})

const loading = ref(false)
const loadError = ref<string | null>(null)
const saving = ref(false)
const error = ref<string | null>(null)
const success = ref<string | null>(null)
const messages = ref<HTMLElement | null>(null)

/** Messages render at the top of the form: bring them into view. */
async function revealMessages(): Promise<void> {
  await nextTick()
  messages.value?.scrollIntoView({ behavior: 'smooth', block: 'start' })
}
const fieldErrors = ref<Record<string, string | undefined>>({})

async function load(): Promise<void> {
  if (isNew.value) return
  loading.value = true
  loadError.value = null
  try {
    setSupplier(await supplierService.get(props.id!))
  } catch (e) {
    loadError.value = errorMessage(e)
  } finally {
    loading.value = false
  }
  if (!isNew.value && route.query.created === '1') {
    success.value = 'Proveedor creado.'
    await router.replace({ query: {} })
    await revealMessages()
  }
}

function setSupplier(detail: Supplier): void {
  supplier.value = detail
  form.name = detail.name
  form.taxId = detail.taxId ?? ''
  form.address = detail.address ?? ''
  form.phone = detail.phone ?? ''
  form.email = detail.email ?? ''
  form.contactName = detail.contactName ?? ''
  form.contactPhone = detail.contactPhone ?? ''
}

function buildRequest(): SaveSupplierRequest {
  return {
    name: form.name,
    taxId: form.taxId || null,
    address: form.address || null,
    phone: form.phone || null,
    email: form.email || null,
    contactName: form.contactName || null,
    contactPhone: form.contactPhone || null,
  }
}

async function save(): Promise<void> {
  error.value = null
  success.value = null
  fieldErrors.value = {}
  saving.value = true
  try {
    const request = buildRequest()
    if (isNew.value) {
      const created = await supplierService.create(request)
      await router.replace({ name: 'supplier-edit', params: { id: created.id }, query: { created: '1' } })
      return
    }
    setSupplier(await supplierService.update(props.id!, request))
    success.value = 'Cambios guardados.'
  } catch (e) {
    error.value = errorMessage(e)
    if (e instanceof ApiError && e.status === 400) {
      fieldErrors.value = {
        name: e.fieldError('name'),
        taxId: e.fieldError('taxId'),
        address: e.fieldError('address'),
        phone: e.fieldError('phone'),
        email: e.fieldError('email'),
        contactName: e.fieldError('contactName'),
        contactPhone: e.fieldError('contactPhone'),
      }
    }
  } finally {
    saving.value = false
  }
  await revealMessages()
}

async function toggleStatus(): Promise<void> {
  if (!supplier.value) return
  const activate = !supplier.value.isActive
  const name = supplier.value.name
  if (!window.confirm(activate ? `¿Activar el proveedor ${name}?` : `¿Desactivar el proveedor ${name}?`)) return
  error.value = null
  success.value = null
  try {
    setSupplier(await supplierService.setStatus(supplier.value.id, activate))
    success.value = activate ? 'Proveedor activado.' : 'Proveedor desactivado.'
  } catch (e) {
    error.value = errorMessage(e)
  }
  await revealMessages()
}

onMounted(load)
</script>

<template>
  <section class="page">
    <PageHeader :title="isNew ? 'Nuevo proveedor' : (supplier?.name ?? 'Proveedor')">
      <template #actions>
        <RouterLink :to="{ name: 'suppliers' }" class="btn btn-secondary">Volver</RouterLink>
      </template>
    </PageHeader>

    <AsyncState :loading="loading" :error="loadError" @retry="load">
      <div ref="messages">
        <p v-if="success" class="alert alert-success" role="status">{{ success }}</p>
        <p v-if="error" class="alert alert-error" role="alert">{{ error }}</p>
      </div>

      <form class="card" novalidate @submit.prevent="save">
        <fieldset :disabled="!canManage || saving" class="form-grid">
          <div class="field">
            <label for="name">Nombre o razón social</label>
            <input id="name" v-model="form.name" class="input" maxlength="100" :aria-invalid="!!fieldErrors.name" />
            <p v-if="fieldErrors.name" class="field-error">{{ fieldErrors.name }}</p>
          </div>
          <div class="field">
            <label for="taxId">NIT</label>
            <input id="taxId" v-model="form.taxId" class="input" maxlength="50" :aria-invalid="!!fieldErrors.taxId" />
            <p v-if="fieldErrors.taxId" class="field-error">{{ fieldErrors.taxId }}</p>
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
          <div class="field">
            <label for="email">Correo</label>
            <input id="email" v-model="form.email" type="email" class="input" maxlength="75" :aria-invalid="!!fieldErrors.email" />
            <p v-if="fieldErrors.email" class="field-error">{{ fieldErrors.email }}</p>
          </div>
          <div class="field">
            <label for="contactName">Contacto</label>
            <input id="contactName" v-model="form.contactName" class="input" maxlength="100" :aria-invalid="!!fieldErrors.contactName" />
            <p v-if="fieldErrors.contactName" class="field-error">{{ fieldErrors.contactName }}</p>
          </div>
          <div class="field">
            <label for="contactPhone">Teléfono del contacto</label>
            <input id="contactPhone" v-model="form.contactPhone" class="input" maxlength="50" :aria-invalid="!!fieldErrors.contactPhone" />
            <p v-if="fieldErrors.contactPhone" class="field-error">{{ fieldErrors.contactPhone }}</p>
          </div>
        </fieldset>

        <div class="footer">
          <span v-if="supplier" class="muted small"><StatusBadge :active="supplier.isActive" /></span>
          <div v-if="canManage" class="actions">
            <button v-if="supplier" type="button" class="btn btn-secondary" @click="toggleStatus">
              {{ supplier.isActive ? 'Desactivar' : 'Activar' }}
            </button>
            <button type="submit" class="btn btn-primary" :disabled="saving">
              {{ saving ? 'Guardando…' : isNew ? 'Crear proveedor' : 'Guardar cambios' }}
            </button>
          </div>
        </div>
        <AuditInfo v-if="supplier" :audit="supplier.audit" />
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
