<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { RouterLink, useRouter } from 'vue-router'
import AsyncState from '@/components/AsyncState.vue'
import AuditInfo from '@/components/AuditInfo.vue'
import PageHeader from '@/components/PageHeader.vue'
import StatusBadge from '@/components/StatusBadge.vue'
import { useAuthStore } from '@/modules/auth/stores/authStore'
import { Permissions } from '@/modules/auth/types'
import { ApiError, errorMessage, http } from '@/services/http'
import type { NamedReference } from '@/types/api'
import { formatMoney } from '@/utils/format'
import { categoryService } from '../services/categoryService'
import { productService } from '../services/productService'
import { supplierService } from '../services/supplierService'
import type { Product, SaveProductRequest } from '../types'

/** Editable branch row; empty strings mean "use the product default". */
interface BranchRow {
  branchId: number
  branchName: string
  price: string
  minStock: string
  shelf: string
}

const props = defineProps<{ id?: number }>()

const auth = useAuthStore()
const router = useRouter()

const isNew = computed(() => props.id === undefined)
const canManage = auth.can(Permissions.CatalogManage)

const product = ref<Product | null>(null)
const categories = ref<NamedReference[]>([])
const suppliers = ref<NamedReference[]>([])
const branchRows = ref<BranchRow[]>([])

const form = reactive({
  name: '',
  presentation: '',
  barcode: '',
  laboratory: '',
  measure: '',
  activeIngredient: '',
  therapeuticAction: '',
  indications: '',
  contraindications: '',
  notes: '',
  categoryId: null as number | null,
  supplierId: null as number | null,
  cost: '',
  price: '',
  minStock: '',
  isControlled: false,
  isBonusEligible: false,
})

const loading = ref(false)
const loadError = ref<string | null>(null)
const saving = ref(false)
const error = ref<string | null>(null)
const success = ref<string | null>(null)
const fieldErrors = ref<Record<string, string | undefined>>({})

function toNumber(value: string): number | null {
  return value.trim() === '' ? null : Number(value)
}

function toText(value: string): string | null {
  return value.trim() === '' ? null : value
}

/** Active branches plus any inactive branch the product already has settings for. */
function buildBranchRows(detail: Product | null, activeBranches: NamedReference[]): BranchRow[] {
  const rows = new Map<number, BranchRow>()
  for (const branch of activeBranches) {
    rows.set(branch.id, { branchId: branch.id, branchName: branch.name, price: '', minStock: '', shelf: '' })
  }
  for (const setting of detail?.branches ?? []) {
    rows.set(setting.branchId, {
      branchId: setting.branchId,
      branchName: setting.branchName,
      price: setting.price?.toString() ?? '',
      minStock: setting.minStock?.toString() ?? '',
      shelf: setting.shelf ?? '',
    })
  }
  return [...rows.values()].sort((a, b) => a.branchName.localeCompare(b.branchName, 'es'))
}

/** The current category or supplier of a product stays selectable even if it was deactivated. */
function withCurrent(options: NamedReference[], current: NamedReference | undefined): NamedReference[] {
  return current && !options.some((o) => o.id === current.id) ? [...options, current] : options
}

async function load(): Promise<void> {
  loading.value = true
  loadError.value = null
  try {
    const [activeCategories, activeSuppliers, activeBranches, detail] = await Promise.all([
      categoryService.list(false),
      supplierService.list(false),
      http.get<NamedReference[]>('/lookups/branches'),
      isNew.value ? Promise.resolve(null) : productService.get(props.id!),
    ])
    categories.value = withCurrent(activeCategories, detail?.category)
    suppliers.value = withCurrent(activeSuppliers, detail?.supplier)
    setProduct(detail, activeBranches)
  } catch (e) {
    loadError.value = errorMessage(e)
  } finally {
    loading.value = false
  }
}

let activeBranchOptions: NamedReference[] = []

