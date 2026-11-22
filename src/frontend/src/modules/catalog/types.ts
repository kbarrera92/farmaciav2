import type { AuditInfo, NamedReference } from '@/types/api'

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

export interface ProductListItem {
  id: number
  name: string
  presentation: string
  barcode: string | null
  category: NamedReference
  supplier: NamedReference
  price: number
  cost: number
  isControlled: boolean
  isBonusEligible: boolean
  isActive: boolean
}

/** Branch-specific values; null means the branch uses the product default. */
export interface ProductBranch {
  branchId: number
  branchName: string
  price: number | null
  minStock: number | null
  shelf: string | null
}

export interface Product {
  id: number
  name: string
  presentation: string
  activeIngredient: string | null
  therapeuticAction: string | null
  indications: string | null
  contraindications: string | null
  notes: string | null
  measure: string | null
  laboratory: string | null
  barcode: string | null
  category: NamedReference
  supplier: NamedReference
  price: number
  cost: number
  minStock: number | null
  isControlled: boolean
  isBonusEligible: boolean
  isActive: boolean
  branches: ProductBranch[]
  audit: AuditInfo
}

export interface ProductQuery {
  search?: string
  categoryId?: number | null
  supplierId?: number | null
  isActive?: boolean | null
  isControlled?: boolean | null
  isBonusEligible?: boolean | null
  page: number
  pageSize: number
}

export interface ProductBranchRequest {
  branchId: number
  price: number | null
  minStock: number | null
  shelf: string | null
}

export interface SaveProductRequest {
  name: string
  presentation: string
  activeIngredient: string | null
  therapeuticAction: string | null
  indications: string | null
  contraindications: string | null
  notes: string | null
  measure: string | null
  laboratory: string | null
  barcode: string | null
  categoryId: number | null
  supplierId: number | null
  price: number | null
  cost: number | null
  minStock: number | null
  isControlled: boolean
  isBonusEligible: boolean
  branches: ProductBranchRequest[]
}
