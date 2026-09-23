import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import type { ClientFormValues } from './ClientForm'
import { ClientForm } from './ClientForm'

const existing: ClientFormValues = {
  companyName: 'Acme Corp',
  contactName: '',
  email: 'billing@acme.test',
  phone: '',
  addressLine1: '1 Main St',
  addressLine2: '',
  city: 'Springfield',
  stateOrRegion: 'IL',
  postalCode: '62701',
  country: 'US',
  preferredCurrency: 'USD',
  isActive: true,
}

function renderForm(overrides: Partial<Parameters<typeof ClientForm>[0]> = {}) {
  const props = {
    mode: 'create' as const,
    submitting: false,
    error: null,
    onSubmit: vi.fn(),
    onCancel: vi.fn(),
    ...overrides,
  }
  render(<ClientForm {...props} />)
  return props
}

describe('ClientForm', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('groups fields into labelled sections and marks optional fields', () => {
    renderForm()

    expect(screen.getByRole('group', { name: 'Company and contact' })).toBeInTheDocument()
    expect(screen.getByRole('group', { name: 'Billing address' })).toBeInTheDocument()
    expect(screen.getByRole('group', { name: 'Invoicing' })).toBeInTheDocument()
    expect(screen.getByLabelText('Contact name (optional)')).not.toBeRequired()
    expect(screen.getByLabelText('Company name')).toBeRequired()
  })

  it('shows inline errors, focuses the first invalid field and does not submit an empty form', async () => {
    const user = userEvent.setup()
    const { onSubmit } = renderForm()

    await user.click(screen.getByRole('button', { name: 'Add client' }))

    expect(onSubmit).not.toHaveBeenCalled()
    expect(screen.getByLabelText('Company name')).toHaveFocus()
    expect(screen.getByLabelText('Company name')).toHaveAccessibleDescription('Enter the company name.')
    expect(screen.getByLabelText('Email')).toHaveAttribute('aria-invalid', 'true')
    expect(screen.getByLabelText('Phone (optional)')).not.toHaveAttribute('aria-invalid')
  })

  it('validates email and currency format when the field loses focus', async () => {
    const user = userEvent.setup()
    renderForm()

    await user.type(screen.getByLabelText('Email'), 'not-an-email')
    await user.type(screen.getByLabelText('Preferred currency'), 'us')
    await user.tab()

    expect(screen.getByText('Enter an email address like name@company.com.')).toBeInTheDocument()
    expect(screen.getByText('Use a 3-letter currency code, like AUD or USD.')).toBeInTheDocument()
  })

  it('clears a field error as soon as the value becomes valid', async () => {
    const user = userEvent.setup()
    renderForm()
    await user.click(screen.getByRole('button', { name: 'Add client' }))

    await user.type(screen.getByLabelText('Company name'), 'G')

    expect(screen.queryByText('Enter the company name.')).not.toBeInTheDocument()
  })

  it('asks before discarding unsaved changes', async () => {
    const confirm = vi.fn().mockReturnValue(false)
    vi.stubGlobal('confirm', confirm)
    const user = userEvent.setup()
    const { onCancel } = renderForm({ mode: 'edit', initialValues: existing })

    await user.type(screen.getByLabelText('City'), 'x')
    await user.click(screen.getByRole('button', { name: 'Cancel' }))

    expect(confirm).toHaveBeenCalled()
    expect(onCancel).not.toHaveBeenCalled()
  })

  it('cancels without asking when nothing changed', async () => {
    const confirm = vi.fn()
    vi.stubGlobal('confirm', confirm)
    const user = userEvent.setup()
    const { onCancel } = renderForm({ mode: 'edit', initialValues: existing })

    await user.click(screen.getByRole('button', { name: 'Back to clients' }))

    expect(confirm).not.toHaveBeenCalled()
    expect(onCancel).toHaveBeenCalled()
  })

  it('shows the server error in an alert', () => {
    renderForm({ error: 'Email is already in use.' })

    expect(screen.getByRole('alert')).toHaveTextContent('Email is already in use.')
  })
})
