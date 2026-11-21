/** Id/name pair returned by the API for related entities and selectors. */
export interface NamedReference {
  id: number
  name: string
}

export interface PagedResult<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
}

/** RFC 7807 Problem Details (with validation errors when status is 400). */
export interface ProblemDetails {
  type?: string
  title?: string
  status?: number
  detail?: string
  errors?: Record<string, string[]>
}

export interface SetStatusRequest {
  isActive: boolean
}
