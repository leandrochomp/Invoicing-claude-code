import { useEffect, useState } from 'react'
import { getSession, logout } from './api/authApi'
import { ClientsPage } from './pages/ClientsPage'
import { LoginPage } from './pages/LoginPage'

function App() {
  const [username, setUsername] = useState<string | null>(null)
  const [checkingSession, setCheckingSession] = useState(true)

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
        <span className="brand">Invoicing</span>
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
        <ClientsPage />
      </div>
    </div>
  )
}

export default App
