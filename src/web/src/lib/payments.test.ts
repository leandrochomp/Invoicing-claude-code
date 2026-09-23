import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { Invoice } from '../api/invoicesApi'
import type { Payment } from '../api/paymentsApi'
import { PaymentMethod } from '../api/paymentsApi'
import { editableMax, newPaymentFormValues, paymentToFormValues, toPaymentInput } from './payments'

const payment: Payment = {
  id: 'p1',
  invoiceId: 'inv1',
  amount: 40,
  paymentDate: '2026-03-14T00:00:00Z',
  method: PaymentMethod.Card,
  notes: null,
  version: 2,
}

describe('payment form values', () => {
  beforeEach(() => {
    vi.useFakeTimers()
    vi.setSystemTime(new Date(2026, 8, 23, 12))
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it('converts form values to the API input, blanking empty notes', () => {
    expect(
      toPaymentInput({ invoiceId: 'inv1', amount: '12.50', paymentDate: '2026-03-14', method: PaymentMethod.Card, notes: '' }),
    ).toEqual({ amount: 12.5, paymentDate: '2026-03-14T00:00:00Z', method: PaymentMethod.Card, notes: null })
  })

  it('round-trips an existing payment into form values', () => {
    const values = paymentToFormValues(payment)

    expect(values).toEqual({ invoiceId: 'inv1', amount: '40', paymentDate: '2026-03-14', method: PaymentMethod.Card, notes: '' })
    expect(toPaymentInput(values)).toEqual({ amount: 40, paymentDate: payment.paymentDate, method: payment.method, notes: null })
  })

  it('defaults a new payment to the full balance, today, by bank transfer', () => {
    expect(newPaymentFormValues('inv1', 60)).toEqual({
      invoiceId: 'inv1',
      amount: '60.00',
      paymentDate: '2026-09-23',
      method: PaymentMethod.BankTransfer,
      notes: '',
    })
    expect(newPaymentFormValues('', null).amount).toBe('')
  })
})

describe('editableMax', () => {
  it('adds the payment’s own amount back to the invoice balance', () => {
    const invoice = { grandTotal: 100, payments: [payment, { ...payment, id: 'p2', amount: 30.1 }] } as Invoice

    expect(editableMax(invoice, payment)).toBe(69.9)
  })
})
