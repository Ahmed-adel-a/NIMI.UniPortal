# AGENTS.md

## Project

**NIMI.UniPortal** — backend for an Arabic RTL university portal, built as a sellable multi-university product (Model 2: one isolated deployment + its own database per university). First client: Cairo University — Faculty of Engineering. **This repo is backend-only**; the React frontend lives in a separate folder/repo (`university-map`) and is wired to this API in phase P11.

Stack: .NET 10 (`net10.0` pinned in `Directory.Build.props`) · Modulith + Clean Architecture (83 projects) · MediatR · EF Core · PostgreSQL (one DB per deployment) · Central Package Management (`Directory.Packages.props`).

**No API runs yet** — the solution is a skeleton; the first vertical slice is P0.1 (`GET /api/platform/settings`).

## Commands

| Command | Description |
|---------|-------------|
| `dotnet build NIMI.UniPortal.sln -c Release` | Build the whole solution |
| `dotnet test NIMI.UniPortal.sln` | Run all tests (once test projects gain content) |
| `dotnet build-server shutdown` | Stop lingering MSBuild/compiler servers (Windows file locks) |

CI: `.github/workflows/backend-ci.yml` — restore → build → test on every push to `main` / `dev` / `module/**` and every PR.

## Repository layout

- `NIMI.UniPortal.sln` — the solution (repo root)
- `src/BuildingBlocks/` — `NIMI.UniPortal.SharedKernel` (no business logic, referenceable by all) · `NIMI.UniPortal.Abstractions` (e.g. `IAcademicLifecycle`) · `NIMI.UniPortal.EventBus` (in-process integration events, published after commit)
- `src/Modules/<Module>/` — one folder per module; layers: `.Contracts` → `.Domain` → `.Application` → `.Infrastructure` → `.Api`
- `src/Hosts/` — one host per deployment (`Host.CairoUniversity`, `Host.AinShamsUniversity`); premium modules and exactly one lifecycle variant are selected per host at build time
- `tests/` — `ArchitectureTests` · `Modules.IntegrationTests` · `Modules.UnitTests`
- `deployments/` — per-client deployment assets (Docker deferred to the deployment phase)

## Binding conventions (full rules: `implementation-plan.md` §1 · reference matrix: `solution-structure.md` §2)

- **No repository/UoW classes** — modules expose `I<Module>DbContext`; exactly one `SaveChangesAsync` per command via `UnitOfWorkBehavior`; integration events published **after** commit
- MediatR pipelines: **Validation + UnitOfWork only**
- Reads: `AsNoTracking` + projections; pagination on every list endpoint
- Time: UTC everywhere, `TimeProvider` only (`system_settings.timezone` is for display) — `DateTime.Now` is banned
- Cross-module work: reference **`.Contracts` only**; no cross-schema FKs (IDs only)
- Dependency matrix (enforced by architecture tests): Domain → nothing · Application → Domain + Contracts + SharedKernel (EF only for `I<Module>DbContext`) · Infrastructure → Application (never Api) · Api → Application + Infrastructure

## Branching · commits · tracking

- Branch model: `main` (release line, protected, PR-only, always green) ← `dev` (integration base — **all work branches start from here**; a module merges into `dev` once it works flawlessly) ← `module/<module>` ← `slice/p<phase>.<n>-<slug>`; also `fix/<slug>`, `chore/<slug>` (based on `dev`)
- Conventional Commits — e.g. `feat(attendance): P4.2 QR check-in (US-S05)`
- **After every step:** update `phase-tracker.md` + `project-map.json` — the live map (both are **local-only**: gitignored, never committed)

## Working style

- Plan first; confirm before large or irreversible steps
- Build module-by-module as vertical slices (7 stations: DB → Domain → Application → Contracts → Api → Tests → Demo)

## Docs (all in repo root — **local-only, gitignored**: they live on your disk but not in the repo or its history; only `AGENTS.md` itself is committed)

| Doc | What |
|-----|------|
| `implementation-plan.md` | **The spec** — phases P0–P11, slice template, DoD |
| `phase-tracker.md` | Live status (human) — current phase, steps, branches |
| `project-map.json` | Live status (machine) — phases, steps, modules, decisions |
| `solution-structure.md` | Modulith + Clean Architecture structure (the deployable design) |
| `architecture.md` | Entity design + DB model (one deployment + DB per client) |
| `mvp-documentation.md` | MVP build scope |
| `user-stories.md` · `database-and-user-stories.md` · `missing-requirements.md` | Requirements & open decisions (D1–D5, each mapped to a gating phase) |
