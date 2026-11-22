import { http } from '@/services/http'
import type { Category, SaveCategoryRequest } from '../types'

export const categoryService = {
  list: (includeInactive: boolean) => http.get<Category[]>('/categories', { query: { includeInactive } }),

  get: (id: number) => http.get<Category>(`/categories/${id}`),

  create: (request: SaveCategoryRequest) => http.post<Category>('/categories', request),

  update: (id: number, request: SaveCategoryRequest) => http.put<Category>(`/categories/${id}`, request),

  setStatus: (id: number, isActive: boolean) => http.patch<Category>(`/categories/${id}/status`, { isActive }),
}
