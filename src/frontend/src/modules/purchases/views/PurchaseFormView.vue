<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, reactive, ref, watch } from 'vue'
import { RouterLink, useRouter } from 'vue-router'
import AsyncState from '@/components/AsyncState.vue'
import PageHeader from '@/components/PageHeader.vue'
import { useAuthStore } from '@/modules/auth/stores/authStore'
import { supplierService } from '@/modules/catalog/services/supplierService'
import { paymentMethodService } from '@/modules/settings/services/paymentMethodService'
import { ApiError, errorMessage, http } from '@/services/http'
import type { NamedReference } from '@/types/api'
import { formatMoney, todayIso } from '@/utils/format'
import { purchaseService } from '../services/purchaseService'
import {
  PurchaseType,
  type PurchaseDetail,
  type PurchaseProduct,
  type PurchaseTypeValue,
  type SavePurchaseRequest,
} from '../types'

/** Editable purchase line; empty strings mean "not entered yet". */
interface LineRow {
  productId: number
  name: string
  presentation: string
  quantity: string | number
  unitCost: string | number
  salePrice: number
}

const DEFAULT_CREDIT_DAYS = 15
const SEARCH_DELAY_MS = 300

const props = defineProps<{ id?: number }>()

const auth = useAuthStore()
const router = useRouter()

const isNew = computed(() => props.id === undefined)

const purchase = ref<PurchaseDetail | null>(null)
const branches = ref<NamedReference[]>([])
const suppliers = ref<NamedReference[]>([])
const paymentMethods = ref<NamedReference[]>([])
const lines = ref<LineRow[]>([])

const form = reactive({
  purchaseDate: todayIso(),
  branchId: null as number | null,
  supplierId: null as number | null,
  documentNumber: '',
  type: PurchaseType.Cash as PurchaseTypeValue,
  paymentMethodId: null as number | null,
  creditDays: String(DEFAULT_CREDIT_DAYS) as string | number,
})

const loading = ref(false)
const loadError = ref<string | null>(null)
const saving = ref(false)
const error = ref<string | null>(null)
const fieldErrors = ref<Record<string, string | undefined>>({})
const itemErrors = ref<string[]>([])

const searchText = ref('')
const searchResults = ref<PurchaseProduct[]>([])
const searching = ref(false)
const searchError = ref<string | null>(null)
let searchTimer: ReturnType<typeof setTimeout> | undefined
let searchToken = 0

// v-model on <input type="number"> yields a number (or '' when cleared), not always a string.
function toNumber(value: string | number | null | undefined): number | null {
  if (value === null || value === undefined) return null
  if (typeof value === 'number') return Number.isFinite(value) ? value : null
  return value.trim() === '' ? null : Number(value)
}

/** The current supplier or payment method of a purchase stays selectable even if it was deactivated. */
function withCurrent(options: NamedReference[], current: NamedReference | null | undefined): NamedReference[] {
  return current && !options.some((o) => o.id === current.id) ? [...options, current] : options
}

const isLocked = computed(
  () => !!purchase.value && (purchase.value.isCancelled || !!purchase.value.accountPayable?.hasPayments),
)

const lockedMessage = computed(() => {
  if (purchase.value?.isCancelled) return 'La compra está anulada y no se puede editar.'
  if (purchase.value?.accountPayable?.hasPayments) return 'La cuenta por pagar de la compra ya tiene abonos y no se puede editar.'
  return null
})

function lineSubtotal(line: LineRow): number {
  return (toNumber(line.quantity) ?? 0) * (toNumber(line.unitCost) ?? 0)
}

const total = computed(() => lines.value.reduce((sum, line) => sum + lineSubtotal(line), 0))

function costWarning(line: LineRow): boolean {
  const cost = toNumber(line.unitCost)
  return cost !== null && cost > 0 && cost >= line.salePrice
}

function setPurchase(detail: PurchaseDetail): void {
  purchase.value = detail
  form.purchaseDate = detail.purchaseDate
  form.branchId = detail.branch.id
  form.supplierId = detail.supplier.id
  form.documentNumber = detail.documentNumber
  form.type = detail.type
  form.paymentMethodId = detail.paymentMethod?.id ?? null
  form.creditDays = detail.creditDays?.toString() ?? String(DEFAULT_CREDIT_DAYS)
  lines.value = detail.items.map((item) => ({
    productId: item.productId,
    name: item.productName,
    presentation: item.presentation,
    quantity: item.quantity,
    unitCost: item.unitCost,
    salePrice: item.salePrice,
  }))
}

