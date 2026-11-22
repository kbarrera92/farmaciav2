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

const dateFormatter = new Intl.DateTimeFormat('es-GT', { dateStyle: 'medium' })

/** Formats a date-only value (`YYYY-MM-DD`) without any time zone conversion. */
export function formatDate(value: string | null | undefined): string {
  if (!value) return '—'
  const [year, month, day] = value.split('-').map(Number)
  const date = new Date(year, month - 1, day)
  return Number.isNaN(date.getTime()) ? '—' : dateFormatter.format(date)
}

/** Today as `YYYY-MM-DD` in the browser's local time. */
export function todayIso(): string {
  const now = new Date()
  const month = String(now.getMonth() + 1).padStart(2, '0')
  const day = String(now.getDate()).padStart(2, '0')
  return `${now.getFullYear()}-${month}-${day}`
}
