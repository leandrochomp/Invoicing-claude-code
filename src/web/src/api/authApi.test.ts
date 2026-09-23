import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { getSession, login, logout } from './authApi'
import { ApiError } from './http'

describe('login', () => {
  beforeEach(() => {
    vi.stubGlobal('fetch', vi.fn())
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('posts credentials to /bff/login with cookies included', async () => {
    vi.mocked(fetch).mockResolvedValue(new Response(null, { status: 200 }))

    await login({ username: 'alice', password: 'secret' })

    expect(fetch).toHaveBeenCalledWith('/bff/login', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include',
      body: JSON.stringify({ username: 'alice', password: 'secret' }),
    })
  })

  it('throws an ApiError with the server-provided message and status on failure', async () => {
    vi.mocked(fetch).mockImplementation(
      async () => new Response(JSON.stringify({ title: 'Invalid credentials.' }), { status: 401 }),
    )

    const error = await login({ username: 'alice', password: 'wrong' }).catch((err) => err)

    expect(error).toBeInstanceOf(ApiError)
    expect(error.message).toBe('Invalid credentials.')
    expect(error.status).toBe(401)
  })

  it('falls back to a generic message when the error body is not JSON', async () => {
    vi.mocked(fetch).mockResolvedValue(new Response('not json', { status: 500 }))

    await expect(login({ username: 'alice', password: 'wrong' })).rejects.toThrow('The request failed.')
  })

  it('joins validation errors from the BFF into the message', async () => {
    vi.mocked(fetch).mockResolvedValue(
      new Response(JSON.stringify({ errors: { Username: ["'Username' must not be empty."] } }), { status: 400 }),
    )

    await expect(login({ username: '', password: 'secret' })).rejects.toThrow("'Username' must not be empty.")
  })
})

describe('logout', () => {
  beforeEach(() => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(null, { status: 200 })))
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('posts to /bff/logout with cookies included', async () => {
    await logout()

    expect(fetch).toHaveBeenCalledWith('/bff/logout', { method: 'POST', credentials: 'include' })
  })
})

describe('getSession', () => {
  beforeEach(() => {
    vi.stubGlobal('fetch', vi.fn())
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('returns null when the session responds 401', async () => {
    vi.mocked(fetch).mockResolvedValue(new Response(null, { status: 401 }))

    await expect(getSession()).resolves.toBeNull()
  })

  it('returns the parsed session on success', async () => {
    vi.mocked(fetch).mockResolvedValue(new Response(JSON.stringify({ username: 'alice' }), { status: 200 }))

    await expect(getSession()).resolves.toEqual({ username: 'alice' })
  })

  it('throws for other non-ok responses', async () => {
    vi.mocked(fetch).mockResolvedValue(new Response(null, { status: 500 }))

    await expect(getSession()).rejects.toThrow('Failed to load session.')
  })
})
