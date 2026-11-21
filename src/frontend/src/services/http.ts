import type { ProblemDetails } from '@/types/api'

/** Error raised for any non-successful API response. */
export class ApiError extends Error {
  readonly status: number
  /** Validation errors by field, with camelCase keys (e.g. `username`). */
  readonly fieldErrors: Record<string, string[]>

  constructor(status: number, problem: ProblemDetails | null) {
    super(problem?.detail ?? problem?.title ?? ApiError.defaultMessage(status))
    this.name = 'ApiError'
    this.status = status
    this.fieldErrors = ApiError.normalizeErrors(problem?.errors)
  }

  /** First validation message for a field, if any. */
  fieldError(field: string): string | undefined {
    return this.fieldErrors[field]?.[0]
  }

  private static normalizeErrors(errors?: Record<string, string[]>): Record<string, string[]> {
    const result: Record<string, string[]> = {}
    for (const [key, messages] of Object.entries(errors ?? {})) {
      const field = key.charAt(0).toLowerCase() + key.slice(1)
      result[field] = [...(result[field] ?? []), ...messages]
    }
    return result
  }

  private static defaultMessage(status: number): string {
    switch (status) {
      case 0:
        return 'No se pudo conectar con el servidor. Verifique su conexión.'
      case 401:
        return 'Su sesión expiró. Inicie sesión nuevamente.'
      case 403:
        return 'No tiene permiso para realizar esta acción.'
      case 404:
        return 'El recurso solicitado no existe.'
      case 429:
        return 'Demasiados intentos. Espere un momento e intente de nuevo.'
      default:
        return 'Ocurrió un error inesperado. Intente nuevamente.'
    }
  }
}

type UnauthorizedHandler = () => void
let onUnauthorized: UnauthorizedHandler | null = null

/** Registers the action executed when an authenticated request receives 401 (session expired). */
export function setUnauthorizedHandler(handler: UnauthorizedHandler): void {
  onUnauthorized = handler
}

type QueryValue = string | number | boolean | null | undefined

interface RequestOptions {
  query?: Record<string, QueryValue>
  body?: unknown
  /** Do not trigger the global 401 handler (login and session checks handle it themselves). */
  skipUnauthorizedHandler?: boolean
}

const API_BASE = '/api'

async function request<T>(method: string, path: string, options: RequestOptions = {}): Promise<T> {
  const url = new URL(`${API_BASE}${path}`, window.location.origin)
  for (const [key, value] of Object.entries(options.query ?? {})) {
    if (value !== undefined && value !== null && value !== '') url.searchParams.set(key, String(value))
  }

  let response: Response
  try {
    response = await fetch(url, {
      method,
      credentials: 'same-origin',
      headers: {
        Accept: 'application/json',
        ...(options.body !== undefined ? { 'Content-Type': 'application/json' } : {}),
      },
      body: options.body !== undefined ? JSON.stringify(options.body) : undefined,
    })
  } catch {
    throw new ApiError(0, null)
  }

  if (!response.ok) {
    if (response.status === 401 && !options.skipUnauthorizedHandler) onUnauthorized?.()
    throw new ApiError(response.status, await readProblem(response))
  }

  if (response.status === 204) return undefined as T
  return (await response.json()) as T
}

async function readProblem(response: Response): Promise<ProblemDetails | null> {
  const contentType = response.headers.get('Content-Type') ?? ''
  if (!contentType.includes('json')) return null
  try {
    return (await response.json()) as ProblemDetails
  } catch {
    return null
  }
}

export const http = {
  get: <T>(path: string, options?: RequestOptions) => request<T>('GET', path, options),
  post: <T>(path: string, body?: unknown, options?: RequestOptions) => request<T>('POST', path, { ...options, body }),
  put: <T>(path: string, body?: unknown, options?: RequestOptions) => request<T>('PUT', path, { ...options, body }),
  patch: <T>(path: string, body?: unknown, options?: RequestOptions) => request<T>('PATCH', path, { ...options, body }),
  delete: <T>(path: string, options?: RequestOptions) => request<T>('DELETE', path, options),
}

/** User-facing message for any error thrown by an API call. */
export function errorMessage(error: unknown): string {
  if (error instanceof ApiError) return error.message
  return 'Ocurrió un error inesperado. Intente nuevamente.'
}
