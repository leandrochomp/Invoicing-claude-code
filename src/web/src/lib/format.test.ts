import { describe, expect, it } from 'vitest'
import { addDays, formatDate, formatDateField, formatMoney, fromDateInput, parseDateField, roundMoney, toDateInput } from './format'

describe('dates', () => {
  it('keeps the calendar day of an API timestamp regardless of its offset', () => {
    expect(toDateInput('2026-09-01T00:00:00+10:00')).toBe('2026-09-01')
    expect(fromDateInput('2026-09-01')).toBe('2026-09-01T00:00:00Z')
  })

  it('formats the calendar day without shifting it into the viewer’s time zone', () => {
    expect(formatDate('2026-01-01T00:00:00+00:00')).toContain('2026')
    expect(formatDate('2026-01-01T00:00:00+00:00')).toMatch(/\b1\b/)
  })

  it('adds days across month ends', () => {
    expect(addDays('2026-01-31', 30)).toBe('2026-03-02')
  })

  it('shows date fields day-first, Australian style', () => {
    expect(formatDateField('2026-10-03')).toBe('03/10/2026')
    expect(formatDateField('')).toBe('')
  })

  it('reads typed dates day-first, not US month-first', () => {
    expect(parseDateField('03/10/2026')).toBe('2026-10-03')
    expect(parseDateField('3/10/26')).toBe('2026-10-03')
    expect(parseDateField(' 3-10-2026 ')).toBe('2026-10-03')
    expect(parseDateField('25.12.2026')).toBe('2026-12-25')
  })

  it('rejects typed dates that are incomplete or not on the calendar', () => {
    expect(parseDateField('03/10/202')).toBe('')
    expect(parseDateField('10/13/2026')).toBe('')
    expect(parseDateField('31/02/2026')).toBe('')
    expect(parseDateField('29/02/2028')).toBe('2028-02-29')
    expect(parseDateField('2026-10-03')).toBe('')
  })
})

describe('money', () => {
  it('rounds half away from zero like the API', () => {
    expect(roundMoney(0.125)).toBe(0.13)
    expect(roundMoney(1.005)).toBe(1.01)
    expect(roundMoney(-0.125)).toBe(-0.13)
    expect(roundMoney(1e-7)).toBe(0)
  })

  it('still shows an amount for an unrecognised currency code', () => {
    expect(formatMoney(12.5, 'ZZ')).toBe('12.50 ZZ')
  })

  it('shows a plain 2-decimal number when there is no currency yet', () => {
    expect(formatMoney(12.5, null)).toBe('12.50')
    expect(formatMoney(3, '')).toBe('3.00')
  })
})
