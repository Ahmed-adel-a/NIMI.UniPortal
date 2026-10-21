# AGENTS.md

> **AI agents:** before any work, read docs/handbook/README.md (local-only) and follow it. It overrides this file where they differ.

## Project

**NIMI.UniPortal**: the backend for an Arabic RTL university portal, built as a sellable multi-university product (Model 2: one isolated deployment + its own database per university). One deployment covers a **whole university**: faculties (colleges) → departments, all managed as data, nothing hardcoded. First client: **Cairo University** (core modules + `Lifecycle.CreditHours` + Forum + Events). **This repo is backend-only**; the React frontend lives in a separate repo (`university-map`) and is wired to this API in phase P12.

Stack: .NET 10 (`net10.0` pinned in `Directory.Build.props`) · Modulith + Clean Architecture (87 projects: 3 BuildingBlocks + 16 modules × 5 + `Host.CairoUniversity` + 3 test projects) · own minimal mediator (no MediatR) · FluentValidation · JWT bearer · EF Core · PostgreSQL (one DB per deployment) · Guid v7 keys · Central Package Management (`Directory.Packages.props`).

**State:** modules complete: **Platform** (university settings: `GET /api/platform/settings` public, `GET`/`PUT /api/admin/settings`; per-faculty academic-rule overrides: `GET`/`PUT`/`DELETE /api/admin/faculties/{facultyId}/settings`, effective rules for any signed-in user at `GET /api/settings/effective`, student-code formats per faculty), **Users** (faculties + departments, accounts with generated student codes, CSV cohort import, user management, login with real JWTs, 5-failure/15-minute lockout, forced password change on first login) and **Notifications** (producers queue `Notifications.Contracts.NotificationRequested`; one row per distinct recipient, idempotent on `source_event_id` + `user_id`; users read their own at `GET /api/notifications/mine`, `GET /api/notifications/mine/unread-count`, `POST /api/notifications/{id}/read`, `POST /api/notifications/read-all`), and **Scheduling** (P3): terms with one active term (`GET /api/terms`, `GET /api/terms/current`, admin create/update/activate/close), the subject catalog, groups per term, professor teaching assignments, facilities per faculty or university-shared, timetable slots (`/api/admin/schedules`) with weekly/biweekly/once occurrence expansion and facility/group/staff conflict detection (409), and the caller's own occurrences at `GET /api/teaching/schedule` (professors only). Shared plumbing for every module: validation + unit-of-work pipeline steps (`ValidationErrors` in SharedKernel), the error envelope `{ code, message, details[] }` (400 / 401 / 403 `password_change_required` / `forbidden` / 404 / 409 / 500), JWT bearer + `Admin` / `PendingPasswordChange` policies, optimistic concurrency, paging (`PagedResult<T>`, max 100). Cross-module reads: `IPlatformSettingsApi` (effective settings + student-code format), `IUsersApi`, `ISchedulingApi` (terms, catalog, offerings, slots, teaching checks, occurrences), and `IAcademicLifecycle` (rosters, membership, progress — exactly one lifecycle variant per host). Cross-module events: in-process **EventBus** (`src/BuildingBlocks/NIMI.UniPortal.EventBus`) — commands queue events in the scoped `IEventCollector`; `ModuleUnitOfWorkBehavior` publishes them **after** the commit, each handler runs in its own DI scope, and a failing handler is logged instead of failing the producer. Migrations apply on startup only when `Database:ApplyMigrationsOnStartup` is true (Development + tests), and **Lifecycle.CreditHours** (P4.A): registration windows and subject prerequisites (admin), the student's active-term offerings with rule reasons and seats, self-registration and drop with an atomic `group_seats` counter (a lost race is 409 "The group is full."; schema `lifecycle_credit`, migration `InitialCreditHours`), confirmed-only group rosters (professor/admin), the student's own schedule at `GET /api/schedule`, progress at `GET /api/registration/progress`, and `IAcademicLifecycle` as the seam core modules consume. Next: **P5 Payments**.

**No Docker during development.** PostgreSQL 18 is installed locally:
- Integration tests: set the user env var `PORTAL_TEST_DB` = `Host=localhost;Port=5432;Username=postgres;Password=…` (no database name; each run creates and drops its own `portal_test_*` database). CI uses the runner's preinstalled PostgreSQL.
- Running the host: `dotnet user-secrets set ConnectionStrings:Portal "Host=localhost;Port=5432;Database=nimi_portal;Username=postgres;Password=…" --project src/Hosts/Host.CairoUniversity`
- First admin (Development only): `dotnet user-secrets set SEED_ADMIN_EMAIL …` + `SEED_ADMIN_PASSWORD …` → seeded at startup, then log in via `POST /api/auth/login` (see `http/users.http`)
- Token signing key: `Auth:Jwt:SigningKey` (≥ 32 bytes, env / user secrets, never committed). **Required** outside Development — the host refuses to start without it; Development generates a random key per run

## Commands

| Command | Description |
|---------|-------------|
| `dotnet build NIMI.UniPortal.sln -c Release` | Build the whole solution |
| `dotnet test NIMI.UniPortal.sln` | Run all tests |
| `dotnet test tests/ArchitectureTests` | Run only the architecture rules — layers, module boundaries, one type per file (fast, no database) |
| `dotnet test tests/Modules.IntegrationTests` | Run the integration tests (needs `PORTAL_TEST_DB`) |
| `dotnet run --project src/Hosts/Host.CairoUniversity` | Run the Cairo host locally |
| `dotnet tool restore` | Restore repo-local tools (`dotnet-ef` 10.0.13, pinned in `dotnet-tools.json`) |
| `dotnet ef migrations add <Name> --project src/Modules/<M>/NIMI.UniPortal.Modules.<M>.Infrastructure --context <M>DbContext --output-dir Persistence/Migrations` | Add a migration for one module (`--context` is mandatory) |
| `dotnet build-server shutdown` | Stop lingering MSBuild/compiler servers (Windows file locks) |

