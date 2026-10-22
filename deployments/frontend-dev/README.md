# Backend for frontend development

The whole backend runs in Docker: the API image (Cairo University: core modules + CreditHours + Forum + Events) and PostgreSQL 18.
You don't need .NET, PostgreSQL or anything else, only **Docker Desktop**.

## Start

```bash
docker compose up -d
```

Run it in this folder. The first start pulls the images, creates the database and fills a demo campus, which takes about a minute.

| What | Where |
|------|-------|
| API | http://localhost:8080 |
| API docs (try every endpoint) | http://localhost:8080/docs |
| OpenAPI document (generate a typed client from it) | http://localhost:8080/openapi/v1.json |
| Health | http://localhost:8080/api/health/db |

The very first start logs a few `Failed executing DbCommand … __ef_migrations_history` lines. That is the API
finding an empty database before it creates the tables, so they are expected. `docker compose logs api` should end with
`Demo campus seeded` and `Now listening on: http://[::]:8080`.

### Other commands

| Task | Command |
|------|---------|
| Get the newest backend | `docker compose pull && docker compose up -d` |
| Logs | `docker compose logs -f api` |
| Stop (keeps the data) | `docker compose down` |
| Start again from a fresh demo campus | `docker compose down -v && docker compose up -d` |

The image is published on every merge to the backend's `dev` branch. If `pull` says "denied", the package is still private:
the backend owner either makes it public in GitHub (Packages → nimi-uniportal-cairo → Package settings → Change
visibility) or gives you read access. In that case, sign in once with `docker login ghcr.io` (a GitHub token with `read:packages`).

## Demo accounts

Every account signs in with the password **`Demo@2026`** (override it with `DEMO_PASSWORD` in a `.env` file before the first start).

| Role | Email | Notes |
|------|-------|-------|
| Student Affairs (admin) | `affairs@demo.cu.edu.eg` | Everything under `/api/admin/...` |
| Professor | `hassan.mahmoud@demo.cu.edu.eg` | Teaches CSE101 (both groups) and CSE301 |
| Professor | `mona.adel@demo.cu.edu.eg` | CSE211 + the CSE101 lab |
| Professor | `karim.fathy@demo.cu.edu.eg`, `salma.nabil@demo.cu.edu.eg` | EPE201, ACC101 |
| Student | `omar.khaled@demo.cu.edu.eg` | Computer Engineering. Registered in CSE101 + CSE211, tuition paid |
| Student | `mariam.ahmed@`, `youssef.ibrahim@`, `nour.hussein@` (all `demo.cu.edu.eg`) | Computer Engineering |
| Student | `ahmed.samir@`, `heba.mostafa@` | Electrical Power Engineering |
| Student | `sara.ali@`, `mohamed.tarek@` | Accounting (Faculty of Commerce) |
| Student | `new.student@demo.cu.edu.eg` | Must change the password on first sign-in (the forced-change flow) |

What the demo campus holds:

- A running term with a weekly timetable (Saturday to Thursday) and a registration window that is still open, so you can register and drop.
- CSE301, which shows a "Missing prerequisite" reason.
- Tuition and activity fees, and one payment. Online checkout uses the mock gateway.
- Announcements, including an urgent one that sends a notification, an open assignment and a PDF lecture material.
- Forum categories with a conversation, and a published Career Fair event.

## Talking to the API

- **Sign in:** `POST /api/auth/login` with `{ "email", "password" }` returns `accessToken` + `expiresAt`, `refreshToken` +
  `refreshTokenExpiresAt` (14 days) and `mustChangePassword`. Send `Authorization: Bearer <accessToken>` on every
  other call; `GET /api/auth/me` returns the signed-in user. While `mustChangePassword` is true, everything except
  `/api/auth/me` and `POST /api/auth/change-password` answers 403 `password_change_required`.
- **Staying signed in:** before the access token expires (or after a 401), `POST /api/auth/refresh` with
  `{ "refreshToken" }` returns a new pair. **Each refresh token works once**: always store the new one. Using an old
  one again ends that sign-in (it looks stolen). `POST /api/auth/logout` with `{ "refreshToken" }` ends the sign-in.
  Changing or resetting the password ends every other sign-in.
- **Forgot password:** `POST /api/auth/forgot-password` with `{ "email" }` always answers 202. The email holds a link to
  `<frontend>/reset-password?token=…` (valid 30 minutes, once). Your page posts `{ "token", "newPassword" }` to
  `POST /api/auth/reset-password` (204). No real email is sent in this setup: read it as Student Affairs at
  `GET /api/admin/dev/emails?to=<address>`.
- **Errors** always have the same shape: `{ "code", "message", "details": [{ "field", "error" }] }`. The codes are
  400 `validation_failed` / `invalid_value` / `bad_request`, 401 (`invalid_credentials` on login), 403 `forbidden` /
  `password_change_required`, 404 `not_found`, 409 `conflict`, 429 `rate_limited` and 500 `server_error`.
  `details` names the offending fields, so show them next to the inputs.
- **Lists** are paged: `?page=1&pageSize=20` (at most 100). The response is `{ items, page, pageSize, totalCount, totalPages }`.
- **Enums** are snake_case strings (`"lecture_hall"`, `"student_affairs"`). Times are UTC ISO-8601. Ids are GUIDs.
- **Bilingual data** comes as pairs (`name` / `nameAr`). The UI picks the language.
- **Files:** uploads are `multipart/form-data`. Downloads and CSV exports set `Content-Disposition` with the file name,
  which is readable from the browser.
- **PDFs (English):** `GET /api/documents/receipts/{paymentId}`, `/api/documents/transcript`,
  `/api/documents/enrollment-certificate` and `/api/documents/schedule?from=&to=` (students), plus
  `/api/admin/documents/students/{studentId}/transcript` and `…/enrollment-certificate` (Student Affairs). Open them
  with the bearer token (fetch as a blob), not as a plain link.
- **CORS:** the API accepts `http://localhost:5173` and `http://localhost:3000`. For another port, set `FRONTEND_ORIGIN`
  in a `.env` file next to `docker-compose.yml`, then run `docker compose up -d` again.

The API docs page lists every endpoint with its request and response shapes. Press **Authorize** there and paste an `accessToken`.

## `.env` options (all optional)

Create a `.env` file in this folder. It is never committed.

```dotenv
API_PORT=8080                       # host port for the API
FRONTEND_ORIGIN=http://localhost:5173
DEMO_PASSWORD=Demo@2026             # only read on the very first start
API_TAG=dev                         # or sha-<commit> to pin one backend build
JWT_SIGNING_KEY=                    # base64 of 48 random bytes; set it to stay signed in across restarts
```
