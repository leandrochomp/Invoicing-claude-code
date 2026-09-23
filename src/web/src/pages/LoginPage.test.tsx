import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { login } from '../api/authApi'
import { ApiError } from '../api/http'
import { LoginPage } from './LoginPage'

vi.mock('../api/authApi', () => ({
  login: vi.fn(),
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

  it('shows the ApiError message and does not call onLoggedIn on failure', async () => {
    vi.mocked(login).mockRejectedValue(new ApiError('Invalid credentials.', 401))
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

  it('shows inline errors instead of calling the API when fields are empty', async () => {
    const user = userEvent.setup()

    render(<LoginPage onLoggedIn={vi.fn()} />)

    await user.click(screen.getByRole('button', { name: 'Sign in' }))

    expect(login).not.toHaveBeenCalled()
    expect(screen.getByLabelText('Username')).toHaveAccessibleDescription('Enter your username.')
    expect(screen.getByLabelText('Password')).toHaveAccessibleDescription('Enter your password.')
    expect(screen.getByLabelText('Username')).toHaveFocus()
  })

  it('toggles password visibility', async () => {
    const user = userEvent.setup()

    render(<LoginPage onLoggedIn={vi.fn()} />)

    expect(screen.getByLabelText('Password')).toHaveAttribute('type', 'password')
    await user.click(screen.getByRole('button', { name: 'Show password' }))
    expect(screen.getByLabelText('Password')).toHaveAttribute('type', 'text')
    expect(screen.getByRole('button', { name: 'Hide password' })).toBeInTheDocument()
  })

  it('keeps the username but clears and focuses the password after a failed sign-in', async () => {
    vi.mocked(login).mockRejectedValue(new ApiError('Invalid credentials.', 401))
    const user = userEvent.setup()

    render(<LoginPage onLoggedIn={vi.fn()} />)

    await user.type(screen.getByLabelText('Username'), 'alice')
    await user.type(screen.getByLabelText('Password'), 'wrong')
    await user.click(screen.getByRole('button', { name: 'Sign in' }))

    await screen.findByRole('alert')
    expect(screen.getByLabelText('Username')).toHaveValue('alice')
    expect(screen.getByLabelText('Password')).toHaveValue('')
    expect(screen.getByLabelText('Password')).toHaveFocus()
  })
})
