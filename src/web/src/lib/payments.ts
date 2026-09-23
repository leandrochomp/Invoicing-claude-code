import type { Invoice } from '../api/invoicesApi'
import { balanceDue } from '../api/invoicesApi'
import type { Payment, PaymentInput } from '../api/paymentsApi'
import { PaymentMethod } from '../api/paymentsApi'
import type { PaymentFormValues } from '../components/PaymentForm'
import { fromDateInput, roundMoney, todayDateInput, toDateInput } from './format'

export function toPaymentInput(values: PaymentFormValues): PaymentInput {
  return {
    amount: Number(values.amount),
    paymentDate: fromDateInput(values.paymentDate),
    method: values.method,
    notes: values.notes || null,
  }
}

export function paymentToFormValues(payment: Payment): PaymentFormValues {
  return {
    invoiceId: payment.invoiceId,
    amount: String(payment.amount),
    paymentDate: toDateInput(payment.paymentDate),
    method: payment.method,
    notes: payment.notes ?? '',
  }
}

// A new payment defaults to settling the whole balance today by bank transfer.
export function newPaymentFormValues(invoiceId: string, balance: number | null): PaymentFormValues {
  return {
    invoiceId,
    amount: balance !== null ? balance.toFixed(2) : '',
    paymentDate: todayDateInput(),
    method: PaymentMethod.BankTransfer,
    notes: '',
  }
}

// The most an existing payment may be edited up to: the invoice balance plus its own current amount.
export function editableMax(invoice: Invoice, payment: Payment): number {
  return roundMoney(balanceDue(invoice) + payment.amount)
}
