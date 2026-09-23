import { useRef, useState } from 'react'
import type { FormEvent } from 'react'
import type { ClientSummary } from '../api/clientsApi'
import { formatMoney, roundMoney } from '../lib/format'
import type { InvoiceFormValues, InvoiceLineValues } from '../lib/invoiceLines'
import { calculateTotals, newLine, parseNumber } from '../lib/invoiceLines'
import { BackLink } from './BackLink'
import { Field } from './Field'

type HeaderField = 'clientId' | 'issueDate' | 'dueDate' | 'currency' | 'notes'
type LineField = 'description' | 'quantity' | 'unitPrice' | 'taxPercent'
interface FormErrors {
  fields: Partial<Record<HeaderField, string>>
  items: Record<string, Partial<Record<LineField, string>>>
  itemsMessage?: string
}

// Mirrors CreateInvoiceValidator / UpdateInvoiceValidator in the API and BFF.
function validateLine(line: InvoiceLineValues): Partial<Record<LineField, string>> {
  const errors: Partial<Record<LineField, string>> = {}
  const quantity = parseNumber(line.quantity)
  const unitPrice = parseNumber(line.unitPrice)
  const taxPercent = parseNumber(line.taxPercent)

  if (line.description.trim() === '') {
    errors.description = 'Describe the work or product.'
  } else if (line.description.trim().length > 1000) {
    errors.description = 'Use 1000 characters or fewer.'
  }
  if (quantity === null || quantity <= 0) {
    errors.quantity = 'Enter a quantity above 0.'
  }
  if (unitPrice === null || unitPrice < 0) {
    errors.unitPrice = 'Enter a price of 0 or more.'
  }
  if (taxPercent === null || taxPercent < 0) {
    errors.taxPercent = 'Enter a tax rate of 0 or more.'
  }
  return errors
}

function validate(values: InvoiceFormValues): FormErrors {
  const fields: FormErrors['fields'] = {}
  if (!values.clientId) {
    fields.clientId = 'Choose who this invoice is for.'
  }
  if (!values.issueDate) {
    fields.issueDate = 'Enter the issue date.'
  }
  if (!values.dueDate) {
    fields.dueDate = 'Enter the due date.'
  } else if (values.issueDate && values.dueDate < values.issueDate) {
    fields.dueDate = 'The due date can’t be before the issue date.'
  }
  if (!/^[A-Z]{3}$/.test(values.currency.trim())) {
    fields.currency = 'Use a 3-letter currency code, like AUD or USD.'
  }
  if (values.notes.length > 4000) {
    fields.notes = 'Use 4000 characters or fewer.'
  }

  const items: FormErrors['items'] = {}
  for (const line of values.items) {
    const lineErrors = validateLine(line)
    if (Object.keys(lineErrors).length > 0) {
      items[line.key] = lineErrors
    }
  }

  return {
    fields,
    items,
    itemsMessage: values.items.length === 0 ? 'Add at least one line item.' : undefined,
  }
}

function hasErrors(errors: FormErrors): boolean {
  return Object.keys(errors.fields).length > 0 || Object.keys(errors.items).length > 0 || Boolean(errors.itemsMessage)
}

interface InvoiceFormProps {
  mode: 'create' | 'edit'
  title: string
  clients: ClientSummary[]
  initialValues: InvoiceFormValues
  submitting: boolean
  error: string | null
  onClientChange?: (clientId: string) => Promise<string | null>
  onSubmit: (values: InvoiceFormValues) => void
  onCancel: () => void
}

