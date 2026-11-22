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
import { paymentMethodService } from '../services/paymentMethodService'
import type { PaymentMethod } from '../types'

const auth = useAuthStore()
const canManage = auth.can(Permissions.SettingsManage)

const paymentMethods = ref<PaymentMethod[]>([])
const includeInactive = ref(false)
const loading = ref(false)
const error = ref<string | null>(null)

async function load(): Promise<void> {
  loading.value = true
  error.value = null
  try {
    paymentMethods.value = await paymentMethodService.list(includeInactive.value)
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
    <PageHeader title="Formas de pago" subtitle="Formas de pago disponibles para registrar compras.">
      <template #actions>
        <RouterLink v-if="canManage" :to="{ name: 'payment-method-create' }" class="btn btn-primary">
          Nueva forma de pago
        </RouterLink>
      </template>
    </PageHeader>

    <div class="card">
      <label class="toggle">
        <input v-model="includeInactive" type="checkbox" @change="load" />
        Mostrar formas de pago inactivas
      </label>

      <AsyncState
        :loading="loading"
        :error="error"
        :empty="paymentMethods.length === 0"
        empty-message="No hay formas de pago registradas."
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
              <tr v-for="paymentMethod in paymentMethods" :key="paymentMethod.id">
                <td>{{ paymentMethod.name }}</td>
                <td><StatusBadge :active="paymentMethod.isActive" /></td>
                <td class="muted small">
                  {{ formatDateTime(paymentMethod.audit.updatedAtUtc) }} · {{ paymentMethod.audit.updatedBy ?? 'Sistema' }}
                </td>
                <td>
                  <RouterLink :to="{ name: 'payment-method-edit', params: { id: paymentMethod.id } }">
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
