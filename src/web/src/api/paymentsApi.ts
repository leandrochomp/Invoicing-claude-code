import { requestJson, requestNoContent, withQuery } from './http'

// Mirrors InvoicingApi's PaymentMethod, which travels over the wire as its number.
export const PaymentMethod = {
  BankTransfer: 0,
  Card: 1,
  Cash: 2,
  Check: 3,
  Other: 4,
} as const
export type PaymentMethod = (typeof PaymentMethod)[keyof typeof PaymentMethod]

export const paymentMethodLabels: Record<PaymentMethod, string> = {
  [PaymentMethod.BankTransfer]: 'Bank transfer',
  [PaymentMethod.Card]: 'Card',
  [PaymentMethod.Cash]: 'Cash',
  [PaymentMethod.Check]: 'Check',
  [PaymentMethod.Other]: 'Other',
}

export interface Payment {
  id: string
  invoiceId: string
  amount: number
  paymentDate: string
  method: PaymentMethod
  notes: string | null
  version: number
}

export interface LedgerPayment extends Payment {
  invoiceNumber: number
  clientId: string
  clientName: string
  currency: string
}

export interface PaymentList {
  items: LedgerPayment[]
  page: number
  pageSize: number
  totalRecords: number
  totalPages: number
}

export interface PaymentInput {
  amount: number
  paymentDate: string
  method: PaymentMethod
  notes: string | null
}

export interface UpdatePaymentInput extends PaymentInput {
  version: number
}

export function listPayments(filter: { clientId?: string; page?: number; pageSize?: number } = {}): Promise<PaymentList> {
  return requestJson<PaymentList>(withQuery('/bff/payments', filter))
}

export function createPayment(invoiceId: string, input: PaymentInput): Promise<Payment> {
  return requestJson<Payment>(`/bff/invoices/${invoiceId}/payments`, { method: 'POST', body: JSON.stringify(input) })
}

export function updatePayment(invoiceId: string, id: string, input: UpdatePaymentInput): Promise<Payment> {
  return requestJson<Payment>(`/bff/invoices/${invoiceId}/payments/${id}`, { method: 'PUT', body: JSON.stringify(input) })
}

export function deletePayment(invoiceId: string, id: string): Promise<void> {
  return requestNoContent(`/bff/invoices/${invoiceId}/payments/${id}`, { method: 'DELETE' })
}
