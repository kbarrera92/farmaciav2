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
import { paymentMethodService } from '../services/paymentMethodService'
import type { PaymentMethod } from '../types'

const props = defineProps<{ id?: number }>()

const auth = useAuthStore()
const router = useRouter()

const isNew = computed(() => props.id === undefined)
const canManage = auth.can(Permissions.SettingsManage)

const paymentMethod = ref<PaymentMethod | null>(null)
const form = reactive({ name: '' })

const loading = ref(false)
const loadError = ref<string | null>(null)
const saving = ref(false)
const error = ref<string | null>(null)
const success = ref<string | null>(null)
const nameError = ref<string | undefined>()

async function load(): Promise<void> {
  if (isNew.value) return
  loading.value = true
  loadError.value = null
  try {
    setPaymentMethod(await paymentMethodService.get(props.id!))
  } catch (e) {
    loadError.value = errorMessage(e)
  } finally {
    loading.value = false
  }
}

function setPaymentMethod(detail: PaymentMethod): void {
  paymentMethod.value = detail
  form.name = detail.name
}

async function save(): Promise<void> {
  error.value = null
  success.value = null
  nameError.value = undefined
  saving.value = true
  try {
    const request = { name: form.name }
    if (isNew.value) {
      const created = await paymentMethodService.create(request)
      await router.replace({ name: 'payment-method-edit', params: { id: created.id } })
      return
    }
    setPaymentMethod(await paymentMethodService.update(props.id!, request))
    success.value = 'Cambios guardados.'
  } catch (e) {
    error.value = errorMessage(e)
    if (e instanceof ApiError && e.status === 400) nameError.value = e.fieldError('name')
  } finally {
    saving.value = false
  }
}

async function toggleStatus(): Promise<void> {
  if (!paymentMethod.value) return
  const activate = !paymentMethod.value.isActive
  const name = paymentMethod.value.name
  if (!window.confirm(activate ? `¿Activar la forma de pago ${name}?` : `¿Desactivar la forma de pago ${name}?`)) return
  error.value = null
  success.value = null
  try {
    setPaymentMethod(await paymentMethodService.setStatus(paymentMethod.value.id, activate))
    success.value = activate ? 'Forma de pago activada.' : 'Forma de pago desactivada.'
  } catch (e) {
    error.value = errorMessage(e)
  }
}

onMounted(load)
</script>

<template>
  <section class="page">
    <PageHeader :title="isNew ? 'Nueva forma de pago' : (paymentMethod?.name ?? 'Forma de pago')">
      <template #actions>
        <RouterLink :to="{ name: 'payment-methods' }" class="btn btn-secondary">Volver</RouterLink>
      </template>
    </PageHeader>

    <AsyncState :loading="loading" :error="loadError" @retry="load">
      <p v-if="success" class="alert alert-success" role="status">{{ success }}</p>
      <p v-if="error" class="alert alert-error" role="alert">{{ error }}</p>

      <form class="card" novalidate @submit.prevent="save">
        <fieldset :disabled="!canManage || saving" class="form-grid">
          <div class="field">
            <label for="name">Nombre</label>
            <input id="name" v-model="form.name" class="input" maxlength="50" :aria-invalid="!!nameError" />
            <p v-if="nameError" class="field-error">{{ nameError }}</p>
          </div>
        </fieldset>

        <div class="footer">
          <span v-if="paymentMethod" class="muted small"><StatusBadge :active="paymentMethod.isActive" /></span>
          <div v-if="canManage" class="actions">
            <button v-if="paymentMethod" type="button" class="btn btn-secondary" @click="toggleStatus">
              {{ paymentMethod.isActive ? 'Desactivar' : 'Activar' }}
            </button>
            <button type="submit" class="btn btn-primary" :disabled="saving">
              {{ saving ? 'Guardando…' : isNew ? 'Crear forma de pago' : 'Guardar cambios' }}
            </button>
          </div>
        </div>
        <AuditInfo v-if="paymentMethod" :audit="paymentMethod.audit" />
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
