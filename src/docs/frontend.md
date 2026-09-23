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

1. The SPA posts credentials to `InvoicingBff` (`/src/InvoicingBff/InvoicingBff`), not to InvoicingApi directly.
2. The BFF calls InvoicingApi's `/auth/login`, and on success stores the returned JWT server-side as a
   claim inside an httpOnly, `SameSite=Strict` session cookie (`InvoicingBff.Auth`). The response the
   browser sees carries only the cookie and the username — never the token.
3. Subsequent BFF endpoints read the JWT off the authenticated `ClaimsPrincipal` to call InvoicingApi on
   the user's behalf. Nothing in `/src/web` ever handles a bearer token.
4. `GET /bff/session` lets the SPA check whether it's logged in on load; `POST /bff/logout` clears the cookie.

This exists to keep the JWT out of `localStorage`/JS-reachable storage (see `security.md`'s XSS notes).

### Dev server proxy — why there's no CORS config

`vite.config.ts` proxies `/bff/*` to the BFF's dev URL (`https://localhost:7180`). From the browser's
perspective every request stays on `https://localhost:5173`, so the BFF's cookie is same-origin and no
CORS policy is needed, in dev or otherwise. In production the BFF is expected to also serve the built
SPA (single origin), for the same reason. If a feature ever needs the SPA and BFF on genuinely different
origins, that's the point to add an explicit CORS allow-list — never a wildcard combined with credentials.

### Ports

All dev endpoints are HTTPS, using the ASP.NET Core dev certificate exported to `~/.aspnet/https` by
`make certs`. The docker compose services and the Rider `https` launch profiles share these ports.

- `InvoicingApi`: `https://localhost:7073` (see `src/Api/InvoicingApi/Properties/launchSettings.json`)
- `InvoicingBff`: `https://localhost:7180` (see `src/InvoicingBff/InvoicingBff/Properties/launchSettings.json`)
- Vite dev server: `https://localhost:5173` (Vite's default — this is the one the browser talks to).
  `vite.config.ts` loads the same certificate and fails fast with a hint if it's missing; builds don't need it.

## Testing

- Framework: Vitest (`jsdom` environment) + React Testing Library, configured in `vitest.config.ts`.
- Test files sit next to the source they cover: `Foo.tsx` → `Foo.test.tsx`, `fooApi.ts` → `fooApi.test.ts`.
- `src/test/setup.ts` wires up `@testing-library/jest-dom` matchers and RTL's `cleanup` after each test.
- Mock `fetch` with `vi.stubGlobal('fetch', vi.fn())` rather than mocking the `api/` module's internals —
  it exercises the same request/response contract the BFF actually returns.
- Run with `npm test` (`cd src/web`) or `make test-web` from the repo root.