import { useRef, useState } from 'react'
import type { FormEvent } from 'react'
import { Field } from './Field'

export interface ClientFormValues {
  companyName: string
  contactName: string
  email: string
  phone: string
  addressLine1: string
  addressLine2: string
  city: string
  stateOrRegion: string
  postalCode: string
  country: string
  preferredCurrency: string
  isActive: boolean
}

type TextField = Exclude<keyof ClientFormValues, 'isActive'>
type FieldErrors = Partial<Record<TextField, string>>

const emptyClientFormValues: ClientFormValues = {
  companyName: '',
  contactName: '',
  email: '',
  phone: '',
  addressLine1: '',
  addressLine2: '',
  city: '',
  stateOrRegion: '',
  postalCode: '',
  country: '',
  preferredCurrency: '',
  isActive: true,
}

// Mirrors CreateClientRequestValidator / UpdateClientRequestValidator in the API so users see
// problems next to the field instead of as a combined server message after submitting.
const fieldRules: Record<TextField, { label: string; required: boolean; maxLength: number }> = {
  companyName: { label: 'Company name', required: true, maxLength: 255 },
  contactName: { label: 'Contact name', required: false, maxLength: 255 },
  email: { label: 'Email', required: true, maxLength: 255 },
  phone: { label: 'Phone', required: false, maxLength: 20 },
  addressLine1: { label: 'Address line 1', required: true, maxLength: 255 },
  addressLine2: { label: 'Address line 2', required: false, maxLength: 255 },
  city: { label: 'City', required: true, maxLength: 100 },
  stateOrRegion: { label: 'State / region', required: true, maxLength: 100 },
  postalCode: { label: 'Postal code', required: true, maxLength: 20 },
  country: { label: 'Country', required: true, maxLength: 100 },
  preferredCurrency: { label: 'Preferred currency', required: true, maxLength: 3 },
}

const fieldOrder = Object.keys(fieldRules) as TextField[]

const commonCurrencies = ['AUD', 'USD', 'EUR', 'GBP', 'NZD', 'CAD', 'JPY', 'SGD', 'CHF', 'BRL']

