import type { InvoiceStatus } from './invoicesApi'
import type { LedgerPayment } from './paymentsApi'
import { requestJson } from './http'

export interface CurrencyTotals {
  currency: string
  outstanding: number
  overdue: number
  collectedLast30Days: number
}

export interface DueInvoice {
  id: string
  invoiceNumber: number
  clientId: string
  clientName: string
  status: InvoiceStatus
  dueDate: string
  currency: string
  grandTotal: number
  amountDue: number
  isOverdue: boolean
}

export interface DashboardSummary {
  totals: CurrencyTotals[]
  counts: { draft: number; outstanding: number; overdue: number; paid: number }
  dueInvoices: DueInvoice[]
  recentPayments: LedgerPayment[]
}

export function getDashboard(): Promise<DashboardSummary> {
  return requestJson<DashboardSummary>('/bff/dashboard')
}
