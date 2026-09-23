import { useEffect, useState } from 'react'

// Hash-based routes keep the current page across refreshes and make the browser's back button work
// without a routing library. Every link in the app is a plain `href="#/..."`.
export type Route =
  | { page: 'home' }
  | { page: 'clients' }
  | { page: 'invoices'; view: 'list' }
  | { page: 'invoices'; view: 'new' }
  | { page: 'invoices'; view: 'detail'; id: string }
  | { page: 'invoices'; view: 'edit'; id: string }
  | { page: 'payments'; view: 'list' }
  | { page: 'payments'; view: 'new'; invoiceId?: string }

export function parseRoute(hash: string): Route {
  const [path, queryString = ''] = hash.replace(/^#/, '').split('?')
  const query = new URLSearchParams(queryString)
  const segments = path.split('/').filter(Boolean)

  switch (segments[0]) {
    case 'clients':
      return { page: 'clients' }
    case 'invoices':
      if (segments[1] === 'new') {
        return { page: 'invoices', view: 'new' }
      }
      if (segments[1]) {
        return segments[2] === 'edit'
          ? { page: 'invoices', view: 'edit', id: segments[1] }
          : { page: 'invoices', view: 'detail', id: segments[1] }
      }
      return { page: 'invoices', view: 'list' }
    case 'payments':
      return segments[1] === 'new'
        ? { page: 'payments', view: 'new', invoiceId: query.get('invoiceId') ?? undefined }
        : { page: 'payments', view: 'list' }
    default:
      return { page: 'home' }
  }
}

export function navigate(path: string) {
  window.location.hash = path
}

export function useRoute(): Route {
  const [route, setRoute] = useState(() => parseRoute(window.location.hash))

  useEffect(() => {
    const onChange = () => {
      setRoute(parseRoute(window.location.hash))
      window.scrollTo?.(0, 0)
    }
    window.addEventListener('hashchange', onChange)
    return () => window.removeEventListener('hashchange', onChange)
  }, [])

  return route
}
