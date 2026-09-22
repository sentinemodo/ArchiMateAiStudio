# ArchiMate AI Studio

AI-assisted enterprise architecture management in **ArchiMate 3.2** notation — native `.archimate` exchange files, RunPod LLM inference, RAG over models and documentation, multi-agent enrichment, and human-in-the-loop change proposals.

## Status

Phase 0 / early Phase 1 — Archimate engine, model CRUD, ChangeProposal approve/reject, NL **generate** (`POST /api/v1/models/{id}/generate` via stub LLM), EF Core + PostgreSQL (optional), and React SPA scaffold. Still to build: RAG, PDF ingest, real RunPod client. **MVP scope locked:** [`docs/architecture/mvp.md`](docs/architecture/mvp.md). Full design: [`docs/architecture/overview.md`](docs/architecture/overview.md).

## Quick start

```bash
dotnet restore ArchiMateAiStudio.slnx
dotnet build ArchiMateAiStudio.slnx
dotnet test ArchiMateAiStudio.slnx
dotnet run --project src/ArchiMateAiStudio.Api --launch-profile http
```

API health: `GET http://localhost:5127/api/v1/health`.

### Persistence

By default the API uses **in-memory** repositories (no database required; CI and local smoke work out of the box).

To use **PostgreSQL**, set `DATABASE_URL` (or `ConnectionStrings:Default`) — see [`.env.example`](.env.example). On startup the API runs EF migrations against that database.

### Web SPA

```bash
cd apps/web
npm install
npm run dev
```

Vite proxies `/api` → `http://localhost:5127`. See [`apps/web/README.md`](apps/web/README.md).

## Repository layout

| Path | Purpose |
|------|---------|
| `src/ArchiMateAiStudio.*` | Backend modules |
| `apps/web/` | React + Vite + TypeScript SPA |
| `tests/` | xUnit tests |
| `docs/architecture/` | Solution + enterprise + security architecture |
| `.cursor/` | Repo-local agents (`/project-architect`, `/tdd`, etc.) |

## Cursor agents

Open this folder as the workspace root (or add to multi-root) so agents in [`.cursor/README.md`](.cursor/README.md) are available.

## Reference model

Round-trip tests use the **ArchiSurance 3.2** case study from The Open Group ([Y194M](https://publications.opengroup.org/y194m)) — see [`tests/fixtures/archisurance/README.md`](tests/fixtures/archisurance/README.md).

## Architecture docs

Start at [`docs/architecture/mvp.md`](docs/architecture/mvp.md), then [`docs/architecture/overview.md`](docs/architecture/overview.md).

Workspace mirror (optional): `Architectures/archimate-ai-studio/` in the parent Cursor workspace.