function setProduct(detail: Product | null, activeBranches: NamedReference[] = activeBranchOptions): void {
  activeBranchOptions = activeBranches
  product.value = detail
  branchRows.value = buildBranchRows(detail, activeBranches)
  if (!detail) return
  form.name = detail.name
  form.presentation = detail.presentation
  form.barcode = detail.barcode ?? ''
  form.laboratory = detail.laboratory ?? ''
  form.measure = detail.measure ?? ''
  form.activeIngredient = detail.activeIngredient ?? ''
  form.therapeuticAction = detail.therapeuticAction ?? ''
  form.indications = detail.indications ?? ''
  form.contraindications = detail.contraindications ?? ''
  form.notes = detail.notes ?? ''
  form.categoryId = detail.category.id
  form.supplierId = detail.supplier.id
  form.cost = detail.cost.toString()
  form.price = detail.price.toString()
  form.minStock = detail.minStock?.toString() ?? ''
  form.isControlled = detail.isControlled
  form.isBonusEligible = detail.isBonusEligible
}

function buildRequest(): SaveProductRequest {
  return {
    name: form.name,
    presentation: form.presentation,
    activeIngredient: toText(form.activeIngredient),
    therapeuticAction: toText(form.therapeuticAction),
    indications: toText(form.indications),
    contraindications: toText(form.contraindications),
    notes: toText(form.notes),
    measure: toText(form.measure),
    laboratory: toText(form.laboratory),
    barcode: toText(form.barcode),
    categoryId: form.categoryId,
    supplierId: form.supplierId,
    price: toNumber(form.price),
    cost: toNumber(form.cost),
    minStock: toNumber(form.minStock),
    isControlled: form.isControlled,
    isBonusEligible: form.isBonusEligible,
    branches: branchRows.value
      .map((row) => ({
        branchId: row.branchId,
        price: toNumber(row.price),
        minStock: toNumber(row.minStock),
        shelf: toText(row.shelf),
      }))
      .filter((row) => row.price !== null || row.minStock !== null || row.shelf !== null),
  }
}

const fields = [
  'name',
  'presentation',
  'barcode',
  'laboratory',
  'measure',
  'activeIngredient',
  'therapeuticAction',
  'indications',
  'contraindications',
  'notes',
  'categoryId',
  'supplierId',
  'cost',
  'price',
  'minStock',
  'branches',
] as const

async function save(): Promise<void> {
  error.value = null
  success.value = null
  fieldErrors.value = {}
  saving.value = true
  try {
    const request = buildRequest()
    if (isNew.value) {
      const created = await productService.create(request)
      await router.replace({ name: 'product-edit', params: { id: created.id } })
      return
    }
    setProduct(await productService.update(props.id!, request))
    success.value = 'Cambios guardados.'
  } catch (e) {
    error.value = errorMessage(e)
    if (e instanceof ApiError && e.status === 400) {
      fieldErrors.value = Object.fromEntries(fields.map((field) => [field, e.fieldError(field)]))
    }
  } finally {
    saving.value = false
  }
}

async function toggleStatus(): Promise<void> {
  if (!product.value) return
  const activate = !product.value.isActive
  const name = product.value.name
  if (!window.confirm(activate ? `¿Activar el producto ${name}?` : `¿Desactivar el producto ${name}?`)) return
  error.value = null
  success.value = null
  try {
    setProduct(await productService.setStatus(product.value.id, activate))
    success.value = activate ? 'Producto activado.' : 'Producto desactivado.'
  } catch (e) {
    error.value = errorMessage(e)
  }
}

onMounted(load)
</script>

