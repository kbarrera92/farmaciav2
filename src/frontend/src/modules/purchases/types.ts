import type { AuditInfo, NamedReference } from '@/types/api'

/** Backend enums arrive as numbers. */
export const PurchaseType = { Cash: 1, Credit: 2 } as const
export type PurchaseTypeValue = (typeof PurchaseType)[keyof typeof PurchaseType]

export const AccountPayableStatus = { Pending: 1, Paid: 2 } as const
export type AccountPayableStatusValue = (typeof AccountPayableStatus)[keyof typeof AccountPayableStatus]

export function purchaseTypeLabel(type: PurchaseTypeValue): string {
  return type === PurchaseType.Cash ? 'Contado' : 'Crédito'
}

export interface PurchaseListItem {
  id: number
  purchaseDate: string
  documentNumber: string
  supplier: NamedReference
  branch: NamedReference
  type: PurchaseTypeValue
  paymentMethod: string | null
  paymentDate: string | null
  total: number
  isCancelled: boolean
  createdBy: string | null
}

export interface PurchaseItem {
  productId: number
  productName: string
  presentation: string
  quantity: number
  unitCost: number
  subtotal: number
  salePrice: number
  /** Non-blocking warning (P-84): the cost is not lower than the sale price. */
  costAtOrAboveSalePrice: boolean
}

export interface AccountPayable {
  id: number
  startDate: string
  dueDate: string
  total: number
  balance: number
  status: AccountPayableStatusValue
  hasPayments: boolean
}

export interface PurchaseDetail {
  id: number
  purchaseDate: string
  documentNumber: string
  supplier: NamedReference
  branch: NamedReference
  type: PurchaseTypeValue
  paymentMethod: NamedReference | null
  paymentDate: string | null
  creditDays: number | null
  total: number
  isCancelled: boolean
  cancelledAtUtc: string | null
  cancelledBy: string | null
  cancellationReason: string | null
  items: PurchaseItem[]
  accountPayable: AccountPayable | null
  audit: AuditInfo
}

export interface PurchaseQuery {
  search?: string
  branchId?: number | null
  supplierId?: number | null
  type?: PurchaseTypeValue | null
  isCancelled?: boolean | null
  from?: string
  to?: string
  page: number
  pageSize: number
}

export interface SavePurchaseItemRequest {
  productId: number
  quantity: number | null
  unitCost: number | null
}

export interface SavePurchaseRequest {
  purchaseDate: string
  branchId: number | null
  supplierId: number | null
  documentNumber: string
  type: PurchaseTypeValue
  paymentMethodId: number | null
  creditDays: number | null
  items: SavePurchaseItemRequest[]
}

/** Product offered when registering a purchase, with the existence and sale price of the branch. */
export interface PurchaseProduct {
  id: number
  name: string
  presentation: string
  laboratory: string | null
  category: NamedReference
  supplier: NamedReference
  stock: number
  cost: number
  salePrice: number
}

export interface PurchaseProductQuery {
  branchId: number
  search?: string
  page: number
  pageSize: number
}

export interface StockListItem {
  productId: number
  name: string
  presentation: string
  category: NamedReference
  quantity: number
  minStock: number | null
  isActive: boolean
}

export interface StockQuery {
  branchId: number
  categoryId?: number | null
  search?: string
  page: number
  pageSize: number
}
