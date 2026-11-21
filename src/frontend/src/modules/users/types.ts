import type { NamedReference } from '@/types/api'

export interface UserListItem {
  id: number
  fullName: string
  username: string
  role: NamedReference
  branch: NamedReference
  isActive: boolean
  isLockedOut: boolean
  lastLoginAtUtc: string | null
}

export interface UserDetail extends UserListItem {
  mustChangePassword: boolean
  createdAtUtc: string
  updatedAtUtc: string
}

export interface UserQuery {
  search?: string
  branchId?: number | null
  roleId?: number | null
  isActive?: boolean | null
  page: number
  pageSize: number
}

export interface UpdateUserRequest {
  fullName: string
  username: string
  roleId: number | null
  branchId: number | null
}

export interface CreateUserRequest extends UpdateUserRequest {
  password: string
}
