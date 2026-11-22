import type { NamedReference } from '@/types/api'

export interface CurrentUser {
  id: number
  fullName: string
  username: string
  role: NamedReference
  branch: NamedReference
  mustChangePassword: boolean
  permissions: string[]
}

export interface LoginRequest {
  username: string
  password: string
}

export interface ChangePasswordRequest {
  currentPassword: string
  newPassword: string
}

/** Permission codes defined by the backend (Pharmacy.Core/Auth/Permissions.cs). */
export const Permissions = {
  UsersView: 'users.view',
  UsersManage: 'users.manage',
  RolesView: 'roles.view',
  RolesManage: 'roles.manage',
  BranchesView: 'branches.view',
  BranchesManage: 'branches.manage',
  SettingsView: 'settings.view',
  SettingsManage: 'settings.manage',
  CatalogView: 'catalog.view',
  CatalogManage: 'catalog.manage',
  PurchasesView: 'purchases.view',
  PurchasesCreate: 'purchases.create',
  PurchasesManage: 'purchases.manage',
} as const

export type PermissionCode = (typeof Permissions)[keyof typeof Permissions]
