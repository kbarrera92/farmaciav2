import type { AuditInfo } from '@/types/api'

export interface Branch {
  id: number
  name: string
  address: string
  phone: string | null
  isActive: boolean
  activeUserCount: number
  audit: AuditInfo
}

export interface SaveBranchRequest {
  name: string
  address: string
  phone: string | null
}
