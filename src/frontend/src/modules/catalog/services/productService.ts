import { http } from '@/services/http'
import type { PagedResult } from '@/types/api'
import type { Product, ProductListItem, ProductQuery, SaveProductRequest } from '../types'

export const productService = {
  list: (query: ProductQuery) =>
    http.get<PagedResult<ProductListItem>>('/products', {
      query: {
        search: query.search?.trim(),
        categoryId: query.categoryId,
        supplierId: query.supplierId,
        isActive: query.isActive,
        isControlled: query.isControlled,
        isBonusEligible: query.isBonusEligible,
        page: query.page,
        pageSize: query.pageSize,
      },
    }),

  get: (id: number) => http.get<Product>(`/products/${id}`),

  create: (request: SaveProductRequest) => http.post<Product>('/products', request),

  update: (id: number, request: SaveProductRequest) => http.put<Product>(`/products/${id}`, request),

  setStatus: (id: number, isActive: boolean) => http.patch<Product>(`/products/${id}/status`, { isActive }),
}
