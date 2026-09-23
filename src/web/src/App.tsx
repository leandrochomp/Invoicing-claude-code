import { useEffect, useState } from 'react'
import { getSession, logout } from './api/authApi'
import type { Route } from './lib/route'
import { useRoute } from './lib/route'
import { ClientsPage } from './pages/ClientsPage'
import { HomePage } from './pages/HomePage'
import { InvoicesPage } from './pages/InvoicesPage'
import { LoginPage } from './pages/LoginPage'
import { PaymentsPage } from './pages/PaymentsPage'

// 24x24 stroke icons, drawn inline so the sidebar needs no icon dependency.
const navItems: { page: Route['page']; label: string; href: string; icon: string }[] = [
  { page: 'home', label: 'Home', href: '#/', icon: 'M3 10.5 12 3l9 7.5V20a1 1 0 0 1-1 1h-5v-6H9v6H4a1 1 0 0 1-1-1z' },
  { page: 'invoices', label: 'Invoices', href: '#/invoices', icon: 'M14 3H6a1 1 0 0 0-1 1v16a1 1 0 0 0 1 1h12a1 1 0 0 0 1-1V8zM14 3v5h5M9 13h6M9 17h6' },
  { page: 'payments', label: 'Payments', href: '#/payments', icon: 'M3 6h18v12H3zM3 10h18M7 15h3' },
  { page: 'clients', label: 'Clients', href: '#/clients', icon: 'M16 20v-1a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v1M9 11a4 4 0 1 0 0-8 4 4 0 0 0 0 8M22 20v-1a4 4 0 0 0-3-3.87M16 3.13a4 4 0 0 1 0 7.75' },
]

function NavIcon({ path }: { path: string }) {
  return (
    <svg viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" strokeWidth="1.75" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
      <path d={path} />
    </svg>
  )
}

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
  // On narrow screens the sidebar is a drawer. Remembering the route it was opened on means any
  // navigation (a new route object) closes it.
  const [menuOpenOn, setMenuOpenOn] = useState<Route | null>(null)
  const menuOpen = menuOpenOn === route

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
      <aside className="sidebar" data-open={menuOpen}>
        <div className="sidebar-top">
          <a className="brand" href="#/">
            Invoicing
          </a>
          <button
            type="button"
            className="button-ghost sidebar-toggle"
            aria-expanded={menuOpen}
            aria-controls="sidebar-menu"
            onClick={() => setMenuOpenOn(menuOpen ? null : route)}
          >
            {menuOpen ? 'Close' : 'Menu'}
          </button>
        </div>
        <div id="sidebar-menu" className="sidebar-menu">
          <nav className="sidebar-nav" aria-label="Main">
            {navItems.map((item) => (
              <a key={item.page} href={item.href} aria-current={route.page === item.page ? 'page' : undefined}>
                <NavIcon path={item.icon} />
                {item.label}
              </a>
            ))}
          </nav>
          <div className="sidebar-user">
            <span>
              Signed in as <strong>{username}</strong>
            </span>
            <button type="button" className="button-secondary button-small" onClick={handleLogout}>
              Sign out
            </button>
          </div>
        </div>
      </aside>
      <div className="app-content">
        <CurrentPage route={route} username={username} />
      </div>
    </div>
  )
}

export default App
