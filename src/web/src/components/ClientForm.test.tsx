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

  it('groups fields into labelled sections and marks required fields with an asterisk', () => {
    renderForm()

    expect(screen.getByRole('group', { name: 'Company and contact' })).toBeInTheDocument()
    expect(screen.getByRole('group', { name: 'Billing address' })).toBeInTheDocument()
    expect(screen.getByRole('group', { name: 'Invoicing' })).toBeInTheDocument()
    expect(screen.getByLabelText('Contact name')).not.toBeRequired()
    expect(screen.getByLabelText('Company name *')).toBeRequired()
    expect(screen.queryByText(/optional/i)).not.toBeInTheDocument()
    expect(screen.queryByText('Fields marked * are required.')).not.toBeInTheDocument()
    expect(screen.getByLabelText('Company name *').closest('.field')?.querySelector('.field-required')).toHaveAttribute(
      'data-tooltip',
      'Required',
    )
  })

  it('shows inline errors, focuses the first invalid field and does not submit an empty form', async () => {
    const user = userEvent.setup()
    const { onSubmit } = renderForm()

    await user.click(screen.getByRole('button', { name: 'Add client' }))

    expect(onSubmit).not.toHaveBeenCalled()
    expect(screen.getByLabelText('Company name *')).toHaveFocus()
    expect(screen.getByLabelText('Company name *')).toHaveAccessibleDescription('Enter the company name.')
    expect(screen.getByLabelText('Email *')).toHaveAttribute('aria-invalid', 'true')
    expect(screen.getByLabelText('Phone')).not.toHaveAttribute('aria-invalid')
  })

  it('validates email and currency format when the field loses focus', async () => {
    const user = userEvent.setup()
    renderForm()

    await user.type(screen.getByLabelText('Email *'), 'not-an-email')
    await user.type(screen.getByLabelText('Preferred currency *'), 'us')
    await user.tab()

    expect(screen.getByText('Enter an email address like name@company.com.')).toBeInTheDocument()
    expect(screen.getByText('Use a 3-letter currency code, like AUD or USD.')).toBeInTheDocument()
  })

  it.each(['a@b', 'name@company', 'name@company.c', 'name..x@company.com', 'name@@company.com'])(
    'rejects the malformed email %s',
    async (email) => {
      const user = userEvent.setup()
      renderForm()

      await user.type(screen.getByLabelText('Email *'), email)
      await user.tab()

      expect(screen.getByLabelText('Email *')).toHaveAccessibleDescription('Enter an email address like name@company.com.')
    },
  )

  it.each(['call me', '12345', '1234567890123456', '61+2 5550 1234'])('rejects the malformed phone %s', async (phone) => {
    const user = userEvent.setup()
    renderForm()

    await user.type(screen.getByLabelText('Phone'), phone)
    await user.tab()

    expect(screen.getByLabelText('Phone')).toHaveAccessibleDescription(
      'Enter a phone number with 7 to 15 digits, like +61 2 5550 1234.',
    )
  })

  it('submits trimmed values when email and phone are well formed', async () => {
    const user = userEvent.setup()
    const { onSubmit } = renderForm({
      mode: 'edit',
      initialValues: { ...existing, email: ' jane.doe+billing@mail.acme.co.uk ', phone: '+1 (555) 010-0100' },
    })

    await user.click(screen.getByRole('button', { name: 'Save changes' }))

    expect(onSubmit).toHaveBeenCalledWith(
      expect.objectContaining({ email: 'jane.doe+billing@mail.acme.co.uk', phone: '+1 (555) 010-0100' }),
    )
  })

  it('clears a field error as soon as the value becomes valid', async () => {
    const user = userEvent.setup()
    renderForm()
    await user.click(screen.getByRole('button', { name: 'Add client' }))

    await user.type(screen.getByLabelText('Company name *'), 'G')

    expect(screen.queryByText('Enter the company name.')).not.toBeInTheDocument()
  })

  it('asks before discarding unsaved changes', async () => {
    const confirm = vi.fn().mockReturnValue(false)
    vi.stubGlobal('confirm', confirm)
    const user = userEvent.setup()
    const { onCancel } = renderForm({ mode: 'edit', initialValues: existing })

    await user.type(screen.getByLabelText('City *'), 'x')
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
