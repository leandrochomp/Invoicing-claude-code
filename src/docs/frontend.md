# Frontend

- Functional components only.
- API calls in `/src/web/src/api/`, one file per resource.
- No state library unless asked. Use React state + context.
- TypeScript strict mode.

## Stack

- Vite + React + TypeScript (`npm create vite@latest -- --template react-ts`), scaffolded at `/src/web`.
- No routing library, form library, or CSS framework unless a feature actually needs one.

## Auth flow (BFF pattern)

The SPA never sees or stores the InvoicingApi JWT. Instead:

1. The SPA posts credentials to `InvoicingBff` (`/src/Web/InvoicingBff`), not to InvoicingApi directly.
2. The BFF calls InvoicingApi's `/auth/login`, and on success stores the returned JWT server-side as a
   claim inside an httpOnly, `SameSite=Strict` session cookie (`InvoicingBff.Auth`). The response the
   browser sees carries only the cookie and the username — never the token.
3. Subsequent BFF endpoints read the JWT off the authenticated `ClaimsPrincipal` to call InvoicingApi on
   the user's behalf. Nothing in `/src/web` ever handles a bearer token.
4. `GET /bff/session` lets the SPA check whether it's logged in on load; `POST /bff/logout` clears the cookie.

This exists to keep the JWT out of `localStorage`/JS-reachable storage (see `security.md`'s XSS notes).

### Dev server proxy — why there's no CORS config

`vite.config.ts` proxies `/bff/*` to the BFF's dev URL (`http://localhost:5180`). From the browser's
perspective every request stays on `http://localhost:5173`, so the BFF's cookie is same-origin and no
CORS policy is needed, in dev or otherwise. In production the BFF is expected to also serve the built
SPA (single origin), for the same reason. If a feature ever needs the SPA and BFF on genuinely different
origins, that's the point to add an explicit CORS allow-list — never a wildcard combined with credentials.

### Ports

- `InvoicingApi`: `http://localhost:5112` (see `src/Api/InvoicingApi/Properties/launchSettings.json`)
- `InvoicingBff`: `http://localhost:5180` (see `src/Web/InvoicingBff/Properties/launchSettings.json`)
- Vite dev server: `http://localhost:5173` (Vite's default — this is the one the browser talks to)

## Known gaps / roadmap

- **No frontend test framework yet.** `LoginPage` and its `api/` client have no unit/component-level
  tests — only the BFF endpoints they talk to are covered (`tests/InvoicingBff.Tests`). Options (Vitest,
  Jest, React Testing Library, Playwright for e2e, etc.) haven't been evaluated. **Next task**: pick one
  and wire it in before the next component lands.