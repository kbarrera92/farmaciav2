<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { RouterLink } from 'vue-router'
import AsyncState from '@/components/AsyncState.vue'
import PageHeader from '@/components/PageHeader.vue'
import PaginationBar from '@/components/PaginationBar.vue'
import StatusBadge from '@/components/StatusBadge.vue'
import { useAuthStore } from '@/modules/auth/stores/authStore'
import { categoryService } from '@/modules/catalog/services/categoryService'
import { errorMessage, http } from '@/services/http'
import type { NamedReference, PagedResult } from '@/types/api'
import { stockService } from '../services/stockService'
import type { StockListItem } from '../types'

const auth = useAuthStore()

const query = reactive({
  branchId: auth.user?.branch.id ?? null,
  categoryId: null as number | null,
  search: '',
  page: 1,
  pageSize: 20,
})
const result = ref<PagedResult<StockListItem> | null>(null)
const branches = ref<NamedReference[]>([])
const categories = ref<NamedReference[]>([])
const loading = ref(false)
const error = ref<string | null>(null)

async function load(): Promise<void> {
  if (query.branchId === null) return
  loading.value = true
  error.value = null
  try {
    result.value = await stockService.list({ ...query, branchId: query.branchId })
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
  try {
    ;[branches.value, categories.value] = await Promise.all([
      http.get<NamedReference[]>('/lookups/branches'),
      categoryService.list(false),
    ])
  } catch (e) {
    error.value = errorMessage(e)
    return
  }
  if (query.branchId === null) query.branchId = branches.value[0]?.id ?? null
  await load()
})
</script>

<template>
  <section class="page">
    <PageHeader title="Existencias" subtitle="Existencia y stock mínimo de los productos por sucursal.">
      <template #actions>
        <RouterLink :to="{ name: 'purchases' }" class="btn btn-secondary">Volver a compras</RouterLink>
      </template>
    </PageHeader>

    <form class="card toolbar" @submit.prevent="applyFilters">
      <div class="field">
        <label for="branch">Sucursal</label>
        <select id="branch" v-model="query.branchId" class="select" @change="applyFilters">
          <option v-for="b in branches" :key="b.id" :value="b.id">{{ b.name }}</option>
        </select>
      </div>
      <div class="field">
        <label for="category">Categoría</label>
        <select id="category" v-model="query.categoryId" class="select" @change="applyFilters">
          <option :value="null">Todas</option>
          <option v-for="c in categories" :key="c.id" :value="c.id">{{ c.name }}</option>
        </select>
      </div>
      <div class="field">
        <label for="search">Buscar</label>
        <input id="search" v-model="query.search" class="input" placeholder="Nombre o código" maxlength="100" />
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
                  <th class="number">Existencia</th>
                  <th class="number">Stock mínimo</th>
                  <th>Estado</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="item in result.items" :key="item.productId">
                  <td>
                    {{ item.name }}
                    <span class="muted small">· {{ item.presentation }}</span>
                  </td>
                  <td>{{ item.category.name }}</td>
                  <td class="number" :class="{ negative: item.quantity < 0 }">{{ item.quantity }}</td>
                  <td class="number">{{ item.minStock ?? '—' }}</td>
                  <td><StatusBadge :active="item.isActive" /></td>
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

.negative {
  color: #b91c1c;
  font-weight: 600;
}
</style>
