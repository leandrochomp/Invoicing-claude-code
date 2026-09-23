import { useEffect, useState } from 'react'
import { getSession, logout } from './api/authApi'
import type { Route } from './lib/route'
import { useRoute } from './lib/route'
import { ClientsPage } from './pages/ClientsPage'
import { HomePage } from './pages/HomePage'
import { InvoicesPage } from './pages/InvoicesPage'
import { LoginPage } from './pages/LoginPage'
import { PaymentsPage } from './pages/PaymentsPage'

const navItems: { page: Route['page']; label: string; href: string }[] = [
  { page: 'home', label: 'Home', href: '#/' },
  { page: 'invoices', label: 'Invoices', href: '#/invoices' },
  { page: 'payments', label: 'Payments', href: '#/payments' },
  { page: 'clients', label: 'Clients', href: '#/clients' },
]

function CurrentPage({ route, username }: { route: Route; username: string }) {
  switch (route.page) {
    case 'invoices':
      return <InvoicesPage route={route} />
    case 'payments':
      return <PaymentsPage route={route} />
    case 'clients':
      return <ClientsPage />
    default:
      return <HomePage username={username} />
  }
}

function App() {
  const [username, setUsername] = useState<string | null>(null)
  const [checkingSession, setCheckingSession] = useState(true)
  const route = useRoute()

  useEffect(() => {
    getSession()
      .then((session) => setUsername(session?.username ?? null))
      .finally(() => setCheckingSession(false))
  }, [])

  async function handleLogout() {
    await logout()
    setUsername(null)
  }

  if (checkingSession) {
    return null
  }

  if (!username) {
    return <LoginPage onLoggedIn={() => getSession().then((session) => setUsername(session?.username ?? null))} />
  }

  return (
    <div className="app-shell">
      <header className="app-header">
        <a className="brand" href="#/">
          Invoicing
        </a>
        <nav className="app-nav" aria-label="Main">
          {navItems.map((item) => (
            <a key={item.page} href={item.href} aria-current={route.page === item.page ? 'page' : undefined}>
              {item.label}
            </a>
          ))}
        </nav>
        <div className="app-header-user">
          <span>
            Signed in as <strong>{username}</strong>
          </span>
          <button type="button" className="button-secondary button-small" onClick={handleLogout}>
            Sign out
          </button>
        </div>
      </header>
      <div className="app-content">
        <CurrentPage route={route} username={username} />
      </div>
    </div>
  )
}

export default App
