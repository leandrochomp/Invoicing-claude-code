// The API sends dates as ISO timestamps; invoices and payments only care about the calendar day, so
// the UI works with the YYYY-MM-DD part and never lets the viewer's time zone shift it.

export function toDateInput(iso: string): string {
  return iso.slice(0, 10)
}

export function fromDateInput(date: string): string {
  return `${date}T00:00:00Z`
}

export function todayDateInput(): string {
  const now = new Date()
  const local = new Date(now.getTime() - now.getTimezoneOffset() * 60_000)
  return local.toISOString().slice(0, 10)
}

export function addDays(date: string, days: number): string {
  const value = new Date(`${date}T00:00:00Z`)
  value.setUTCDate(value.getUTCDate() + days)
  return value.toISOString().slice(0, 10)
}

// Date fields show and accept Australian day-first dates (DD/MM/YYYY) whatever the browser's locale.
export function formatDateField(date: string): string {
  const [year, month, day] = date.split('-')
  return date ? `${day}/${month}/${year}` : ''
}

// Accepts D/M/YY or DD/MM/YYYY with `/`, `-` or `.` separators. Returns '' for anything that isn't a
// real calendar day, so form validation treats it like an empty field.
export function parseDateField(text: string): string {
  const match = /^(\d{1,2})[/.-](\d{1,2})[/.-](\d{2}|\d{4})$/.exec(text.trim())
  if (!match) {
    return ''
  }
  const day = Number(match[1])
  const month = Number(match[2])
  const year = Number(match[3].length === 2 ? `20${match[3]}` : match[3])
  // Date.UTC rolls impossible days like 31/02 into the next month, so round-trip to reject them.
  const value = new Date(Date.UTC(year, month - 1, day))
  if (value.getUTCFullYear() !== year || value.getUTCMonth() !== month - 1 || value.getUTCDate() !== day) {
    return ''
  }
  return value.toISOString().slice(0, 10)
}

const dateFormat = new Intl.DateTimeFormat('en-AU', { day: 'numeric', month: 'short', year: 'numeric', timeZone: 'UTC' })

export function formatDate(iso: string): string {
  return dateFormat.format(new Date(fromDateInput(toDateInput(iso))))
}

export function formatMoney(amount: number, currency: string): string {
  try {
    return new Intl.NumberFormat(undefined, { style: 'currency', currency }).format(amount)
  } catch {
    // An unrecognised code would otherwise throw; still show the number with its code.
    return `${amount.toFixed(2)} ${currency}`
  }
}

export function formatInvoiceNumber(invoiceNumber: number): string {
  return `#${invoiceNumber}`
}

// Rounds half away from zero to cents, like the API's MidpointRounding.AwayFromZero. Shifting by the
// decimal exponent (rather than `* 100`) rounds the number as written: 1.005 * 100 is 100.49999…
export function roundMoney(amount: number): number {
  const text = String(Math.abs(amount))
  // Very small or large numbers already print in exponent form, where the shift can't be spliced in.
  const cents = text.includes('e') ? Math.round(Math.abs(amount) * 100) : Math.round(Number(`${text}e2`))
  return Math.sign(amount) * Number(`${cents}e-2`)
}