CI: `.github/workflows/backend-ci.yml` runs restore → build → test (against the runner's PostgreSQL) on every push to `main` / `dev` / `module/**` / `slice/**` / `fix/**` / `chore/**` and on every PR. Failing tests appear as public annotations.

## Repository layout

- `NIMI.UniPortal.sln`: the solution (repo root)
- `src/BuildingBlocks/`: `NIMI.UniPortal.SharedKernel` (no business logic, referenceable by all) · `NIMI.UniPortal.Abstractions` (`IAcademicLifecycle`: rosters, progress, graduation) · `NIMI.UniPortal.EventBus` (in-process integration events, published after commit)
- `src/Modules/<Module>/`: one folder per module; layers `.Contracts` → `.Domain` → `.Application` → `.Infrastructure` → `.Api`. Core: Platform, Users, Notifications, Scheduling, Attendance, Materials, Announcements, Assignments, Grades, Payments, Audit. Lifecycle variants: `Lifecycle.CreditHours`, `Lifecycle.SemesterBased`. Premium: Forum, Events, Library
- `src/Hosts/`: one host per deployment. `Host.CairoUniversity` (real) · `Host.AinShamsUniversity` (placeholder reference host for the SemesterBased variant, created in P4.10). Premium modules and exactly one lifecycle variant are selected per host at build time
- `tests/`: `ArchitectureTests` · `Modules.IntegrationTests` · `Modules.UnitTests`
- `deployments/`: per-client deployment assets (Docker deferred to the deployment phase, DEP)
- `http/`: `.http` demo files, one per module (`platform.http`, …)

## Binding conventions (full rules: `implementation-plan.md` §1 · reference matrix: `solution-structure.md` §2)

- **No repository/UoW classes**: modules expose `I<Module>DbContext`; exactly one `SaveChangesAsync` per command via `UnitOfWorkBehavior`; integration events published **after** commit
- Mediator: our own minimal one in `SharedKernel.Messaging` (`IQuery`/`ICommand`, `IRequestHandler`, `IPipelineBehavior`, `ISender`) — **no MediatR** (licensing). Pipelines: **Validation + UnitOfWork only**
- **One type per file, file named after the type** (classes, records, structs, interfaces, enums, delegates — nested types too; `src/` and `tests/`). Enforced by `OneTypePerFileTests`; only EF `Migrations/` and `Program.cs` are exempt
- IDs: **Guid v7** created in the domain (`Guid.CreateVersion7()`)
- Reads: `AsNoTracking` + projections; pagination on every list endpoint
- Time: store UTC; wall-clock schedule/attendance math uses the effective timezone setting; `TimeProvider` only. `DateTime.Now` is banned
- Settings: read via `IPlatformApi.GetEffectiveSettings(facultyId)` (faculty override → university default); never hardcode university rules
- Rosters/enrollment: owned by the lifecycle module; other modules ask `IAcademicLifecycle`
- Cross-module work: reference **`.Contracts` only**; no cross-schema FKs (IDs only)
- Dependency matrix (enforced by architecture tests): Domain → nothing · Contracts → SharedKernel + EventBus · Application → Domain + Contracts + SharedKernel (EF only for `I<Module>DbContext`) · Infrastructure → Application (never Api) · Api → Application + Infrastructure

## Branching · commits · tracking

- Branch model: `main` (release line, protected, PR-only, always green) ← `dev` (integration base: **all work branches start from here**; a module merges into `dev` once it works flawlessly) ← `module/<module>`; also `fix/<slug>`, `chore/<slug>` (based on `dev`)
- **One module at a time, built whole** on its `module/<module>` branch (every feature DB → Api → Tests, one commit per feature), then **one PR** into `dev` when the module is complete and green. No per-feature branches. Present the exact work list before starting a module/phase and wait for approval
- Conventional Commits, e.g. `feat(attendance): P6.2 QR check-in (US-S05)`
- **After every step:** update `phase-tracker.md` + `project-map.json`, the live map (both **local-only**: gitignored, never committed). New decisions go into `decisions.md`

## Working style

- Plan first; confirm before large or irreversible steps; ask when the docs conflict
- Build module by module as vertical slices (7 stations: DB → Domain → Application → Contracts → Api → Tests → Demo)

## Docs (repo root, **local-only, gitignored**: they live on your disk but not in the repo or its history; only `AGENTS.md` is committed)

| Doc | What |
|-----|------|
| `implementation-plan.md` | **The spec**: MVP scope, phases P0–P12 + DEP, slice template, DoD, canonical routes |
| `decisions.md` | Decision log: resolved + open decisions; other docs cite their IDs (D1–D5, MILEDU, …) |
| `architecture.md` | Entity design + DB model (PostgreSQL, one schema per module) |
| `solution-structure.md` | Modulith + Clean Architecture structure (the deployable design) |
| `user-stories.md` | Roles & permissions + every user story with its phase + post-MVP backlog |
| `phase-tracker.md` | Live status (human): current phase, steps, branches |
| `project-map.json` | Live status (machine): phases, steps, modules, decisions |
| `docs/archive/` | Superseded docs (historical only, never cite them) |
