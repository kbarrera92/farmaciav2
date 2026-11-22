import { http } from '@/services/http'
import type { SaveSupplierRequest, Supplier } from '../types'

export const supplierService = {
  list: (includeInactive: boolean) => http.get<Supplier[]>('/suppliers', { query: { includeInactive } }),

  get: (id: number) => http.get<Supplier>(`/suppliers/${id}`),

  create: (request: SaveSupplierRequest) => http.post<Supplier>('/suppliers', request),

  update: (id: number, request: SaveSupplierRequest) => http.put<Supplier>(`/suppliers/${id}`, request),

  setStatus: (id: number, isActive: boolean) => http.patch<Supplier>(`/suppliers/${id}/status`, { isActive }),
}
