import { http } from '@/services/http'
import type { PagedResult } from '@/types/api'
import type { StockListItem, StockQuery } from '../types'

export const stockService = {
  list: (query: StockQuery) =>
    http.get<PagedResult<StockListItem>>('/stocks', {
      query: {
        branchId: query.branchId,
        categoryId: query.categoryId,
        search: query.search?.trim(),
        page: query.page,
        pageSize: query.pageSize,
      },
    }),
}
