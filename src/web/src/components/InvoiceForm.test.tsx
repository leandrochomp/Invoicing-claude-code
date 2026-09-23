import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { formatMoney } from '../lib/format'
import type { InvoiceFormValues } from '../lib/invoiceLines'
import { calculateTotals, newLine } from '../lib/invoiceLines'
import { InvoiceForm } from './InvoiceForm'

const clients = [
  { id: 'client-1', companyName: 'Acme Corp', email: 'billing@acme.test' },
  { id: 'client-2', companyName: 'Globex', email: 'ap@globex.test' },
]

function emptyValues(): InvoiceFormValues {
  return { clientId: '', issueDate: '2026-09-01', dueDate: '2026-10-01', currency: '', notes: '', items: [newLine()] }
}

function renderForm(overrides: Partial<Parameters<typeof InvoiceForm>[0]> = {}) {
  const onSubmit = vi.fn()
  render(
    <InvoiceForm
      mode="create"
      title="New invoice"
      clients={clients}
      initialValues={emptyValues()}
      submitting={false}
      error={null}
      onSubmit={onSubmit}
      onCancel={vi.fn()}
      {...overrides}
    />,
  )
  return { onSubmit }
}

describe('calculateTotals', () => {
  it('rounds each line and its tax to cents, like the API', () => {
    const line = { ...newLine(), quantity: '3', unitPrice: '0.335', taxPercent: '10' }

    expect(calculateTotals([line])).toEqual({ subTotal: 1.01, taxTotal: 0.1, grandTotal: 1.11 })
  })
})

describe('InvoiceForm', () => {
  it('flags missing fields instead of submitting', async () => {
    const user = userEvent.setup()
    const { onSubmit } = renderForm()

    await user.click(screen.getByRole('button', { name: 'Create draft' }))

    expect(onSubmit).not.toHaveBeenCalled()
    expect(screen.getByText('Choose who this invoice is for.')).toBeInTheDocument()
    expect(screen.getByText('Use a 3-letter currency code, like AUD or USD.')).toBeInTheDocument()
    expect(screen.getByText('Describe the work or product.')).toBeInTheDocument()
    expect(screen.getByText('Enter a price of 0 or more.')).toBeInTheDocument()
  })

  it('rejects a due date before the issue date', async () => {
    const user = userEvent.setup()
    const { onSubmit } = renderForm({
      initialValues: { ...emptyValues(), clientId: 'client-1', currency: 'USD', dueDate: '2026-08-01' },
    })

    await user.click(screen.getByRole('button', { name: 'Create draft' }))

    expect(onSubmit).not.toHaveBeenCalled()
    expect(screen.getByText('The due date can’t be before the issue date.')).toBeInTheDocument()
  })

  it('defaults the currency to the chosen client’s preferred currency', async () => {
    const user = userEvent.setup()
    const onClientChange = vi.fn().mockResolvedValue('EUR')
    renderForm({ onClientChange })

    await user.selectOptions(screen.getByLabelText('Client *'), 'client-2')

    await waitFor(() => expect(screen.getByLabelText('Currency *')).toHaveValue('EUR'))
    expect(onClientChange).toHaveBeenCalledWith('client-2')
  })

  it('does not overwrite a currency the user already typed', async () => {
    const user = userEvent.setup()
    const onClientChange = vi.fn().mockResolvedValue('EUR')
    renderForm({ onClientChange })

    await user.type(screen.getByLabelText('Currency *'), 'aud')
    await user.selectOptions(screen.getByLabelText('Client *'), 'client-2')

    expect(screen.getByLabelText('Currency *')).toHaveValue('AUD')
    expect(onClientChange).not.toHaveBeenCalled()
  })

  it('adds and removes line items and shows a running total', async () => {
    const user = userEvent.setup()
    const { onSubmit } = renderForm({ initialValues: { ...emptyValues(), clientId: 'client-1', currency: 'USD' } })

    await user.type(screen.getByLabelText('Description (Line 1)'), 'Design')
    await user.type(screen.getByLabelText('Unit price (Line 1)'), '100')
    await user.click(screen.getByRole('button', { name: 'Add line' }))
    await user.type(screen.getByLabelText('Description (Line 2)'), 'Hosting')
    await user.clear(screen.getByLabelText('Quantity (Line 2)'))
    await user.type(screen.getByLabelText('Quantity (Line 2)'), '2')
    await user.type(screen.getByLabelText('Unit price (Line 2)'), '25')
    await user.clear(screen.getByLabelText('Tax % (Line 2)'))
    await user.type(screen.getByLabelText('Tax % (Line 2)'), '10')

    // Compared as raw text: Intl output can contain a non-breaking space that toHaveTextContent normalises away.
    expect(screen.getByText('Total').nextElementSibling?.textContent).toBe(formatMoney(155, 'USD'))

    await user.click(screen.getByRole('button', { name: 'Create draft' }))

    expect(onSubmit).toHaveBeenCalledWith(
      expect.objectContaining({
        clientId: 'client-1',
        items: [
          expect.objectContaining({ description: 'Design', quantity: '1', unitPrice: '100', taxPercent: '0' }),
          expect.objectContaining({ description: 'Hosting', quantity: '2', unitPrice: '25', taxPercent: '10' }),
        ],
      }),
    )
  })

  it('keeps at least one line', () => {
    renderForm()

    expect(screen.getByRole('button', { name: 'Remove line 1' })).toBeDisabled()
  })
})

describe('InvoiceForm focus', () => {
  it('moves focus to the first invalid field after a failed submit', async () => {
    const user = userEvent.setup()
    renderForm({ initialValues: { ...emptyValues(), clientId: 'client-1', currency: 'USD' } })

    await user.click(screen.getByRole('button', { name: 'Create draft' }))

    await waitFor(() => expect(screen.getByLabelText('Description (Line 1)')).toHaveFocus())
  })
})
