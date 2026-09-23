import { useRef, useState } from 'react'
import type { FormEvent } from 'react'
import type { PaymentMethod } from '../api/paymentsApi'
import { paymentMethodLabels } from '../api/paymentsApi'
import { formatMoney } from '../lib/format'
import { Field } from './Field'

export interface PaymentFormValues {
  invoiceId: string
  amount: string
  paymentDate: string
  method: PaymentMethod
  notes: string
}

type PaymentField = keyof PaymentFormValues
type FieldErrors = Partial<Record<PaymentField, string>>

export interface InvoiceOption {
  id: string
  label: string
}

interface PaymentFormProps {
  heading: string
  initialValues: PaymentFormValues
  // When given, the user picks which invoice the payment is for; otherwise it's fixed by the caller.
  invoiceOptions?: InvoiceOption[]
  onInvoiceChange?: (invoiceId: string) => void
  currency: string | null
  // The most this payment may be — the invoice's balance, plus this payment's own amount when editing.
  maxAmount: number | null
  submitLabel: string
  submitting: boolean
  error: string | null
  onSubmit: (values: PaymentFormValues) => void
  onCancel: () => void
}

const methods = Object.entries(paymentMethodLabels).map(([value, label]) => ({ value: Number(value) as PaymentMethod, label }))

// Mirrors CreatePaymentValidator / UpdatePaymentValidator, plus the API's "not more than the balance" rule.
function validate(values: PaymentFormValues, needsInvoice: boolean, maxAmount: number | null, currency: string | null): FieldErrors {
  const errors: FieldErrors = {}
  const amount = Number(values.amount)
  if (needsInvoice && !values.invoiceId) {
    errors.invoiceId = 'Choose the invoice this payment is for.'
  }
  if (values.amount.trim() === '' || !Number.isFinite(amount) || amount <= 0) {
    errors.amount = 'Enter an amount above 0.'
  } else if (maxAmount !== null && amount > maxAmount + 1e-9) {
    const limit = currency ? formatMoney(maxAmount, currency) : maxAmount.toFixed(2)
    errors.amount = `This is more than the ${limit} still owed.`
  }
  if (!values.paymentDate) {
    errors.paymentDate = 'Enter the date the payment arrived.'
  }
  if (values.notes.length > 500) {
    errors.notes = 'Use 500 characters or fewer.'
  }
  return errors
}

export function PaymentForm({
  heading,
  initialValues,
  invoiceOptions,
  onInvoiceChange,
  currency,
  maxAmount,
  submitLabel,
  submitting,
  error,
  onSubmit,
  onCancel,
}: PaymentFormProps) {
  const [values, setValues] = useState<PaymentFormValues>(initialValues)
  const [errors, setErrors] = useState<FieldErrors>({})
  const formRef = useRef<HTMLFormElement>(null)

  function update<K extends PaymentField>(key: K, value: PaymentFormValues[K]) {
    setValues((current) => ({ ...current, [key]: value }))
    if (errors[key]) {
      setErrors((current) => ({ ...current, [key]: undefined }))
    }
  }

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const trimmed = { ...values, amount: values.amount.trim(), notes: values.notes.trim() }
    const nextErrors = validate(trimmed, Boolean(invoiceOptions), maxAmount, currency)
    setErrors(nextErrors)
    if (Object.values(nextErrors).some(Boolean)) {
      // Errors render on the next frame; only then can the first invalid control be found and focused.
      requestAnimationFrame(() => formRef.current?.querySelector<HTMLElement>('[aria-invalid="true"]')?.focus())
      return
    }
    onSubmit(trimmed)
  }

  return (
    <form ref={formRef} className="panel payment-form" onSubmit={handleSubmit} noValidate aria-labelledby="payment-form-title">
      <h2 id="payment-form-title" className="panel-title">
        {heading}
      </h2>
      <div className="field-grid">
        {invoiceOptions && (
          <Field id="invoiceId" label="Invoice" required error={errors.invoiceId} wide>
            {(describedBy) => (
              <select
                id="invoiceId"
                value={values.invoiceId}
                onChange={(event) => {
                  update('invoiceId', event.target.value)
                  onInvoiceChange?.(event.target.value)
                }}
                required
                aria-invalid={errors.invoiceId ? true : undefined}
                aria-describedby={describedBy}
                autoFocus
              >
                <option value="">Choose an unpaid invoice…</option>
                {invoiceOptions.map((option) => (
                  <option key={option.id} value={option.id}>
                    {option.label}
                  </option>
                ))}
              </select>
            )}
          </Field>
        )}
        <Field
          id="amount"
          label={currency ? `Amount (${currency})` : 'Amount'}
          required
          hint={maxAmount !== null && currency ? `${formatMoney(maxAmount, currency)} still owed.` : undefined}
          error={errors.amount}
        >
          {(describedBy) => (
            <input
              id="amount"
              type="number"
              inputMode="decimal"
              min={0}
              step="0.01"
              value={values.amount}
              onChange={(event) => update('amount', event.target.value)}
              required
              aria-invalid={errors.amount ? true : undefined}
              aria-describedby={describedBy}
              autoFocus={!invoiceOptions}
            />
          )}
        </Field>
        <Field id="paymentDate" label="Payment date" required error={errors.paymentDate}>
          {(describedBy) => (
            <input
              id="paymentDate"
              type="date"
              value={values.paymentDate}
              onChange={(event) => update('paymentDate', event.target.value)}
              required
              aria-invalid={errors.paymentDate ? true : undefined}
              aria-describedby={describedBy}
            />
          )}
        </Field>
        <Field id="method" label="Method" required>
          {(describedBy) => (
            <select
              id="method"
              value={values.method}
              onChange={(event) => update('method', Number(event.target.value) as PaymentMethod)}
              aria-describedby={describedBy}
            >
              {methods.map((method) => (
                <option key={method.value} value={method.value}>
                  {method.label}
                </option>
              ))}
            </select>
          )}
        </Field>
        <Field id="paymentNotes" label="Reference or notes" error={errors.notes}>
          {(describedBy) => (
            <input
              id="paymentNotes"
              value={values.notes}
              onChange={(event) => update('notes', event.target.value)}
              aria-invalid={errors.notes ? true : undefined}
              aria-describedby={describedBy}
            />
          )}
        </Field>
      </div>

      {error && (
        <div className="form-alert" role="alert">
          <strong>We couldn’t save this payment.</strong> {error}
        </div>
      )}

      <div className="inline-actions">
        <button type="button" className="button-secondary" onClick={onCancel} disabled={submitting}>
          Cancel
        </button>
        <button type="submit" className="button-primary" disabled={submitting}>
          {submitting ? 'Saving…' : submitLabel}
        </button>
      </div>
    </form>
  )
}
