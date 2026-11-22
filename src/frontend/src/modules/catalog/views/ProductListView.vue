<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { RouterLink } from 'vue-router'
import AsyncState from '@/components/AsyncState.vue'
import PageHeader from '@/components/PageHeader.vue'
import PaginationBar from '@/components/PaginationBar.vue'
import StatusBadge from '@/components/StatusBadge.vue'
import { useAuthStore } from '@/modules/auth/stores/authStore'
import { Permissions } from '@/modules/auth/types'
import { errorMessage } from '@/services/http'
import type { PagedResult } from '@/types/api'
import { formatMoney } from '@/utils/format'
import { categoryService } from '../services/categoryService'
import { productService } from '../services/productService'
import { supplierService } from '../services/supplierService'
import type { Category, ProductListItem, ProductQuery, Supplier } from '../types'

type ProductType = 'all' | 'controlled' | 'bonus'

const auth = useAuthStore()
const canManage = auth.can(Permissions.CatalogManage)

const query = reactive<ProductQuery>({
  search: '',
  categoryId: null,
  supplierId: null,
  isActive: true,
  page: 1,
  pageSize: 20,
})
const productType = ref<ProductType>('all')
const result = ref<PagedResult<ProductListItem> | null>(null)
const categories = ref<Category[]>([])
const suppliers = ref<Supplier[]>([])
const loading = ref(false)
const error = ref<string | null>(null)

const request = computed<ProductQuery>(() => ({
  ...query,
  isControlled: productType.value === 'controlled' ? true : null,
  isBonusEligible: productType.value === 'bonus' ? true : null,
}))

async function load(): Promise<void> {
  loading.value = true
  error.value = null
  try {
    result.value = await productService.list(request.value)
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
    ;[categories.value, suppliers.value] = await Promise.all([categoryService.list(true), supplierService.list(true)])
  } catch {
    // the category and supplier filters are optional
  }
})
</script>

<template>
  <section class="page">
    <PageHeader title="Productos" subtitle="Catálogo de productos con sus precios y clasificación.">
      <template #actions>
        <RouterLink v-if="canManage" :to="{ name: 'product-create' }" class="btn btn-primary">
          Nuevo producto
        </RouterLink>
      </template>
    </PageHeader>

    <form class="card toolbar" @submit.prevent="applyFilters">
      <div class="field">
        <label for="search">Buscar</label>
        <input
          id="search"
          v-model="query.search"
          class="input"
          placeholder="Nombre, código, composición o laboratorio"
          maxlength="100"
        />
      </div>
      <div class="field">
        <label for="category">Categoría</label>
        <select id="category" v-model="query.categoryId" class="select" @change="applyFilters">
          <option :value="null">Todas</option>
          <option v-for="c in categories" :key="c.id" :value="c.id">{{ c.name }}</option>
        </select>
      </div>
      <div class="field">
        <label for="supplier">Proveedor</label>
        <select id="supplier" v-model="query.supplierId" class="select" @change="applyFilters">
          <option :value="null">Todos</option>
          <option v-for="s in suppliers" :key="s.id" :value="s.id">{{ s.name }}</option>
        </select>
      </div>
      <div class="field">
        <label for="type">Tipo</label>
        <select id="type" v-model="productType" class="select" @change="applyFilters">
          <option value="all">Todos</option>
          <option value="controlled">Controlados</option>
          <option value="bonus">Bonificables</option>
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
        empty-message="No se encontraron productos con esos filtros."
        @retry="load"
      >
        <template v-if="result">
          <div class="table-wrapper">
            <table class="table">
              <thead>
                <tr>
                  <th>Producto</th>
                  <th>Categoría</th>
                  <th>Proveedor</th>
                  <th class="number">Costo</th>
                  <th class="number">Precio</th>
                  <th>Tipo</th>
                  <th>Estado</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                <tr v-for="product in result.items" :key="product.id">
                  <td>
                    {{ product.name }}
                    <span class="muted small">· {{ product.presentation }}</span>
                    <div v-if="product.barcode" class="muted small">{{ product.barcode }}</div>
                  </td>
                  <td>{{ product.category.name }}</td>
                  <td>{{ product.supplier.name }}</td>
                  <td class="number">{{ formatMoney(product.cost) }}</td>
                  <td class="number">{{ formatMoney(product.price) }}</td>
                  <td>
                    <span v-if="product.isControlled" class="badge badge-warning">Controlado</span>
                    <span v-if="product.isBonusEligible" class="badge badge-info">Bonificable</span>
                    <span v-if="!product.isControlled && !product.isBonusEligible" class="muted small">Normal</span>
                  </td>
                  <td><StatusBadge :active="product.isActive" /></td>
                  <td>
                    <RouterLink :to="{ name: 'product-edit', params: { id: product.id } }">
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
.number {
  text-align: right;
  white-space: nowrap;
}
</style>
