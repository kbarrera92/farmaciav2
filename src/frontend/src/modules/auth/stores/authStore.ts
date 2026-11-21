import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import { ApiError } from '@/services/http'
import { authService } from '../services/authService'
import type { ChangePasswordRequest, CurrentUser, LoginRequest, PermissionCode } from '../types'

export const useAuthStore = defineStore('auth', () => {
  const user = ref<CurrentUser | null>(null)
  /** True once the session has been checked against the server. */
  const initialized = ref(false)

  const isAuthenticated = computed(() => user.value !== null)

  /** UI hint only: the API enforces every permission. */
  function can(permission: PermissionCode): boolean {
    return user.value?.permissions.includes(permission) ?? false
  }

  async function loadSession(): Promise<void> {
    try {
      user.value = await authService.me()
    } catch (error) {
      if (!(error instanceof ApiError) || error.status !== 401) throw error
      user.value = null
    } finally {
      initialized.value = true
    }
  }

  async function login(request: LoginRequest): Promise<void> {
    user.value = await authService.login(request)
    initialized.value = true
  }

  async function changePassword(request: ChangePasswordRequest): Promise<void> {
    user.value = await authService.changePassword(request)
  }

  async function logout(): Promise<void> {
    try {
      await authService.logout()
    } finally {
      clearSession()
    }
  }

  function clearSession(): void {
    user.value = null
  }

  return { user, initialized, isAuthenticated, can, loadSession, login, changePassword, logout, clearSession }
})
