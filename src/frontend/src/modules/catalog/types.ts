import type { AuditInfo } from '@/types/api'

export interface Category {
  id: number
  name: string
  isActive: boolean
  audit: AuditInfo
}

export interface SaveCategoryRequest {
  name: string
}