async function load(): Promise<void> {
  loading.value = true
  loadError.value = null
  try {
    const [activeBranches, activeSuppliers, activeMethods, detail] = await Promise.all([
      http.get<NamedReference[]>('/lookups/branches'),
      supplierService.list(false),
      paymentMethodService.list(false),
      isNew.value ? Promise.resolve(null) : purchaseService.get(props.id!),
    ])
    branches.value = withCurrent(activeBranches, detail?.branch)
    suppliers.value = withCurrent(activeSuppliers, detail?.supplier)
    paymentMethods.value = withCurrent(activeMethods, detail?.paymentMethod)
    if (detail) setPurchase(detail)
    else form.branchId = auth.user?.branch.id ?? null
  } catch (e) {
    loadError.value = errorMessage(e)
  } finally {
    loading.value = false
  }
}

async function runSearch(): Promise<void> {
  const text = searchText.value.trim()
  const branchId = form.branchId
  if (text.length < 2 || branchId === null) {
    searchResults.value = []
    searchError.value = null
    searching.value = false
    return
  }
  const token = ++searchToken
  searching.value = true
  searchError.value = null
  try {
    const result = await purchaseService.listProducts({ branchId, search: text, page: 1, pageSize: 10 })
    if (token === searchToken) searchResults.value = result.items
  } catch (e) {
    if (token === searchToken) searchError.value = errorMessage(e)
  } finally {
    if (token === searchToken) searching.value = false
  }
}

function scheduleSearch(): void {
  clearTimeout(searchTimer)
  searchTimer = setTimeout(() => void runSearch(), SEARCH_DELAY_MS)
}

watch(searchText, scheduleSearch)
watch(
  () => form.branchId,
  () => {
    searchResults.value = []
    scheduleSearch()
  },
)
onBeforeUnmount(() => clearTimeout(searchTimer))

function addProduct(product: PurchaseProduct): void {
  if (lines.value.some((l) => l.productId === product.id)) return
  lines.value.push({
    productId: product.id,
    name: product.name,
    presentation: product.presentation,
    quantity: 1,
    unitCost: product.cost,
    salePrice: product.salePrice,
  })
  searchText.value = ''
  searchResults.value = []
}

function removeLine(index: number): void {
  lines.value.splice(index, 1)
}

function isInLines(productId: number): boolean {
  return lines.value.some((l) => l.productId === productId)
}

function buildRequest(): SavePurchaseRequest {
  const isCash = form.type === PurchaseType.Cash
  return {
    purchaseDate: form.purchaseDate,
    branchId: form.branchId,
    supplierId: form.supplierId,
    documentNumber: form.documentNumber,
    type: form.type,
    paymentMethodId: isCash ? form.paymentMethodId : null,
    creditDays: isCash ? null : toNumber(form.creditDays),
    items: lines.value.map((line) => ({
      productId: line.productId,
      quantity: toNumber(line.quantity),
      unitCost: toNumber(line.unitCost),
    })),
  }
}

const fields = [
  'purchaseDate',
  'branchId',
  'supplierId',
  'documentNumber',
  'type',
  'paymentMethodId',
  'creditDays',
] as const

async function save(): Promise<void> {
  error.value = null
  fieldErrors.value = {}
  itemErrors.value = []
  saving.value = true
  try {
    const request = buildRequest()
    if (isNew.value) {
      const created = await purchaseService.create(request)
      await router.replace({ name: 'purchase-detail', params: { id: created.id }, query: { created: '1' } })
    } else {
      await purchaseService.update(props.id!, request)
      await router.replace({ name: 'purchase-detail', params: { id: props.id }, query: { saved: '1' } })
    }
    return
  } catch (e) {
    error.value = errorMessage(e)
    if (e instanceof ApiError) {
      if (e.status === 400) {
        fieldErrors.value = Object.fromEntries(fields.map((field) => [field, e.fieldError(field)]))
        itemErrors.value = Object.entries(e.fieldErrors)
          .filter(([key]) => key.startsWith('items'))
          .flatMap(([, messages]) => messages)
      } else if (e.status === 409) {
        fieldErrors.value = { documentNumber: e.message }
      }
    }
    window.scrollTo({ top: 0, behavior: 'smooth' })
  } finally {
    saving.value = false
  }
}

onMounted(load)
</script>

