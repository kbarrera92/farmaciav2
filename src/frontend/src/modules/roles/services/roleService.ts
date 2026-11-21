import { http } from '@/services/http'
import type { PermissionGroup, RoleDetail, RoleListItem, SaveRoleRequest } from '../types'

export const roleService = {
  list: () => http.get<RoleListItem[]>('/roles'),

  get: (id: number) => http.get<RoleDetail>(`/roles/${id}`),

  create: (request: SaveRoleRequest) => http.post<RoleDetail>('/roles', request),

  update: (id: number, request: SaveRoleRequest) => http.put<RoleDetail>(`/roles/${id}`, request),

  setStatus: (id: number, isActive: boolean) => http.patch<RoleDetail>(`/roles/${id}/status`, { isActive }),

  remove: (id: number) => http.delete<void>(`/roles/${id}`),

  permissionCatalog: () => http.get<PermissionGroup[]>('/permissions'),
}
