import { http } from '@/services/http'
import type { PagedResult } from '@/types/api'
import type {
  PurchaseDetail,
  PurchaseListItem,
  PurchaseProduct,
  PurchaseProductQuery,
  PurchaseQuery,
  SavePurchaseRequest,
} from '../types'

export const purchaseService = {
  list: (query: PurchaseQuery) =>
    http.get<PagedResult<PurchaseListItem>>('/purchases', {
      query: {
        search: query.search?.trim(),
        branchId: query.branchId,
        supplierId: query.supplierId,
        type: query.type,
        isCancelled: query.isCancelled,
        from: query.from,
        to: query.to,
        page: query.page,
        pageSize: query.pageSize,
      },
    }),

  get: (id: number) => http.get<PurchaseDetail>(`/purchases/${id}`),

  create: (request: SavePurchaseRequest) => http.post<PurchaseDetail>('/purchases', request),

  update: (id: number, request: SavePurchaseRequest) => http.put<PurchaseDetail>(`/purchases/${id}`, request),

  cancel: (id: number, reason: string) => http.post<PurchaseDetail>(`/purchases/${id}/cancel`, { reason }),

  listProducts: (query: PurchaseProductQuery) =>
    http.get<PagedResult<PurchaseProduct>>('/purchases/products', {
      query: {
        branchId: query.branchId,
        search: query.search?.trim(),
        page: query.page,
        pageSize: query.pageSize,
      },
    }),
}
