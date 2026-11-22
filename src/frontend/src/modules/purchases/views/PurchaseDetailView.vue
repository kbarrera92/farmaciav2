<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { RouterLink, useRoute, useRouter } from 'vue-router'
import AsyncState from '@/components/AsyncState.vue'
import AuditInfo from '@/components/AuditInfo.vue'
import PageHeader from '@/components/PageHeader.vue'
import { useAuthStore } from '@/modules/auth/stores/authStore'
import { Permissions } from '@/modules/auth/types'
import { ApiError, errorMessage } from '@/services/http'
import { formatDate, formatDateTime, formatMoney } from '@/utils/format'
import { purchaseService } from '../services/purchaseService'
import { AccountPayableStatus, PurchaseType, purchaseTypeLabel, type PurchaseDetail } from '../types'

const props = defineProps<{ id: number }>()

const route = useRoute()
const router = useRouter()

const purchase = ref<PurchaseDetail | null>(null)
const loading = ref(false)
const loadError = ref<string | null>(null)
const success = ref<string | null>(null)

const auth = useAuthStore()
const canManage = auth.can(Permissions.PurchasesManage)

/** Cancelled purchases and credit purchases with payments cannot be edited (P-83). */
const canEdit = computed(
  () =>
    canManage && !!purchase.value && !purchase.value.isCancelled && !purchase.value.accountPayable?.hasPayments,
)

const hasCostWarning =computed(() => purchase.value?.items.some((i) => i.costAtOrAboveSalePrice) ?? false)

async function load(): Promise<void> {
  loading.value = true
  loadError.value = null
  try {
    purchase.value = await purchaseService.get(props.id)
  } catch (e) {
    loadError.value = errorMessage(e)
  } finally {
    loading.value = false
  }
  if (route.query.created === '1' || route.query.saved === '1') {
    success.value = route.query.created === '1' ? 'Compra registrada.' : 'Cambios guardados.'
    await router.replace({ query: {} })
  }
}

const cancelling = ref(false)
const cancelReason = ref('')
const cancelSaving = ref(false)
const cancelError = ref<string | null>(null)
const cancelFieldError = ref<string | undefined>()

function openCancel(): void {
  cancelReason.value = ''
  cancelError.value = null
  cancelFieldError.value = undefined
  cancelling.value = true
}

async function confirmCancel(): Promise<void> {
  if (!purchase.value) return
  cancelError.value = null
  cancelFieldError.value = undefined
  cancelSaving.value = true
  try {
    purchase.value = await purchaseService.cancel(purchase.value.id, cancelReason.value)
    cancelling.value = false
    success.value = 'Compra anulada. El stock fue restado.'
  } catch (e) {
    cancelError.value = errorMessage(e)
    if (e instanceof ApiError && e.status === 400) cancelFieldError.value = e.fieldError('reason')
  } finally {
    cancelSaving.value = false
  }
}

onMounted(load)
</script>

