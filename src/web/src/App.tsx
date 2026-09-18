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
        <p>Signed in as {username}.</p>
        <button type="button" onClick={handleLogout}>
          Sign out
        </button>
      </header>
      <ClientsPage />
    </div>
  )
}

export default App
