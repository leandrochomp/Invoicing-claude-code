import { roundMoney } from './format'

export interface InvoiceLineValues {
  // Stable React key for the row; `id` is only set for lines that already exist on the server.
  key: string
  id: string | null
  description: string
  quantity: string
  unitPrice: string
  // Entered as a percentage (10 means 10%); converted to the API's fraction on submit.
  taxPercent: string
}

export interface InvoiceFormValues {
  clientId: string
  issueDate: string
  dueDate: string
  currency: string
  notes: string
  items: InvoiceLineValues[]
}

let nextLineKey = 0
export function newLine(): InvoiceLineValues {
  nextLineKey += 1
  return { key: `line-${nextLineKey}`, id: null, description: '', quantity: '1', unitPrice: '', taxPercent: '0' }
}

export function parseNumber(value: string): number | null {
  if (value.trim() === '') {
    return null
  }
  const parsed = Number(value)
  return Number.isFinite(parsed) ? parsed : null
}

// A preview only: the API recalculates every total when it saves, with the same rounding.
export function calculateTotals(items: InvoiceLineValues[]) {
  let subTotal = 0
  let taxTotal = 0
  for (const line of items) {
    const lineTotal = roundMoney((parseNumber(line.quantity) ?? 0) * (parseNumber(line.unitPrice) ?? 0))
    subTotal += lineTotal
    taxTotal += roundMoney(lineTotal * ((parseNumber(line.taxPercent) ?? 0) / 100))
  }
  return { subTotal: roundMoney(subTotal), taxTotal: roundMoney(taxTotal), grandTotal: roundMoney(subTotal + taxTotal) }
}
