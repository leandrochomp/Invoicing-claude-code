import { requestNoContent } from './http'

export interface LoginRequest {
  username: string
  password: string
}

export interface Session {
  username: string
}

export function login(request: LoginRequest): Promise<void> {
  return requestNoContent('/bff/login', { method: 'POST', body: JSON.stringify(request) })
}

export async function logout(): Promise<void> {
  await fetch('/bff/logout', { method: 'POST', credentials: 'include' })
}

export async function getSession(): Promise<Session | null> {
  const response = await fetch('/bff/session', { credentials: 'include' })
  if (response.status === 401) {
    return null
  }
  if (!response.ok) {
    throw new Error('Failed to load session.')
  }
  return (await response.json()) as Session
}
