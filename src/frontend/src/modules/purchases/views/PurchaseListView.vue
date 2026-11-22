<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { RouterLink } from 'vue-router'
import AsyncState from '@/components/AsyncState.vue'
import PageHeader from '@/components/PageHeader.vue'
import PaginationBar from '@/components/PaginationBar.vue'
import { supplierService } from '@/modules/catalog/services/supplierService'
import { errorMessage, http } from '@/services/http'
import type { NamedReference, PagedResult } from '@/types/api'
import { formatDate, formatMoney } from '@/utils/format'
import { purchaseService } from '../services/purchaseService'
import { PurchaseType, purchaseTypeLabel, type PurchaseListItem, type PurchaseQuery } from '../types'

const query = reactive<PurchaseQuery>({
  search: '',
  branchId: null,
  supplierId: null,
  type: null,
  isCancelled: false,
  from: '',
  to: '',
  page: 1,
  pageSize: 20,
})
const result = ref<PagedResult<PurchaseListItem> | null>(null)
const branches = ref<NamedReference[]>([])
const suppliers = ref<NamedReference[]>([])
const loading = ref(false)
const error = ref<string | null>(null)

async function load(): Promise<void> {
  loading.value = true
  error.value = null
  try {
    result.value = await purchaseService.list(query)
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
    ;[branches.value, suppliers.value] = await Promise.all([
      http.get<NamedReference[]>('/lookups/branches'),
      supplierService.list(true),
    ])
  } catch {
    // the branch and supplier filters are optional
  }
})
</script>

<template>
  <section class="page">
    <PageHeader title="Compras" subtitle="Compras a proveedores registradas por sucursal.">
    </PageHeader>

    <form class="card toolbar" @submit.prevent="applyFilters">
      <div class="field">
        <label for="search">Documento</label>
        <input id="search" v-model="query.search" class="input" placeholder="Número de factura" maxlength="30" />
      </div>
      <div class="field">
        <label for="branch">Sucursal</label>
        <select id="branch" v-model="query.branchId" class="select" @change="applyFilters">
          <option :value="null">Todas</option>
          <option v-for="b in branches" :key="b.id" :value="b.id">{{ b.name }}</option>
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
        <select id="type" v-model="query.type" class="select" @change="applyFilters">
          <option :value="null">Todos</option>
          <option :value="PurchaseType.Cash">Contado</option>
          <option :value="PurchaseType.Credit">Crédito</option>
        </select>
      </div>
      <div class="field">
        <label for="status">Estado</label>
        <select id="status" v-model="query.isCancelled" class="select" @change="applyFilters">
          <option :value="false">Válidas</option>
          <option :value="true">Anuladas</option>
          <option :value="null">Todas</option>
        </select>
      </div>
      <div class="field">
        <label for="from">Desde</label>
        <input id="from" v-model="query.from" type="date" class="input" @change="applyFilters" />
      </div>
      <div class="field">
        <label for="to">Hasta</label>
        <input id="to" v-model="query.to" type="date" class="input" @change="applyFilters" />
      </div>
      <button type="submit" class="btn btn-secondary">Buscar</button>
    </form>

    <div class="card">
      <AsyncState
        :loading="loading"
        :error="error"
        :empty="result?.items.length === 0"
        empty-message="No se encontraron compras con esos filtros."
        @retry="load"
      >
        <template v-if="result">
          <div class="table-wrapper">
            <table class="table">
              <thead>
                <tr>
                  <th>Fecha</th>
                  <th>Documento</th>
                  <th>Proveedor</th>
                  <th>Sucursal</th>
                  <th>Tipo</th>
                  <th>Pago</th>
                  <th class="number">Total</th>
                  <th>Estado</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                <tr v-for="purchase in result.items" :key="purchase.id">
                  <td class="nowrap">{{ formatDate(purchase.purchaseDate) }}</td>
                  <td>{{ purchase.documentNumber }}</td>
                  <td>{{ purchase.supplier.name }}</td>
                  <td>{{ purchase.branch.name }}</td>
                  <td>{{ purchaseTypeLabel(purchase.type) }}</td>
                  <td>
                    <template v-if="purchase.paymentDate">
                      {{ purchase.paymentMethod ?? 'Cuenta por pagar' }}
                      <div class="muted small">{{ formatDate(purchase.paymentDate) }}</div>
                    </template>
                    <span v-else class="muted small">
                      {{ purchase.type === PurchaseType.Credit ? 'Pendiente' : '—' }}
                    </span>
                  </td>
                  <td class="number">{{ formatMoney(purchase.total) }}</td>
                  <td>
                    <span v-if="purchase.isCancelled" class="badge badge-muted">Anulada</span>
                    <span v-else class="badge badge-success">Válida</span>
                  </td>
                  <td>
                    <RouterLink :to="{ name: 'purchase-detail', params: { id: purchase.id } }">Ver</RouterLink>
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

.nowrap {
  white-space: nowrap;
}
</style>