export function InvoiceForm({
  mode,
  title,
  clients,
  initialValues,
  submitting,
  error,
  onClientChange,
  onSubmit,
  onCancel,
}: InvoiceFormProps) {
  const [values, setValues] = useState<InvoiceFormValues>(initialValues)
  const [errors, setErrors] = useState<FormErrors>({ fields: {}, items: {} })
  const [currencyTouched, setCurrencyTouched] = useState(mode === 'edit')
  const formRef = useRef<HTMLFormElement>(null)

  const isDirty = JSON.stringify(values) !== JSON.stringify(initialValues)
  const totals = calculateTotals(values.items)
  const currency = /^[A-Z]{3}$/.test(values.currency) ? values.currency : null

  function update<K extends keyof InvoiceFormValues>(key: K, value: InvoiceFormValues[K]) {
    setValues((current) => ({ ...current, [key]: value }))
    if (errors.fields[key as HeaderField]) {
      setErrors((current) => ({ ...current, fields: { ...current.fields, [key]: undefined } }))
    }
  }

  function updateLine(key: string, field: LineField, value: string) {
    setValues((current) => ({
      ...current,
      items: current.items.map((line) => (line.key === key ? { ...line, [field]: value } : line)),
    }))
    if (errors.items[key]?.[field]) {
      setErrors((current) => ({ ...current, items: { ...current.items, [key]: { ...current.items[key], [field]: undefined } } }))
    }
  }

  function addLine() {
    const line = newLine()
    setValues((current) => ({ ...current, items: [...current.items, line] }))
    setErrors((current) => ({ ...current, itemsMessage: undefined }))
    // Focus the new row's description once it has rendered.
    requestAnimationFrame(() => formRef.current?.querySelector<HTMLInputElement>(`#${line.key}-description`)?.focus())
  }

  function removeLine(key: string) {
    setValues((current) => ({ ...current, items: current.items.filter((line) => line.key !== key) }))
  }

  async function handleClientChange(clientId: string) {
    update('clientId', clientId)
    // Default the currency to the client's preferred one, unless the user already chose a currency.
    if (clientId && onClientChange && !currencyTouched) {
      const preferred = await onClientChange(clientId)
      if (preferred) {
        setValues((current) => (current.clientId === clientId ? { ...current, currency: preferred } : current))
      }
    }
  }

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const trimmed: InvoiceFormValues = {
      ...values,
      currency: values.currency.trim(),
      notes: values.notes.trim(),
      items: values.items.map((line) => ({ ...line, description: line.description.trim() })),
    }
    const nextErrors = validate(trimmed)
    setErrors(nextErrors)
    if (hasErrors(nextErrors)) {
      // Errors render on the next frame; only then can the first invalid control be found and focused.
      requestAnimationFrame(() => {
        const form = formRef.current
        ;(form?.querySelector<HTMLElement>('[aria-invalid="true"]') ?? form?.querySelector<HTMLElement>('#add-line'))?.focus()
      })
      return
    }
    onSubmit(trimmed)
  }

  function handleCancel() {
    if (isDirty && !window.confirm('Discard your changes to this invoice?')) {
      return
    }
    onCancel()
  }

  return (
    <main className="page-narrow page-wide">
      <form ref={formRef} className="form-stack" onSubmit={handleSubmit} noValidate aria-labelledby="invoice-form-title">
        <header className="page-header">
          <div>
            <BackLink label={mode === 'create' ? 'Back to invoices' : 'Back to invoice'} onClick={handleCancel} disabled={submitting} />
            <h1 id="invoice-form-title">{title}</h1>
            <p className="page-intro">Fields marked * are required.</p>
          </div>
        </header>

        <fieldset className="panel">
          <legend>Details</legend>
          <div className="field-grid">
            <Field id="clientId" label="Client" required error={errors.fields.clientId} wide>
              {(describedBy) => (
                <select
                  id="clientId"
                  value={values.clientId}
                  onChange={(event) => handleClientChange(event.target.value)}
                  required
                  aria-invalid={errors.fields.clientId ? true : undefined}
                  aria-describedby={describedBy}
                  autoFocus={mode === 'create'}
                >
                  <option value="">Choose a client…</option>
                  {clients.map((client) => (
                    <option key={client.id} value={client.id}>
                      {client.companyName}
                    </option>
                  ))}
                </select>
              )}
            </Field>
            <Field id="issueDate" label="Issue date" required error={errors.fields.issueDate}>
              {(describedBy) => (
                <input
                  id="issueDate"
                  type="date"
                  value={values.issueDate}
                  onChange={(event) => update('issueDate', event.target.value)}
                  required
                  aria-invalid={errors.fields.issueDate ? true : undefined}
                  aria-describedby={describedBy}
                />
              )}
            </Field>
            <Field id="dueDate" label="Due date" required error={errors.fields.dueDate}>
              {(describedBy) => (
                <input
                  id="dueDate"
                  type="date"
                  value={values.dueDate}
                  min={values.issueDate || undefined}
                  onChange={(event) => update('dueDate', event.target.value)}
                  required
                  aria-invalid={errors.fields.dueDate ? true : undefined}
                  aria-describedby={describedBy}
                />
              )}
            </Field>
            <Field id="currency" label="Currency" required hint="3-letter ISO code, like AUD or USD." error={errors.fields.currency}>
              {(describedBy) => (
                <input
                  id="currency"
                  className="input-code"
                  value={values.currency}
                  maxLength={3}
                  onChange={(event) => {
                    setCurrencyTouched(true)
                    update('currency', event.target.value.toUpperCase())
                  }}
                  required
                  aria-invalid={errors.fields.currency ? true : undefined}
                  aria-describedby={describedBy}
                />
              )}
            </Field>
          </div>
        </fieldset>

        <fieldset className="panel">
          <legend>Line items</legend>
          <div className="line-items">
            <div className="line-items-head" aria-hidden="true">
              <span>Description</span>
              <span>Qty</span>
              <span>Unit price</span>
              <span>Tax %</span>
              <span className="line-items-amount">Amount</span>
              <span />
            </div>
            {values.items.map((line, index) => {
              const lineErrors = errors.items[line.key] ?? {}
              const rowLabel = `Line ${index + 1}`
              const amount = roundMoney((parseNumber(line.quantity) ?? 0) * (parseNumber(line.unitPrice) ?? 0))
              return (
                <div key={line.key} className="line-item" role="group" aria-label={rowLabel}>
                  {(['description', 'quantity', 'unitPrice', 'taxPercent'] as const).map((field) => {
                    const labels: Record<LineField, string> = {
                      description: 'Description',
                      quantity: 'Quantity',
                      unitPrice: 'Unit price',
                      taxPercent: 'Tax %',
                    }
                    const inputId = `${line.key}-${field}`
                    const errorId = `${inputId}-error`
                    return (
                      <div key={field} className={`line-item-cell line-item-${field}`}>
                        <label htmlFor={inputId} className="line-item-label">
                          {labels[field]} <span className="visually-hidden">({rowLabel})</span>
                        </label>
                        <input
                          id={inputId}
                          type={field === 'description' ? 'text' : 'number'}
                          inputMode={field === 'description' ? undefined : 'decimal'}
                          min={field === 'description' ? undefined : 0}
                          step={field === 'description' ? undefined : 'any'}
                          value={line[field]}
                          onChange={(event) => updateLine(line.key, field, event.target.value)}
                          aria-invalid={lineErrors[field] ? true : undefined}
                          aria-describedby={lineErrors[field] ? errorId : undefined}
                        />
                        {lineErrors[field] && (
                          <p id={errorId} className="field-error">
                            {lineErrors[field]}
                          </p>
                        )}
                      </div>
                    )
                  })}
                  <div className="line-item-cell line-items-amount">
                    <span className="line-item-label" aria-hidden="true">
                      Amount
                    </span>
                    {currency ? formatMoney(amount, currency) : amount.toFixed(2)}
                  </div>
                  <div className="line-item-cell line-item-remove">
                    <button
                      type="button"
                      className="button-ghost button-ghost-danger"
                      onClick={() => removeLine(line.key)}
                      disabled={values.items.length === 1}
                      aria-label={`Remove ${rowLabel.toLowerCase()}`}
                    >
                      Remove
                    </button>
                  </div>
                </div>
              )
            })}
            {errors.itemsMessage && <p className="field-error">{errors.itemsMessage}</p>}
            <div>
              <button id="add-line" type="button" className="button-secondary button-small" onClick={addLine}>
                Add line
              </button>
            </div>
          </div>

          <dl className="totals">
            <div>
              <dt>Subtotal</dt>
              <dd>{currency ? formatMoney(totals.subTotal, currency) : totals.subTotal.toFixed(2)}</dd>
            </div>
            <div>
              <dt>Tax</dt>
              <dd>{currency ? formatMoney(totals.taxTotal, currency) : totals.taxTotal.toFixed(2)}</dd>
            </div>
            <div className="totals-grand">
              <dt>Total</dt>
              <dd>{currency ? formatMoney(totals.grandTotal, currency) : totals.grandTotal.toFixed(2)}</dd>
            </div>
          </dl>
        </fieldset>

        <fieldset className="panel">
          <legend>Notes</legend>
          <div className="field-grid">
            <Field id="notes" label="Notes for the client" hint="Payment terms, bank details or a thank-you." error={errors.fields.notes} wide>
              {(describedBy) => (
                <textarea
                  id="notes"
                  rows={3}
                  value={values.notes}
                  onChange={(event) => update('notes', event.target.value)}
                  aria-invalid={errors.fields.notes ? true : undefined}
                  aria-describedby={describedBy}
                />
              )}
            </Field>
          </div>
        </fieldset>

        {error && (
          <div className="form-alert" role="alert">
            <strong>We couldn’t save this invoice.</strong> {error}
          </div>
        )}

        <div className="form-actions">
          <button type="button" className="button-secondary" onClick={handleCancel} disabled={submitting}>
            Cancel
          </button>
          <button type="submit" className="button-primary" disabled={submitting}>
            {submitting ? 'Saving…' : mode === 'create' ? 'Create draft' : 'Save changes'}
          </button>
        </div>
      </form>
    </main>
  )
}
