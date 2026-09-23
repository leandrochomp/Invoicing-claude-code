import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { useState } from 'react'
import { describe, expect, it, vi } from 'vitest'
import { DatePicker } from './DatePicker'

function renderPicker(initial: string, min?: string) {
  const onChange = vi.fn()
  function Harness() {
    const [value, setValue] = useState(initial)
    return (
      <>
        <label htmlFor="due">Due date</label>
        <DatePicker
          id="due"
          value={value}
          min={min}
          onChange={(next) => {
            setValue(next)
            onChange(next)
          }}
        />
      </>
    )
  }
  render(<Harness />)
  return { onChange, input: screen.getByLabelText('Due date') }
}

describe('DatePicker', () => {
  it('shows the value day-first', () => {
    const { input } = renderPicker('2026-10-03')

    expect(input).toHaveValue('03/10/2026')
  })

  it('reads a typed date as day/month/year', async () => {
    const { input, onChange } = renderPicker('')

    await userEvent.type(input, '3/10/2026')

    expect(onChange).toHaveBeenLastCalledWith('2026-10-03')
    expect(input).toHaveValue('3/10/2026')
  })

  it('keeps a half-typed date in the field while reporting it as empty', async () => {
    const { input, onChange } = renderPicker('2026-10-03')

    await userEvent.clear(input)
    await userEvent.type(input, '15/1')

    expect(onChange).toHaveBeenLastCalledWith('')
    expect(input).toHaveValue('15/1')
  })

  it('tidies a typed date when the field loses focus', async () => {
    const { input } = renderPicker('')

    await userEvent.type(input, '3/1/26')
    await userEvent.tab()

    expect(input).toHaveValue('03/01/2026')
  })

  it('picks a date from the calendar', async () => {
    const { input, onChange } = renderPicker('2026-10-03')

    await userEvent.click(screen.getByRole('button', { name: 'Choose date' }))
    const calendar = screen.getByRole('dialog', { name: 'Calendar' })
    await userEvent.click(calendar.querySelector('[data-day="2026-10-15"] button')!)

    expect(onChange).toHaveBeenLastCalledWith('2026-10-15')
    expect(input).toHaveValue('15/10/2026')
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
  })

  it('starts the calendar week on Monday', async () => {
    renderPicker('2026-10-03')

    await userEvent.click(screen.getByRole('button', { name: 'Choose date' }))
    const calendar = screen.getByRole('dialog', { name: 'Calendar' })

    expect(calendar.querySelector('.rdp-weekday')).toHaveAttribute('aria-label', 'Monday')
  })

  it('disables days before the minimum', async () => {
    renderPicker('2026-10-03', '2026-10-02')

    await userEvent.click(screen.getByRole('button', { name: 'Choose date' }))
    const calendar = screen.getByRole('dialog', { name: 'Calendar' })

    expect(calendar.querySelector('[data-day="2026-10-01"] button')).toBeDisabled()
    expect(calendar.querySelector('[data-day="2026-10-02"] button')).toBeEnabled()
  })

  it('closes the calendar on Escape and returns focus to the button', async () => {
    renderPicker('2026-10-03')
    const toggle = screen.getByRole('button', { name: 'Choose date' })

    await userEvent.click(toggle)
    await userEvent.keyboard('{Escape}')

    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    expect(toggle).toHaveFocus()
  })
})
