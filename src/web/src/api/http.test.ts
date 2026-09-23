import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError, errorMessage, requestJson, requestNoContent, withQuery } from './http'

beforeEach(() => {
  vi.stubGlobal('fetch', vi.fn())
})

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('withQuery', () => {
  it('drops empty parameters', () => {
    expect(withQuery('/bff/invoices', { clientId: undefined, status: 'Sent', page: 2, notes: '', other: null })).toBe(
      '/bff/invoices?status=Sent&page=2',
    )
  })

  it('returns the bare path when there is nothing to add', () => {
    expect(withQuery('/bff/payments', {})).toBe('/bff/payments')
  })
})

describe('requestJson', () => {
  it('prefers a ProblemDetails detail over its title', async () => {
    vi.mocked(fetch).mockResolvedValue(
      new Response(JSON.stringify({ title: 'Conflict', detail: 'Cannot record a payment on a draft or voided invoice.' }), { status: 409 }),
    )

    const error = (await requestJson('/bff/anything').catch((err: unknown) => err)) as ApiError

    expect(error).toBeInstanceOf(ApiError)
    expect(error.status).toBe(409)
    expect(error.message).toBe('Cannot record a payment on a draft or voided invoice.')
  })

  it('falls back to a generic message when the body is not JSON', async () => {
    vi.mocked(fetch).mockResolvedValue(new Response('oops', { status: 500 }))

    await expect(requestJson('/bff/anything')).rejects.toThrow('The request failed.')
  })
})

describe('requestNoContent', () => {
  it('resolves on a 204', async () => {
    vi.mocked(fetch).mockResolvedValue(new Response(null, { status: 204 }))

    await expect(requestNoContent('/bff/anything', { method: 'DELETE' })).resolves.toBeUndefined()
  })
})

describe('errorMessage', () => {
  it('uses the ApiError message and the fallback for anything else', () => {
    expect(errorMessage(new ApiError('Nope.', 400), 'Fallback')).toBe('Nope.')
    expect(errorMessage(new TypeError('network'), 'Fallback')).toBe('Fallback')
  })
})
