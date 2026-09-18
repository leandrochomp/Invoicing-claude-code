import { useState } from 'react'
import type { FormEvent } from 'react'

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

interface ClientFormProps {
  mode: 'create' | 'edit'
  initialValues?: ClientFormValues
  submitting: boolean
  error: string | null
  onSubmit: (values: ClientFormValues) => void
  onCancel: () => void
}

export function ClientForm({ mode, initialValues, submitting, error, onSubmit, onCancel }: ClientFormProps) {
  const [values, setValues] = useState<ClientFormValues>(initialValues ?? emptyClientFormValues)

  function update<K extends keyof ClientFormValues>(key: K, value: ClientFormValues[K]) {
    setValues((current) => ({ ...current, [key]: value }))
  }

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    onSubmit(values)
  }

  return (
    <main className="client-form-page">
      <form className="client-form" onSubmit={handleSubmit}>
        <h1>{mode === 'create' ? 'Add client' : 'Edit client'}</h1>

        <label htmlFor="companyName">Company name</label>
        <input
          id="companyName"
          value={values.companyName}
          onChange={(event) => update('companyName', event.target.value)}
          required
        />

        <label htmlFor="contactName">Contact name</label>
        <input
          id="contactName"
          value={values.contactName}
          onChange={(event) => update('contactName', event.target.value)}
        />

        <label htmlFor="email">Email</label>
        <input
          id="email"
          type="email"
          value={values.email}
          onChange={(event) => update('email', event.target.value)}
          required
        />

        <label htmlFor="phone">Phone</label>
        <input id="phone" value={values.phone} onChange={(event) => update('phone', event.target.value)} />

        <label htmlFor="addressLine1">Address line 1</label>
        <input
          id="addressLine1"
          value={values.addressLine1}
          onChange={(event) => update('addressLine1', event.target.value)}
          required
        />

        <label htmlFor="addressLine2">Address line 2</label>
        <input
          id="addressLine2"
          value={values.addressLine2}
          onChange={(event) => update('addressLine2', event.target.value)}
        />

        <label htmlFor="city">City</label>
        <input id="city" value={values.city} onChange={(event) => update('city', event.target.value)} required />

        <label htmlFor="stateOrRegion">State / region</label>
        <input
          id="stateOrRegion"
          value={values.stateOrRegion}
          onChange={(event) => update('stateOrRegion', event.target.value)}
          required
        />

        <label htmlFor="postalCode">Postal code</label>
        <input
          id="postalCode"
          value={values.postalCode}
          onChange={(event) => update('postalCode', event.target.value)}
          required
        />

        <label htmlFor="country">Country</label>
        <input
          id="country"
          value={values.country}
          onChange={(event) => update('country', event.target.value)}
          required
        />

        <label htmlFor="preferredCurrency">Preferred currency</label>
        <input
          id="preferredCurrency"
          value={values.preferredCurrency}
          onChange={(event) => update('preferredCurrency', event.target.value.toUpperCase())}
          maxLength={3}
          required
        />

        {mode === 'edit' && (
          <label className="client-form-checkbox" htmlFor="isActive">
            <input
              id="isActive"
              type="checkbox"
              checked={values.isActive}
              onChange={(event) => update('isActive', event.target.checked)}
            />
            Active
          </label>
        )}

        {error && (
          <p className="client-form-error" role="alert">
            {error}
          </p>
        )}

        <div className="client-form-actions">
          <button type="button" onClick={onCancel} disabled={submitting}>
            Cancel
          </button>
          <button type="submit" disabled={submitting}>
            {submitting ? 'Saving…' : 'Save'}
          </button>
        </div>
      </form>
    </main>
  )
}
