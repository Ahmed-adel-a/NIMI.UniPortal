# AGENTS.md

## Project

**NIMI.UniPortal**: the backend for an Arabic RTL university portal, built as a sellable multi-university product (Model 2: one isolated deployment + its own database per university). One deployment covers a **whole university**: faculties (colleges) → departments, all managed as data, nothing hardcoded. First client: **Cairo University** (core modules + `Lifecycle.CreditHours` + Forum + Events). **This repo is backend-only**; the React frontend lives in a separate repo (`university-map`) and is wired to this API in phase P12.

Stack: .NET 10 (`net10.0` pinned in `Directory.Build.props`) · Modulith + Clean Architecture (87 projects: 3 BuildingBlocks + 16 modules × 5 + `Host.CairoUniversity` + 3 test projects) · MediatR · EF Core · PostgreSQL (one DB per deployment) · Guid v7 keys · Central Package Management (`Directory.Packages.props`).

**State:** `Host.CairoUniversity` boots with an empty pipeline; no endpoints yet. Architecture tests guard the layer/module rules (they read the `.csproj` reference graph). Next: test infrastructure (P0.A4), then the first vertical slice P0.1 (`GET /api/platform/settings`).

## Commands

| Command | Description |
|---------|-------------|
| `dotnet build NIMI.UniPortal.sln -c Release` | Build the whole solution |
| `dotnet test NIMI.UniPortal.sln` | Run all tests |
| `dotnet test tests/ArchitectureTests` | Run only the architecture rules (fast, no Docker) |
| `dotnet run --project src/Hosts/Host.CairoUniversity` | Run the Cairo host locally |
| `dotnet build-server shutdown` | Stop lingering MSBuild/compiler servers (Windows file locks) |

CI: `.github/workflows/backend-ci.yml` runs restore → build → test on every push to `main` / `dev` / `module/**` and on every PR.

## Repository layout

- `NIMI.UniPortal.sln`: the solution (repo root)
- `src/BuildingBlocks/`: `NIMI.UniPortal.SharedKernel` (no business logic, referenceable by all) · `NIMI.UniPortal.Abstractions` (`IAcademicLifecycle`: rosters, progress, graduation) · `NIMI.UniPortal.EventBus` (in-process integration events, published after commit)
- `src/Modules/<Module>/`: one folder per module; layers `.Contracts` → `.Domain` → `.Application` → `.Infrastructure` → `.Api`. Core: Platform, Users, Notifications, Scheduling, Attendance, Materials, Announcements, Assignments, Grades, Payments, Audit. Lifecycle variants: `Lifecycle.CreditHours`, `Lifecycle.SemesterBased`. Premium: Forum, Events, Library
- `src/Hosts/`: one host per deployment. `Host.CairoUniversity` (real) · `Host.AinShamsUniversity` (placeholder reference host for the SemesterBased variant, created in P4.10). Premium modules and exactly one lifecycle variant are selected per host at build time
- `tests/`: `ArchitectureTests` · `Modules.IntegrationTests` · `Modules.UnitTests`
- `deployments/`: per-client deployment assets (Docker deferred to the deployment phase, DEP)

## Binding conventions (full rules: `implementation-plan.md` §1 · reference matrix: `solution-structure.md` §2)

- **No repository/UoW classes**: modules expose `I<Module>DbContext`; exactly one `SaveChangesAsync` per command via `UnitOfWorkBehavior`; integration events published **after** commit
- MediatR pipelines: **Validation + UnitOfWork only**
- IDs: **Guid v7** created in the domain (`Guid.CreateVersion7()`)
- Reads: `AsNoTracking` + projections; pagination on every list endpoint
- Time: store UTC; wall-clock schedule/attendance math uses the effective timezone setting; `TimeProvider` only. `DateTime.Now` is banned
- Settings: read via `IPlatformApi.GetEffectiveSettings(facultyId)` (faculty override → university default); never hardcode university rules
- Rosters/enrollment: owned by the lifecycle module; other modules ask `IAcademicLifecycle`
- Cross-module work: reference **`.Contracts` only**; no cross-schema FKs (IDs only)
- Dependency matrix (enforced by architecture tests): Domain → nothing · Contracts → SharedKernel + EventBus · Application → Domain + Contracts + SharedKernel (EF only for `I<Module>DbContext`) · Infrastructure → Application (never Api) · Api → Application + Infrastructure

## Branching · commits · tracking

- Branch model: `main` (release line, protected, PR-only, always green) ← `dev` (integration base: **all work branches start from here**; a module merges into `dev` once it works flawlessly) ← `module/<module>` ← `slice/p<phase>.<n>-<slug>`; also `fix/<slug>`, `chore/<slug>` (based on `dev`)
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
