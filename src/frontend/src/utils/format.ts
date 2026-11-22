const dateTimeFormatter = new Intl.DateTimeFormat('es-GT', {
  dateStyle: 'medium',
  timeStyle: 'short',
})

/** Formats a UTC ISO date from the API in the browser's local time. */
export function formatDateTime(value: string | null | undefined): string {
  if (!value) return '—'
  const iso = /[zZ]|[+-]\d{2}:\d{2}$/.test(value) ? value : `${value}Z`
  const date = new Date(iso)
  return Number.isNaN(date.getTime()) ? '—' : dateTimeFormatter.format(date)
}

const moneyFormatter = new Intl.NumberFormat('es-GT', { minimumFractionDigits: 2, maximumFractionDigits: 2 })

/** Formats an amount with thousands separators and two decimals. */
export function formatMoney(value: number | null | undefined): string {
  return value === null || value === undefined ? '—' : moneyFormatter.format(value)
}
