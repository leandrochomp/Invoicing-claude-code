import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { LoginError, login } from '../api/authApi'
import { LoginPage } from './LoginPage'

vi.mock('../api/authApi', () => ({
  login: vi.fn(),
  LoginError: class LoginError extends Error {},
}))

describe('LoginPage', () => {
  afterEach(() => {
    vi.clearAllMocks()
  })

  it('submits the entered credentials and calls onLoggedIn on success', async () => {
    vi.mocked(login).mockResolvedValue(undefined)
    const onLoggedIn = vi.fn()
    const user = userEvent.setup()

    render(<LoginPage onLoggedIn={onLoggedIn} />)

    await user.type(screen.getByLabelText('Username'), 'alice')
    await user.type(screen.getByLabelText('Password'), 'secret')
    await user.click(screen.getByRole('button', { name: 'Sign in' }))

    expect(login).toHaveBeenCalledWith({ username: 'alice', password: 'secret' })
    await waitFor(() => expect(onLoggedIn).toHaveBeenCalledTimes(1))
  })

  it('shows the LoginError message and does not call onLoggedIn on failure', async () => {
    vi.mocked(login).mockRejectedValue(new LoginError('Invalid credentials.'))
    const onLoggedIn = vi.fn()
    const user = userEvent.setup()

    render(<LoginPage onLoggedIn={onLoggedIn} />)

    await user.type(screen.getByLabelText('Username'), 'alice')
    await user.type(screen.getByLabelText('Password'), 'wrong')
    await user.click(screen.getByRole('button', { name: 'Sign in' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Invalid credentials.')
    expect(onLoggedIn).not.toHaveBeenCalled()
  })

  it('shows a generic message for unexpected errors', async () => {
    vi.mocked(login).mockRejectedValue(new Error('network down'))
    const user = userEvent.setup()

    render(<LoginPage onLoggedIn={vi.fn()} />)

    await user.type(screen.getByLabelText('Username'), 'alice')
    await user.type(screen.getByLabelText('Password'), 'secret')
    await user.click(screen.getByRole('button', { name: 'Sign in' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Something went wrong. Please try again.')
  })

  it('disables the submit button while submitting', async () => {
    let resolveLogin: () => void = () => {}
    vi.mocked(login).mockReturnValue(
      new Promise<void>((resolve) => {
        resolveLogin = resolve
      }),
    )
    const user = userEvent.setup()

    render(<LoginPage onLoggedIn={vi.fn()} />)

    await user.type(screen.getByLabelText('Username'), 'alice')
    await user.type(screen.getByLabelText('Password'), 'secret')
    await user.click(screen.getByRole('button', { name: 'Sign in' }))

    expect(screen.getByRole('button', { name: 'Signing in…' })).toBeDisabled()

    resolveLogin()
    await waitFor(() => expect(screen.getByRole('button', { name: 'Sign in' })).not.toBeDisabled())
  })
})