<template>
  <section class="page">
    <PageHeader :title="purchase ? `Compra ${purchase.documentNumber}` : 'Compra'">
      <template #actions>
        <RouterLink v-if="canEdit" :to="{ name: 'purchase-edit', params: { id } }" class="btn btn-primary">
          Editar
        </RouterLink>
        <button v-if="canEdit && !cancelling" type="button" class="btn btn-danger" @click="openCancel">
          Anular
        </button>
        <RouterLink :to="{ name: 'purchases' }" class="btn btn-secondary">Volver</RouterLink>
      </template>
    </PageHeader>

    <AsyncState :loading="loading" :error="loadError" @retry="load">
      <template v-if="purchase">
        <p v-if="success" class="alert alert-success" role="status">{{ success }}</p>
        <p v-if="purchase.isCancelled" class="alert alert-error" role="alert">
          Compra anulada por {{ purchase.cancelledBy ?? 'Sistema' }} el {{ formatDateTime(purchase.cancelledAtUtc) }}.
          Motivo: {{ purchase.cancellationReason }}
        </p>
        <p v-if="hasCostWarning" class="alert alert-info">
          Hay productos cuyo costo de compra es mayor o igual al precio de venta de la sucursal.
        </p>

        <form v-if="cancelling" class="card" novalidate @submit.prevent="confirmCancel">
          <h2>Anular compra</h2>
          <p class="muted">
            Se restará del stock lo recibido en esta compra
            <template v-if="purchase.accountPayable"> y se descartará su cuenta por pagar</template>. La compra queda
            marcada como anulada y no se puede revertir.
          </p>
          <p v-if="cancelError" class="alert alert-error" role="alert">{{ cancelError }}</p>
          <div class="field">
            <label for="reason">Motivo</label>
            <textarea
              id="reason"
              v-model="cancelReason"
              class="textarea"
              rows="3"
              maxlength="250"
              :disabled="cancelSaving"
              :aria-invalid="!!cancelFieldError"
            />
            <p v-if="cancelFieldError" class="field-error">{{ cancelFieldError }}</p>
          </div>
          <div class="actions">
            <button type="submit" class="btn btn-danger" :disabled="cancelSaving">
              {{ cancelSaving ? 'Anulando…' : 'Confirmar anulación' }}
            </button>
            <button type="button" class="btn btn-secondary" :disabled="cancelSaving" @click="cancelling = false">
              Cancelar
            </button>
          </div>
        </form>

        <div class="card">
          <h2>Datos de la compra</h2>
          <dl class="details">
            <div>
              <dt>Fecha</dt>
              <dd>{{ formatDate(purchase.purchaseDate) }}</dd>
            </div>
            <div>
              <dt>Documento</dt>
              <dd>{{ purchase.documentNumber }}</dd>
            </div>
            <div>
              <dt>Proveedor</dt>
              <dd>{{ purchase.supplier.name }}</dd>
            </div>
            <div>
              <dt>Sucursal</dt>
              <dd>{{ purchase.branch.name }}</dd>
            </div>
            <div>
              <dt>Tipo</dt>
              <dd>{{ purchaseTypeLabel(purchase.type) }}</dd>
            </div>
            <div v-if="purchase.type === PurchaseType.Cash">
              <dt>Forma de pago</dt>
              <dd>{{ purchase.paymentMethod?.name ?? '—' }}</dd>
            </div>
            <div v-else>
              <dt>Días de crédito</dt>
              <dd>{{ purchase.creditDays ?? '—' }}</dd>
            </div>
            <div>
              <dt>Fecha de pago</dt>
              <dd>{{ purchase.paymentDate ? formatDate(purchase.paymentDate) : 'Pendiente' }}</dd>
            </div>
          </dl>
        </div>

        <div v-if="purchase.accountPayable" class="card">
          <h2>Cuenta por pagar</h2>
          <dl class="details">
            <div>
              <dt>Inicio</dt>
              <dd>{{ formatDate(purchase.accountPayable.startDate) }}</dd>
            </div>
            <div>
              <dt>Vencimiento</dt>
              <dd>{{ formatDate(purchase.accountPayable.dueDate) }}</dd>
            </div>
            <div>
              <dt>Total</dt>
              <dd>{{ formatMoney(purchase.accountPayable.total) }}</dd>
            </div>
            <div>
              <dt>Saldo</dt>
              <dd>{{ formatMoney(purchase.accountPayable.balance) }}</dd>
            </div>
            <div>
              <dt>Estado</dt>
              <dd>
                <span
                  class="badge"
                  :class="
                    purchase.accountPayable.status === AccountPayableStatus.Paid ? 'badge-success' : 'badge-warning'
                  "
                >
                  {{ purchase.accountPayable.status === AccountPayableStatus.Paid ? 'Pagada' : 'Pendiente' }}
                </span>
              </dd>
            </div>
          </dl>
        </div>

        <div class="card">
          <h2>Productos</h2>
          <div class="table-wrapper">
            <table class="table">
              <thead>
                <tr>
                  <th>Producto</th>
                  <th class="number">Cantidad</th>
                  <th class="number">Costo</th>
                  <th class="number">Precio venta</th>
                  <th class="number">Subtotal</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="item in purchase.items" :key="item.productId">
                  <td>
                    {{ item.productName }}
                    <span class="muted small">· {{ item.presentation }}</span>
                  </td>
                  <td class="number">{{ item.quantity }}</td>
                  <td class="number">
                    {{ formatMoney(item.unitCost) }}
                    <span v-if="item.costAtOrAboveSalePrice" class="badge badge-warning">≥ venta</span>
                  </td>
                  <td class="number">{{ formatMoney(item.salePrice) }}</td>
                  <td class="number">{{ formatMoney(item.subtotal) }}</td>
                </tr>
              </tbody>
              <tfoot>
                <tr>
                  <th colspan="4" class="number">Total</th>
                  <th class="number">{{ formatMoney(purchase.total) }}</th>
                </tr>
              </tfoot>
            </table>
          </div>
        </div>

        <AuditInfo :audit="purchase.audit" />
      </template>
    </AsyncState>
  </section>
</template>

<style scoped>
.details {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
  gap: 1rem;
  margin: 0;
}

dt {
  font-size: 0.8rem;
  color: var(--color-muted, #6b7280);
}

dd {
  margin: 0.15rem 0 0;
  font-weight: 500;
}

.number {
  text-align: right;
  white-space: nowrap;
}
</style>
