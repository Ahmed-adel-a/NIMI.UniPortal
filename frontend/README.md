# NIMI UniPortal — web app

One web app for every role: students, teaching staff, department heads and Student Affairs. Arabic (right-to-left)
by default, with an English switch; light, dark or the system theme.

React + Vite + TypeScript, Tailwind CSS, React Router, TanStack Query, i18next. API types are generated from the
backend's OpenAPI document, so the app can't drift from the API.

## Run it

You need Node 22 and a running backend.

1. Start the backend. Either:
   - Docker (no .NET needed): `docker compose up -d` in `deployments/frontend-dev` (API on http://localhost:8080
     with a demo campus), or
   - the .NET host: `dotnet run --project src/Hosts/Host.CairoUniversity`, then set `VITE_API_URL` in
     `frontend/.env.local` to its URL (see `.env.example`).
2. Start the app:

   ```
   cd frontend
   npm install
   npm run dev
   ```

   Open http://localhost:5173. The dev server forwards `/api` to the backend, so there is nothing to configure for CORS.

**Accounts:** the demo accounts in `deployments/frontend-dev/README.md`, or your own test accounts: put
`TEST_NATIONAL_ID` and `TEST_PASSWORD` in `deployments/frontend-dev/.env` (Docker) or set
`Seed:TestAccount:NationalId` / `Seed:TestAccount:Password` in the host's user secrets (dotnet run). Sign in with the
national ID; the app asks which role to use.

## Scripts

| Command | What it does |
|---------|--------------|
| `npm run dev` | Development server on port 5173 |
| `npm test` | Unit tests (Vitest + Testing Library) |
| `npm run lint` / `npm run typecheck` | ESLint / TypeScript |
| `npm run build` | Production build in `dist/` |
| `npm run api:fetch` then `npm run api:types` | Refresh `openapi.json` from a running backend and regenerate `src/api/schema.d.ts` |

## Layout

```
src/
  api/        client (tokens, refresh, errors), generated schema types
  auth/       session context, route guards, role homes
  i18n/       Arabic + English texts (keys must match; a test checks)
  layout/     the app frame: sidebar per role, top bar, theme
  components/ small UI pieces (buttons, fields, cards, tables, states)
  pages/      auth/, common/, student/, teaching/ (class tabs), head/, admin/ (Student Affairs; loaded on first use)
  lib/        formatting (dates, money, Saturday-based weeks), device id
```

Sessions: the access token is refreshed shortly before it expires, and only one refresh runs at a time (the
refresh token is single-use). A temporary password always leads to the change-password page.