<template>
  <section class="page">
    <PageHeader :title="isNew ? 'Nueva compra' : `Editar compra ${purchase?.documentNumber ?? ''}`">
      <template #actions>
        <RouterLink
          :to="isNew ? { name: 'purchases' } : { name: 'purchase-detail', params: { id } }"
          class="btn btn-secondary"
        >
          Cancelar
        </RouterLink>
      </template>
    </PageHeader>

    <AsyncState :loading="loading" :error="loadError" @retry="load">
      <p v-if="lockedMessage" class="alert alert-error" role="alert">{{ lockedMessage }}</p>
      <p v-if="error" class="alert alert-error" role="alert">{{ error }}</p>

      <form class="card" novalidate @submit.prevent="save">
        <fieldset :disabled="isLocked || saving">
          <h2>Datos de la compra</h2>
          <div class="form-grid">
            <div class="field">
              <label for="purchaseDate">Fecha</label>
              <input
                id="purchaseDate"
                v-model="form.purchaseDate"
                type="date"
                class="input"
                :max="todayIso()"
                :aria-invalid="!!fieldErrors.purchaseDate"
              />
              <p v-if="fieldErrors.purchaseDate" class="field-error">{{ fieldErrors.purchaseDate }}</p>
            </div>
            <div class="field">
              <label for="branchId">Sucursal</label>
              <select
                id="branchId"
                v-model="form.branchId"
                class="select"
                :disabled="!isNew"
                :aria-invalid="!!fieldErrors.branchId"
              >
                <option :value="null" disabled>Seleccione…</option>
                <option v-for="b in branches" :key="b.id" :value="b.id">{{ b.name }}</option>
              </select>
              <p v-if="!isNew" class="field-hint">La sucursal de una compra registrada no se puede cambiar.</p>
              <p v-if="fieldErrors.branchId" class="field-error">{{ fieldErrors.branchId }}</p>
            </div>
            <div class="field">
              <label for="supplierId">Proveedor</label>
              <select
                id="supplierId"
                v-model="form.supplierId"
                class="select"
                :aria-invalid="!!fieldErrors.supplierId"
              >
                <option :value="null" disabled>Seleccione…</option>
                <option v-for="s in suppliers" :key="s.id" :value="s.id">{{ s.name }}</option>
              </select>
              <p v-if="fieldErrors.supplierId" class="field-error">{{ fieldErrors.supplierId }}</p>
            </div>
            <div class="field">
              <label for="documentNumber">Número de documento</label>
              <input
                id="documentNumber"
                v-model="form.documentNumber"
                class="input"
                maxlength="30"
                placeholder="Factura del proveedor"
                :aria-invalid="!!fieldErrors.documentNumber"
              />
              <p v-if="fieldErrors.documentNumber" class="field-error">{{ fieldErrors.documentNumber }}</p>
            </div>
            <div class="field">
              <label for="type">Tipo de compra</label>
              <select id="type" v-model="form.type" class="select" :aria-invalid="!!fieldErrors.type">
                <option :value="PurchaseType.Cash">Contado</option>
                <option :value="PurchaseType.Credit">Crédito</option>
              </select>
              <p v-if="fieldErrors.type" class="field-error">{{ fieldErrors.type }}</p>
            </div>
            <div v-if="form.type === PurchaseType.Cash" class="field">
              <label for="paymentMethodId">Forma de pago</label>
              <select
                id="paymentMethodId"
                v-model="form.paymentMethodId"
                class="select"
                :aria-invalid="!!fieldErrors.paymentMethodId"
              >
                <option :value="null" disabled>Seleccione…</option>
                <option v-for="m in paymentMethods" :key="m.id" :value="m.id">{{ m.name }}</option>
              </select>
              <p v-if="fieldErrors.paymentMethodId" class="field-error">{{ fieldErrors.paymentMethodId }}</p>
            </div>
            <div v-else class="field">
              <label for="creditDays">Días de crédito</label>
              <input
                id="creditDays"
                v-model="form.creditDays"
                type="number"
                min="1"
                max="365"
                class="input"
                :aria-invalid="!!fieldErrors.creditDays"
              />
              <p v-if="fieldErrors.creditDays" class="field-error">{{ fieldErrors.creditDays }}</p>
            </div>
          </div>

          <h2>Productos</h2>
          <div class="field search">
            <label for="productSearch">Agregar producto</label>
            <input
              id="productSearch"
              v-model="searchText"
              class="input"
              maxlength="100"
              autocomplete="off"
              placeholder="Buscar por nombre, código o laboratorio (mín. 2 letras)"
              :disabled="form.branchId === null"
            />
            <p v-if="form.branchId === null" class="field-hint">Seleccione primero la sucursal.</p>
            <p v-if="searching" class="field-hint">Buscando…</p>
            <p v-if="searchError" class="field-error">{{ searchError }}</p>
            <ul v-if="searchResults.length > 0" class="results">
              <li v-for="product in searchResults" :key="product.id">
                <button type="button" :disabled="isInLines(product.id)" @click="addProduct(product)">
                  <span>
                    {{ product.name }}
                    <span class="muted small">· {{ product.presentation }}</span>
                    <span v-if="product.laboratory" class="muted small"> · {{ product.laboratory }}</span>
                  </span>
                  <span class="muted small">
                    Existencia {{ product.stock }} · Costo {{ formatMoney(product.cost) }} · Venta
                    {{ formatMoney(product.salePrice) }}
                    <template v-if="isInLines(product.id)"> · Ya agregado</template>
                  </span>
                </button>
              </li>
            </ul>
            <p
              v-else-if="!searching && searchText.trim().length >= 2 && !searchError"
              class="field-hint"
            >
              Sin resultados (solo productos activos).
            </p>
          </div>

          <ul v-if="itemErrors.length > 0" class="item-errors">
            <li v-for="message in itemErrors" :key="message" class="field-error">{{ message }}</li>
          </ul>

          <p v-if="lines.length === 0" class="muted">Aún no hay productos en la compra.</p>
          <div v-else class="table-wrapper">
            <table class="table">
              <thead>
                <tr>
                  <th>Producto</th>
                  <th class="number">Cantidad</th>
                  <th class="number">Costo unitario</th>
                  <th class="number">Subtotal</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                <tr v-for="(line, index) in lines" :key="line.productId">
                  <td>
                    {{ line.name }}
                    <span class="muted small">· {{ line.presentation }}</span>
                    <div v-if="costWarning(line)" class="warning small">
                      El costo es mayor o igual al precio de venta ({{ formatMoney(line.salePrice) }}).
                    </div>
                  </td>
                  <td class="number">
                    <input
                      v-model="line.quantity"
                      type="number"
                      min="1"
                      step="1"
                      class="input input-number"
                      :aria-label="`Cantidad de ${line.name}`"
                    />
                  </td>
                  <td class="number">
                    <input
                      v-model="line.unitCost"
                      type="number"
                      min="0"
                      step="0.01"
                      class="input input-number"
                      :aria-label="`Costo unitario de ${line.name}`"
                    />
                  </td>
                  <td class="number">{{ formatMoney(lineSubtotal(line)) }}</td>
                  <td>
                    <button type="button" class="btn btn-link" @click="removeLine(index)">Quitar</button>
                  </td>
                </tr>
              </tbody>
              <tfoot>
                <tr>
                  <th colspan="3" class="number">Total</th>
                  <th class="number">{{ formatMoney(total) }}</th>
                  <th />
                </tr>
              </tfoot>
            </table>
          </div>

          <div class="form-actions">
            <button type="submit" class="btn btn-primary" :disabled="isLocked || saving">
              {{ saving ? 'Guardando…' : isNew ? 'Registrar compra' : 'Guardar cambios' }}
            </button>
          </div>
        </fieldset>
      </form>
    </AsyncState>
  </section>
