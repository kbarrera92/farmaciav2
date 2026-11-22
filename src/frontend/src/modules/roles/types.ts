import type { AuditInfo } from '@/types/api'

export interface RoleListItem {
  id: number
  name: string
  description: string | null
  isSystem: boolean
  isActive: boolean
  permissionCount: number
  userCount: number
}

export interface RoleDetail {
  id: number
  name: string
  description: string | null
  isSystem: boolean
  isActive: boolean
  permissions: string[]
  audit: AuditInfo
}

export interface SaveRoleRequest {
  name: string
  description: string | null
  permissions: string[]
}

export interface PermissionItem {
  code: string
  name: string
  description: string | null
}

export interface PermissionGroup {
  module: string
  permissions: PermissionItem[]
}
