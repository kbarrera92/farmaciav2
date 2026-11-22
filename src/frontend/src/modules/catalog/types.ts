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

export interface Supplier {
  id: number
  name: string
  taxId: string | null
  address: string | null
  phone: string | null
  email: string | null
  contactName: string | null
  contactPhone: string | null
  isActive: boolean
  audit: AuditInfo
}

export interface SaveSupplierRequest {
  name: string
  taxId: string | null
  address: string | null
  phone: string | null
  email: string | null
  contactName: string | null
  contactPhone: string | null
}
