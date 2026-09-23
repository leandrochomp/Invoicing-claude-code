import { describe, expect, it } from 'vitest'
import { addDays, formatDate, formatMoney, fromDateInput, roundMoney, toDateInput } from './format'

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
})