<template>
  <section class="page">
    <PageHeader :title="isNew ? 'Nuevo producto' : (product?.name ?? 'Producto')">
      <template #actions>
        <RouterLink :to="{ name: 'products' }" class="btn btn-secondary">Volver</RouterLink>
      </template>
    </PageHeader>

    <AsyncState :loading="loading" :error="loadError" @retry="load">
      <p v-if="success" class="alert alert-success" role="status">{{ success }}</p>
      <p v-if="error" class="alert alert-error" role="alert">{{ error }}</p>

      <form class="card" novalidate @submit.prevent="save">
        <fieldset :disabled="!canManage || saving">
          <h2>Datos generales</h2>
          <div class="form-grid">
            <div class="field">
              <label for="name">Nombre</label>
              <input id="name" v-model="form.name" class="input" maxlength="150" :aria-invalid="!!fieldErrors.name" />
              <p v-if="fieldErrors.name" class="field-error">{{ fieldErrors.name }}</p>
            </div>
            <div class="field">
              <label for="presentation">Presentación</label>
              <input
                id="presentation"
                v-model="form.presentation"
                class="input"
                maxlength="100"
                :aria-invalid="!!fieldErrors.presentation"
              />
              <p v-if="fieldErrors.presentation" class="field-error">{{ fieldErrors.presentation }}</p>
            </div>
            <div class="field">
              <label for="barcode">Código de barras</label>
              <input
                id="barcode"
                v-model="form.barcode"
                class="input"
                maxlength="25"
                :aria-invalid="!!fieldErrors.barcode"
              />
              <p v-if="fieldErrors.barcode" class="field-error">{{ fieldErrors.barcode }}</p>
            </div>
            <div class="field">
              <label for="laboratory">Laboratorio</label>
              <input
                id="laboratory"
                v-model="form.laboratory"
                class="input"
                maxlength="100"
                :aria-invalid="!!fieldErrors.laboratory"
              />
              <p v-if="fieldErrors.laboratory" class="field-error">{{ fieldErrors.laboratory }}</p>
            </div>
            <div class="field">
              <label for="measure">Medida</label>
              <input
                id="measure"
                v-model="form.measure"
                class="input"
                maxlength="75"
                :aria-invalid="!!fieldErrors.measure"
              />
              <p v-if="fieldErrors.measure" class="field-error">{{ fieldErrors.measure }}</p>
            </div>
            <div class="field">
              <label for="activeIngredient">Composición</label>
              <input
                id="activeIngredient"
                v-model="form.activeIngredient"
                class="input"
                maxlength="150"
                :aria-invalid="!!fieldErrors.activeIngredient"
              />
              <p v-if="fieldErrors.activeIngredient" class="field-error">{{ fieldErrors.activeIngredient }}</p>
            </div>
            <div class="field">
              <label for="therapeuticAction">Acción terapéutica</label>
              <input
                id="therapeuticAction"
                v-model="form.therapeuticAction"
                class="input"
                maxlength="150"
                :aria-invalid="!!fieldErrors.therapeuticAction"
              />
              <p v-if="fieldErrors.therapeuticAction" class="field-error">{{ fieldErrors.therapeuticAction }}</p>
            </div>
            <div class="field">
              <label for="indications">Indicaciones</label>
              <input
                id="indications"
                v-model="form.indications"
                class="input"
                maxlength="150"
                :aria-invalid="!!fieldErrors.indications"
              />
              <p v-if="fieldErrors.indications" class="field-error">{{ fieldErrors.indications }}</p>
            </div>
            <div class="field">
              <label for="contraindications">Contraindicaciones</label>
              <input
                id="contraindications"
                v-model="form.contraindications"
                class="input"
                maxlength="150"
                :aria-invalid="!!fieldErrors.contraindications"
              />
              <p v-if="fieldErrors.contraindications" class="field-error">{{ fieldErrors.contraindications }}</p>
            </div>
            <div class="field">
              <label for="notes">Observaciones</label>
              <input
                id="notes"
                v-model="form.notes"
                class="input"
                maxlength="250"
                :aria-invalid="!!fieldErrors.notes"
              />
              <p v-if="fieldErrors.notes" class="field-error">{{ fieldErrors.notes }}</p>
            </div>
          </div>

          <h2>Clasificación</h2>
          <div class="form-grid">
            <div class="field">
              <label for="category">Categoría</label>
              <select
                id="category"
                v-model="form.categoryId"
                class="select"
                :aria-invalid="!!fieldErrors.categoryId"
              >
                <option :value="null" disabled>Seleccione una categoría</option>
                <option v-for="c in categories" :key="c.id" :value="c.id">{{ c.name }}</option>
              </select>
              <p v-if="fieldErrors.categoryId" class="field-error">{{ fieldErrors.categoryId }}</p>
            </div>
            <div class="field">
              <label for="supplier">Proveedor</label>
              <select
                id="supplier"
                v-model="form.supplierId"
                class="select"
                :aria-invalid="!!fieldErrors.supplierId"
              >
                <option :value="null" disabled>Seleccione un proveedor</option>
                <option v-for="s in suppliers" :key="s.id" :value="s.id">{{ s.name }}</option>
              </select>
              <p v-if="fieldErrors.supplierId" class="field-error">{{ fieldErrors.supplierId }}</p>
            </div>
            <div class="field">
              <span class="label">Tipo</span>
              <label class="check"><input v-model="form.isControlled" type="checkbox" /> Controlado</label>
              <label class="check"><input v-model="form.isBonusEligible" type="checkbox" /> Bonificable</label>
              <p class="muted small">Sin ninguna casilla, el producto es normal.</p>
            </div>
          </div>

          <h2>Precios y stock mínimo</h2>
          <div class="form-grid">
            <div class="field">
              <label for="cost">Costo</label>
              <input
                id="cost"
                v-model="form.cost"
                type="number"
                min="0"
                step="0.01"
                class="input"
                :aria-invalid="!!fieldErrors.cost"
              />
              <p v-if="fieldErrors.cost" class="field-error">{{ fieldErrors.cost }}</p>
            </div>
            <div class="field">
              <label for="price">Precio general</label>
              <input
                id="price"
                v-model="form.price"
                type="number"
                min="0"
                step="0.01"
                class="input"
                :aria-invalid="!!fieldErrors.price"
              />
              <p v-if="fieldErrors.price" class="field-error">{{ fieldErrors.price }}</p>
            </div>
            <div class="field">
              <label for="minStock">Stock mínimo general</label>
              <input
                id="minStock"
                v-model="form.minStock"
                type="number"
                min="0"
                step="1"
                class="input"
                :aria-invalid="!!fieldErrors.minStock"
              />
              <p v-if="fieldErrors.minStock" class="field-error">{{ fieldErrors.minStock }}</p>
            </div>
          </div>

          <h2>Por sucursal</h2>
          <p class="muted small">
            Deje el precio o el stock mínimo vacío para usar el valor general
            <template v-if="form.price"> (precio general: {{ formatMoney(toNumber(form.price)) }})</template>. La
            estantería indica dónde está el producto en esa sucursal.
          </p>
          <p v-if="fieldErrors.branches" class="field-error">{{ fieldErrors.branches }}</p>
          <div class="table-wrapper">
            <table class="table">
              <thead>
                <tr>
                  <th>Sucursal</th>
                  <th>Precio</th>
                  <th>Stock mínimo</th>
                  <th>Estantería</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="row in branchRows" :key="row.branchId">
                  <td>{{ row.branchName }}</td>
                  <td>
                    <input
                      v-model="row.price"
                      type="number"
                      min="0"
                      step="0.01"
                      class="input"
                      :placeholder="form.price || 'General'"
                      :aria-label="`Precio en ${row.branchName}`"
                    />
                  </td>
                  <td>
                    <input
                      v-model="row.minStock"
                      type="number"
                      min="0"
                      step="1"
                      class="input"
                      :placeholder="form.minStock || 'General'"
                      :aria-label="`Stock mínimo en ${row.branchName}`"
                    />
                  </td>
                  <td>
                    <input
                      v-model="row.shelf"
                      class="input"
                      maxlength="30"
                      :aria-label="`Estantería en ${row.branchName}`"
                    />
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </fieldset>

        <div class="footer">
          <span v-if="product" class="muted small"><StatusBadge :active="product.isActive" /></span>
          <div v-if="canManage" class="actions">
            <button v-if="product" type="button" class="btn btn-secondary" @click="toggleStatus">
              {{ product.isActive ? 'Desactivar' : 'Activar' }}
            </button>
            <button type="submit" class="btn btn-primary" :disabled="saving">
              {{ saving ? 'Guardando…' : isNew ? 'Crear producto' : 'Guardar cambios' }}
            </button>
          </div>
        </div>
        <AuditInfo v-if="product" :audit="product.audit" />
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

h2 {
  margin: 1.5rem 0 0.75rem;
  font-size: 1rem;
}

h2:first-child {
  margin-top: 0;
}

.label {
  font-size: 0.85rem;
  font-weight: 600;
}

.check {
  display: flex;
  gap: 0.5rem;
  align-items: center;
  font-weight: 400;
  cursor: pointer;
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
