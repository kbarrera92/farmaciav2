import type { AuditInfo } from '@/types/api'

export interface CompanySettings {
  /** False until the company data is saved for the first time. */
  isConfigured: boolean
  name: string
  taxId: string | null
  address: string | null
  phone: string | null
  email: string | null
  hasLogo: boolean
  logoUpdatedAtUtc: string | null
  audit: AuditInfo | null
}

export interface SaveCompanySettingsRequest {
  name: string
  taxId: string | null
  address: string | null
  phone: string | null
  email: string | null
}

export interface PaymentMethod {
  id: number
  name: string
  isActive: boolean
  audit: AuditInfo
}

export interface SavePaymentMethodRequest {
  name: string
}

/** Limits mirrored from the backend (CompanySettingsService), only to warn before uploading. */
export const LogoRules = {
  maxBytes: 512 * 1024,
  accept: 'image/png,image/jpeg',
} as const
