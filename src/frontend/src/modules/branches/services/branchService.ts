import { http } from '@/services/http'
import type { Branch, SaveBranchRequest } from '../types'

export const branchService = {
  list: (includeInactive: boolean) => http.get<Branch[]>('/branches', { query: { includeInactive } }),

  get: (id: number) => http.get<Branch>(`/branches/${id}`),

  create: (request: SaveBranchRequest) => http.post<Branch>('/branches', request),

  update: (id: number, request: SaveBranchRequest) => http.put<Branch>(`/branches/${id}`, request),

  setStatus: (id: number, isActive: boolean) => http.patch<Branch>(`/branches/${id}/status`, { isActive }),
}
