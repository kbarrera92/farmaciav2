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

/** Who and when created and last modified a record (null user = system). */
export interface AuditInfo {
  createdAtUtc: string
  createdBy: string | null
  updatedAtUtc: string
  updatedBy: string | null
}

export interface SetStatusRequest {
  isActive: boolean
}
