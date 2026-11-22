import { http } from '@/services/http'
import type { PaymentMethod, SavePaymentMethodRequest } from '../types'

export const paymentMethodService = {
  list: (includeInactive: boolean) => http.get<PaymentMethod[]>('/payment-methods', { query: { includeInactive } }),

  get: (id: number) => http.get<PaymentMethod>(`/payment-methods/${id}`),

  create: (request: SavePaymentMethodRequest) => http.post<PaymentMethod>('/payment-methods', request),

  update: (id: number, request: SavePaymentMethodRequest) =>
    http.put<PaymentMethod>(`/payment-methods/${id}`, request),

  setStatus: (id: number, isActive: boolean) =>
    http.patch<PaymentMethod>(`/payment-methods/${id}/status`, { isActive }),
}
