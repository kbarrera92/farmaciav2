import { http } from '@/services/http'
import type { ChangePasswordRequest, CurrentUser, LoginRequest } from '../types'

export const authService = {
  login: (request: LoginRequest) =>
    http.post<CurrentUser>('/auth/login', request, { skipUnauthorizedHandler: true }),

  logout: () => http.post<void>('/auth/logout', undefined, { skipUnauthorizedHandler: true }),

  me: () => http.get<CurrentUser>('/auth/me', { skipUnauthorizedHandler: true }),

  changePassword: (request: ChangePasswordRequest) => http.post<CurrentUser>('/auth/change-password', request),
}
