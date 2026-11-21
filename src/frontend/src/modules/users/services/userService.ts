import { http } from '@/services/http'
import type { NamedReference, PagedResult } from '@/types/api'
import type { CreateUserRequest, UpdateUserRequest, UserDetail, UserListItem, UserQuery } from '../types'

export const userService = {
  list: (query: UserQuery) =>
    http.get<PagedResult<UserListItem>>('/users', {
      query: {
        search: query.search?.trim(),
        branchId: query.branchId,
        roleId: query.roleId,
        isActive: query.isActive,
        page: query.page,
        pageSize: query.pageSize,
      },
    }),

  get: (id: number) => http.get<UserDetail>(`/users/${id}`),

  create: (request: CreateUserRequest) => http.post<UserDetail>('/users', request),

  update: (id: number, request: UpdateUserRequest) => http.put<UserDetail>(`/users/${id}`, request),

  setStatus: (id: number, isActive: boolean) => http.patch<UserDetail>(`/users/${id}/status`, { isActive }),

  resetPassword: (id: number, newPassword: string) =>
    http.post<void>(`/users/${id}/reset-password`, { newPassword }),

  roleOptions: () => http.get<NamedReference[]>('/lookups/roles'),

  branchOptions: () => http.get<NamedReference[]>('/lookups/branches'),
}
