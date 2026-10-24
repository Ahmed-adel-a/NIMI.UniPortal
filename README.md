# NIMI UniPortal

The backend of an Arabic, right-to-left university portal for students, professors and Student Affairs. It is built as
a **product sold to many universities**: every university gets its own deployment and its own database, and one
deployment covers the whole university (faculties → departments, all managed as data).

The first client is **Cairo University**. This repository holds the **.NET backend** and the **web app**
(`frontend/`: React, Arabic right-to-left with an English switch, one app for every role). See
[`frontend/README.md`](frontend/README.md) to run it.

- [What it does](#what-it-does)
- [How it is built](#how-it-is-built)
- [Run it with Docker (no .NET needed)](#run-it-with-docker-no-net-needed)
- [Run it locally for backend development](#run-it-locally-for-backend-development)
- [Tests](#tests)
- [Configuration](#configuration)
- [Using the API](#using-the-api)
- [Repository layout](#repository-layout)
- [Working on the code](#working-on-the-code)

---

## What it does

**Roles:** students, professors (teaching staff; teaching assistants too), department heads (supervise one department
and may also teach) and Student Affairs (the administrators).

| Area | What it covers |
|------|----------------|
| **Platform** | University settings and branding; academic rules per faculty (working days, attendance threshold, grading scale, student-code format, …) |
| **Users** | Faculties and departments; accounts with generated student codes; CSV cohort import; sign-in with JWT and rotating refresh tokens; forced first password change; lockout after 5 failures; password reset by email |
| **Scheduling** | Terms; the subject catalog; groups (شعب); teaching assignments; halls; the weekly timetable with clash detection |
| **Academic lifecycle** (chosen per department) | **Credit hours** (ساعات معتمدة): self-registration with prerequisites, credit limits and seats; GPA; graduation checks. **Semester-based** (نظام الفرق): study plans, year levels, generated enrollment, automatic year-end promotion (carried subjects, stage gates, fees) |
| **Attendance** | Sessions per lecture with a rotating QR code; check-in from one registered phone; lateness, manual entries, warnings, finals eligibility |
| **Materials & Announcements** | Course files per subject and group; subject announcements with priority, pinning and expiry |
| **Assignments** | Deadlines, one submission per student with a receipt, review with a grade |
| **Grades** | Professors draft (also from a CSV); Student Affairs confirms and publishes; term and cumulative GPA; audited corrections |
| **Payments** | Fee items per term, faculty or department; office payments with receipts; online checkout through a provider-neutral gateway (a mock until the real provider is chosen) |
| **Student reports** | Students send reports to their department head, who answers once and/or closes them; Student Affairs reads them all |
| **Notifications** | In-app notifications; an email outbox (password-reset links) |
| **Dashboards, Audit, Documents** | Student dashboard; Student Affairs metrics; an activity log of sensitive actions; CSV exports; English PDFs (receipt, grade report, enrollment certificate, schedule) |
| **Premium modules** (sold separately) | **Forum** (with identity, moderation, reports) · **Events** (registration with seats) · **Library** (physical lending, reservations, reminders, and a digital library of PDF/EPUB books) |

**Both academic systems run in every deployment.** Each department uses one of them, set by Student Affairs at the
department, faculty or university level, so a university can mix them. A class group can hold students from both.
Cairo University's deployment = the core + **Forum** + **Events**, with departments defaulting to credit hours. A second,
reference deployment (`Host.AinShamsUniversity`) = the core + **Library**, defaulting to year groups. It shows that one
codebase serves different universities.

## How it is built

- **.NET 10**, ASP.NET Core minimal APIs, **EF Core** on **PostgreSQL 18** (one schema per module, snake_case),
  FluentValidation, JWT bearer auth, an in-house mediator (no MediatR), Guid v7 keys, Central Package Management.
- **A modular monolith.** 16 modules, each with five layers: `Contracts → Domain → Application → Infrastructure → Api`.
  - Modules never touch each other's tables; they talk through small **Contracts** and **integration events**,
    published after the database commit by an in-process event bus.
  - Architecture tests enforce the boundaries, the layer dependencies and "one type per file".
- **One host per university** (`src/Hosts/Host.*`). Each host composes the shared core (`NIMI.UniPortal.Hosting`,
  which includes both lifecycle variants behind one router) and the premium modules that university bought.
- **One error shape everywhere:** `{ code, message, details[{ field, error }] }`.

```
React app ──HTTP/JSON──► Host.CairoUniversity ──► core modules + Lifecycle.CreditHours + Forum + Events ──► PostgreSQL
                                     │ (shared: NIMI.UniPortal.Hosting: auth, errors, CORS, docs, dashboards, PDFs)
```

## Run it with Docker (no .NET needed)

For the frontend developer, or anyone who only needs a running backend. You need **[Docker Desktop](https://www.docker.com/products/docker-desktop)**
and the folder [`deployments/frontend-dev`](deployments/frontend-dev):

- **Windows:** double-click `start.cmd`.
- **Mac / Linux:** run `./start.sh`.

The script pulls the published image (`ghcr.io/ahmed-adel-a/nimi-uniportal-cairo:dev`) and PostgreSQL 18, then starts
both. It waits until the API is ready and **opens the API docs** at http://localhost:8080/docs. On the first start
the API creates the database and fills a **demo campus**: faculties, a running term with a timetable, professors,
students, fees, posts, forum and events.

| Demo account | Password `Demo@2026` |
|--------------|----------------------|
| Student Affairs | `affairs@demo.cu.edu.eg` |
| Professor | `hassan.mahmoud@demo.cu.edu.eg` |
| Department head (Computer) | `hala.nabil@demo.cu.edu.eg` |
| Student | `omar.khaled@demo.cu.edu.eg` |
| Student with a forced password change | `new.student@demo.cu.edu.eg` |

Day-to-day commands (in `deployments/frontend-dev`):

```bash
docker compose pull && docker compose up -d   # newest backend (published on every merge to dev)
docker compose logs -f api                    # logs
docker compose down                           # stop, keep the data
docker compose down -v                        # stop and wipe: next start is a fresh demo campus
```

More options (port, frontend origin, demo password) are in [`deployments/frontend-dev/README.md`](deployments/frontend-dev/README.md).

**Build the image yourself** (from the repository root):

```bash
docker build -f deployments/cairo-university/Dockerfile -t ghcr.io/ahmed-adel-a/nimi-uniportal-cairo:local .
```

Then run it with `API_TAG=local` set before the compose command.

## Run it locally for backend development

Backend development needs no Docker.

**1. Install** the [.NET 10 SDK](https://dotnet.microsoft.com/download) and [PostgreSQL 18](https://www.postgresql.org/download/).

**2. Give the host your database.** Create `src/Hosts/Host.CairoUniversity/appsettings.Local.json`. Git and Docker
ignore it, and it is read in Development only:

```json
{
  "ConnectionStrings": {
    "Portal": "Host=localhost;Port=5432;Database=nimi_portal;Username=postgres;Password=YOUR-PASSWORD"
  },
  "SEED_ADMIN_EMAIL": "admin@example.com",
  "SEED_ADMIN_PASSWORD": "Choose-A-Strong-Pass1",
  "Seed": { "DemoData": "true", "DemoPassword": "Demo@2026" }
}
```

- The two `SEED_ADMIN_*` keys create your first Student Affairs account.
- The `Seed` block fills the demo campus into an empty database. Leave it out for a clean start.
- User secrets work too, e.g.
  `dotnet user-secrets set ConnectionStrings:Portal "…" --project src/Hosts/Host.CairoUniversity`.

**3. Run:**

```bash
dotnet run --project src/Hosts/Host.CairoUniversity
```

The API listens on **http://localhost:5080**, with the docs at http://localhost:5080/docs. On start it creates the
database if needed and applies every module's migrations. The reference host runs the same way on port 5090. It reads
its settings from its own user secrets (`--project src/Hosts/Host.AinShamsUniversity`):

```bash
dotnet run --project src/Hosts/Host.AinShamsUniversity
```

In Development a random token-signing key is generated on each start, so sign in again after a restart. Set
`Auth:Jwt:SigningKey` (at least 32 bytes) to keep tokens valid across restarts.

## Tests

```bash
dotnet test tests/ArchitectureTests        # layers, module boundaries, one type per file (fast, no database)
dotnet test tests/Modules.UnitTests        # domain rules (fast, no database)
dotnet test tests/Modules.IntegrationTests # the real hosts against a throwaway PostgreSQL database per run
```

Integration tests need the environment variable `PORTAL_TEST_DB` set to a connection string **without** a database
name, e.g. `Host=localhost;Port=5432;Username=postgres;Password=…`. Each run creates and drops its own `portal_test_*`
database.

**CI** (GitHub Actions) runs build + all tests on every push and pull request, and builds the Docker image on every
pull request. It **publishes the image** to GitHub Container Registry on every merge to `dev`.

## Configuration

Settings come from `appsettings*.json`, `appsettings.Local.json` (Development), user secrets, environment variables
(`Section__Key`) and the command line, later sources winning.

| Key | What it does | Default |
|-----|--------------|---------|
| `ConnectionStrings:Portal` | The PostgreSQL database | *(required)* |
| `Database:ApplyMigrationsOnStartup` | Create/upgrade the schema on start | `true` in Development |
| `Auth:Jwt:SigningKey` | Token signing key, ≥ 32 bytes | random per start in Development; **required** elsewhere |
| `SEED_ADMIN_EMAIL`, `SEED_ADMIN_PASSWORD` | First Student Affairs account (Development) | — |
| `Seed:DemoData`, `Seed:DemoPassword` | Demo campus on an empty database (Development) | off |
| `Cors:AllowedOrigins` | Frontend origins allowed to call the API | `localhost:5173`, `:3000` in Development |
| `ApiDocs:Enabled` | API docs outside Development | on in Development |
| `App:FrontendUrl` | Where emailed links point | `http://localhost:5173` |
| `Email:Provider`, `Email:ExposeOutbox` | Email delivery (only `outbox` exists so far) and the outbox endpoint | `outbox` in Development |
| `Payments:Gateway`, `Payments:Mock:Secret` | Online payments (only `mock` exists so far) | `mock` in Development |
| `Platform:DefaultAcademicSystem` | Academic system where Student Affairs set none | `credit_hours` (reference host: `year_groups`) |
| `Storage:RootPath` | Where uploaded files are kept | `App_Data/files` |
| `Library:Reminders:Enabled` | Daily library reminders (reference host) | `true` |

Production refuses to start with the mock payment gateway or the outbox email provider. The real providers are still
to be chosen.

## Using the API

- **Docs:** `/docs` (Scalar, try every endpoint) and `/openapi/v1.json` (generate a typed client).
- **Sign in:**
  - `POST /api/auth/login` returns an access token (send it as `Authorization: Bearer …`) and a refresh token.
  - `POST /api/auth/refresh` renews both. Each refresh token works once.
  - `POST /api/auth/logout` ends the sign-in.
- **Forgot password:** `POST /api/auth/forgot-password`, then `POST /api/auth/reset-password` with the emailed token.
- **Errors:** always `{ code, message, details[] }`, with status 400 / 401 / 403 / 404 / 409 / 429 / 500.
- **Lists** are paged: `?page=&pageSize=` (at most 100), answering `{ items, page, pageSize, totalCount, totalPages }`.
- **Academic system:** `GET /api/auth/me` returns `academicSystem` (`credit_hours` or `year_groups`). Show
  registration screens for credit hours and year-level screens for year groups. Schedule, progress and rosters work
  the same way for both.
- **Sign-in:** `POST /api/auth/login` takes an email **or a national ID** in `email`. When one national ID has several
  accounts, it answers 409 `choice_required` with one `details` entry per role; send the login again with `role`.
- **Roles:** `/api/auth/me` returns `role`: `student`, `professor`, `department_head` or `admin`. A department head
  gets the reports inbox (`/api/department/reports`) and, for subjects they are assigned to, the teaching screens.
- **Values:** enums are snake_case strings, times are UTC, ids are GUIDs, and names come in pairs (`name` / `nameAr`).

Every flow with examples is in [`deployments/frontend-dev/README.md`](deployments/frontend-dev/README.md), and the
`.http` files in [`http/`](http) hold ready-made requests for each module.

## Repository layout

```
NIMI.UniPortal.sln
src/
  BuildingBlocks/   SharedKernel (mediator, errors, paging, CSV, PDF) · EventBus · Abstractions (lifecycle seam, file storage)
  Modules/<Module>/ Contracts · Domain · Application · Infrastructure (EF Core, migrations) · Api (endpoints)
  Hosts/            NIMI.UniPortal.Hosting (shared) · Host.CairoUniversity · Host.AinShamsUniversity
tests/              ArchitectureTests · Modules.UnitTests · Modules.IntegrationTests
frontend/           the web app (React + Vite + TypeScript), see frontend/README.md
deployments/        cairo-university/Dockerfile · frontend-dev/ (compose, start scripts, README)
http/               .http demo requests per module
.github/workflows/  backend-ci.yml (build + tests) · backend-image.yml (Docker image) · frontend-ci.yml (web app)
```

## Working on the code

- **Branches:**
  - `main` is the release line;
  - `dev` is the integration base, which every branch starts from;
  - work happens on `module/<name>`, `fix/<slug>` and `chore/<slug>`.
  
  Every change goes through a pull request into `dev` with green CI.
- **Commits** follow Conventional Commits, e.g. `feat(grades): …`.
- **A new migration** for one module:

  ```bash
  dotnet tool restore
  dotnet ef migrations add <Name> --project src/Modules/<M>/NIMI.UniPortal.Modules.<M>.Infrastructure --context <M>DbContext --output-dir Persistence/Migrations
  ```
- **Rules** for contributors and AI agents: [`AGENTS.md`](AGENTS.md).