// Same patterns as ContactRuleExtensions in the API and BFF: a dotted domain with a letter TLD, and a
// phone made of digits, spaces and ( ) . - with an optional leading +.
const emailPattern =
  /^[A-Za-z0-9!#$%&'*+/=?^_`{|}~-]+(\.[A-Za-z0-9!#$%&'*+/=?^_`{|}~-]+)*@([A-Za-z0-9]([A-Za-z0-9-]*[A-Za-z0-9])?\.)+[A-Za-z]{2,}$/

function isValidPhone(value: string): boolean {
  const digits = value.replace(/\D/g, '').length
  return /^\+?[0-9 ().-]+$/.test(value) && digits >= 7 && digits <= 15
}

function validateField(field: TextField, rawValue: string): string | undefined {
  const value = rawValue.trim()
  const rule = fieldRules[field]

  if (rule.required && value === '') {
    return `Enter the ${rule.label.toLowerCase()}.`
  }
  if (field === 'email' && value !== '' && !emailPattern.test(value)) {
    return 'Enter an email address like name@company.com.'
  }
  if (field === 'phone' && value !== '' && !isValidPhone(value)) {
    return 'Enter a phone number with 7 to 15 digits, like +61 2 5550 1234.'
  }
  if (field === 'preferredCurrency' && value !== '' && !/^[A-Z]{3}$/.test(value)) {
    return 'Use a 3-letter currency code, like AUD or USD.'
  }
  if (value.length > rule.maxLength) {
    return `Use ${rule.maxLength} characters or fewer.`
  }
  return undefined
}

function validateAll(values: ClientFormValues): FieldErrors {
  const errors: FieldErrors = {}
  for (const field of fieldOrder) {
    const error = validateField(field, values[field])
    if (error) {
      errors[field] = error
    }
  }
  return errors
}

interface ClientFormProps {
  mode: 'create' | 'edit'
  initialValues?: ClientFormValues
  submitting: boolean
  error: string | null
  onSubmit: (values: ClientFormValues) => void
  onCancel: () => void
}

export function ClientForm({ mode, initialValues, submitting, error, onSubmit, onCancel }: ClientFormProps) {
  const startingValues = initialValues ?? emptyClientFormValues
  const [values, setValues] = useState<ClientFormValues>(startingValues)
  const [errors, setErrors] = useState<FieldErrors>({})
  const formRef = useRef<HTMLFormElement>(null)

  const isDirty = (Object.keys(startingValues) as (keyof ClientFormValues)[]).some(
    (key) => values[key] !== startingValues[key],
  )

  function update<K extends keyof ClientFormValues>(key: K, value: ClientFormValues[K]) {
    setValues((current) => ({ ...current, [key]: value }))
    // Clear a field's error as soon as the user fixes it, but don't nag while they're still typing.
    if (key !== 'isActive' && errors[key as TextField]) {
      setErrors((current) => ({ ...current, [key]: validateField(key as TextField, value as string) }))
    }
  }

  function handleBlur(field: TextField) {
    // Only validate fields the user has actually typed in; tabbing past an empty field isn't an error yet.
    if (values[field] !== '' || errors[field]) {
      setErrors((current) => ({ ...current, [field]: validateField(field, values[field]) }))
    }
  }

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const nextErrors = validateAll(values)
    setErrors(nextErrors)

    const firstInvalid = fieldOrder.find((field) => nextErrors[field])
    if (firstInvalid) {
      formRef.current?.querySelector<HTMLInputElement>(`#${firstInvalid}`)?.focus()
      return
    }
    // Validation runs on trimmed values, so send those too — the API doesn't trim before checking formats.
    const trimmed = { ...values }
    for (const field of fieldOrder) {
      trimmed[field] = values[field].trim()
    }
    onSubmit(trimmed)
  }

  function handleCancel() {
    if (isDirty && !window.confirm('Discard your changes to this client?')) {
      return
    }
    onCancel()
  }

  function textField(
    name: TextField,
    options: { type?: string; inputMode?: 'tel' | 'email'; list?: string; hint?: string; wide?: boolean } = {},
  ) {
    const rule = fieldRules[name]
    return (
      <Field id={name} label={rule.label} required={rule.required} hint={options.hint} error={errors[name]} wide={options.wide}>
        {(describedBy) => (
          <input
            id={name}
            name={name}
            type={options.type ?? 'text'}
            inputMode={options.inputMode}
            list={options.list}
            value={values[name]}
            onChange={(event) =>
              update(name, name === 'preferredCurrency' ? event.target.value.toUpperCase() : event.target.value)
            }
            onBlur={() => handleBlur(name)}
            maxLength={name === 'preferredCurrency' ? 3 : undefined}
            required={rule.required}
            aria-invalid={errors[name] ? true : undefined}
            aria-describedby={describedBy}
            autoFocus={name === 'companyName'}
          />
        )}
      </Field>
    )
  }

  const title = mode === 'create' ? 'Add client' : 'Edit client'

  return (
    <main className="page-narrow">
      <form ref={formRef} className="form-stack" onSubmit={handleSubmit} noValidate aria-labelledby="client-form-title">
        <header className="page-header">
          <div>
            <button type="button" className="back-link" onClick={handleCancel} disabled={submitting}>
              <svg viewBox="0 0 16 16" width="16" height="16" aria-hidden="true">
                <path d="M10 3 5 8l5 5" fill="none" stroke="currentColor" strokeWidth="1.75" strokeLinecap="round" strokeLinejoin="round" />
              </svg>
              Back to clients
            </button>
            <h1 id="client-form-title">{title}</h1>
          </div>
        </header>

        <fieldset className="panel">
          <legend>Company and contact</legend>
          <div className="field-grid">
            {textField('companyName', { wide: true })}
            {textField('contactName')}
            {textField('phone', { type: 'tel', inputMode: 'tel' })}
            {textField('email', { type: 'email', inputMode: 'email', wide: true })}
          </div>
        </fieldset>

        <fieldset className="panel">
          <legend>Billing address</legend>
          <div className="field-grid">
            {textField('addressLine1', { wide: true })}
            {textField('addressLine2', { wide: true })}
            {textField('city')}
            {textField('stateOrRegion')}
            {textField('postalCode')}
            {textField('country')}
          </div>
        </fieldset>

        <fieldset className="panel">
          <legend>Invoicing</legend>
          <div className="field-grid">
            {textField('preferredCurrency', { list: 'client-currency-options', hint: '3-letter ISO code, like AUD or USD.' })}
            <datalist id="client-currency-options">
              {commonCurrencies.map((code) => (
                <option key={code} value={code} />
              ))}
            </datalist>

            {mode === 'edit' && (
              <div className="field field-wide">
                <label className="checkbox" htmlFor="isActive">
                  <input
                    id="isActive"
                    type="checkbox"
                    checked={values.isActive}
                    onChange={(event) => update('isActive', event.target.checked)}
                  />
                  Active
                </label>
              </div>
            )}
          </div>
        </fieldset>

        {error && (
          <div className="form-alert" role="alert">
            <strong>We couldn’t save this client.</strong> {error}
          </div>
        )}

        <div className="form-actions">
          <button type="button" className="button-secondary" onClick={handleCancel} disabled={submitting}>
            Cancel
          </button>
          <button type="submit" className="button-primary" disabled={submitting}>
            {submitting ? 'Saving…' : mode === 'create' ? 'Add client' : 'Save changes'}
          </button>
        </div>
      </form>
    </main>
  )
}
