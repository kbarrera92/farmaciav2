import { http } from '@/services/http'
import type { CompanySettings, SaveCompanySettingsRequest } from '../types'

const basePath = '/settings/company'

export const companySettingsService = {
  get: () => http.get<CompanySettings>(basePath),

  update: (request: SaveCompanySettingsRequest) => http.put<CompanySettings>(basePath, request),

  uploadLogo: (file: File) => {
    const form = new FormData()
    form.append('logo', file)
    return http.put<CompanySettings>(`${basePath}/logo`, form)
  },

  removeLogo: () => http.delete<CompanySettings>(`${basePath}/logo`),

  /** Image URL; the version changes with every new logo so the browser does not show a stale one. */
  logoUrl: (settings: CompanySettings): string | null =>
    settings.hasLogo ? `/api${basePath}/logo?v=${encodeURIComponent(settings.logoUpdatedAtUtc ?? '')}` : null,
}