</template>

<style scoped>
fieldset {
  border: 0;
  margin: 0;
  padding: 0;
  min-width: 0;
}

h2 {
  margin: 1.25rem 0 0.75rem;
}

h2:first-child {
  margin-top: 0;
}

.search {
  position: relative;
}

.results {
  list-style: none;
  margin: 0.25rem 0 0;
  padding: 0;
  border: 1px solid var(--color-border);
  border-radius: 6px;
  background: var(--color-surface);
  max-height: 320px;
  overflow-y: auto;
}

.results button {
  display: flex;
  flex-direction: column;
  gap: 0.15rem;
  width: 100%;
  padding: 0.55rem 0.75rem;
  border: 0;
  border-bottom: 1px solid var(--color-border);
  background: none;
  text-align: left;
  cursor: pointer;
  font: inherit;
}

.results li:last-child button {
  border-bottom: 0;
}

.results button:hover:not(:disabled) {
  background: var(--color-primary-soft);
}

.results button:disabled {
  opacity: 0.55;
  cursor: not-allowed;
}

.item-errors {
  margin: 0.5rem 0;
  padding-left: 1.1rem;
}

.number {
  text-align: right;
  white-space: nowrap;
}

.input-number {
  width: 7.5rem;
  text-align: right;
}

.warning {
  color: #b45309;
}

.form-actions {
  display: flex;
  justify-content: flex-end;
  margin-top: 1.25rem;
}
</style>
