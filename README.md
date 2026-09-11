# ArchiMate AI Studio

AI-assisted enterprise architecture management in **ArchiMate 3.2** notation — native `.archimate` exchange files, RunPod LLM inference, RAG over models and documentation, multi-agent enrichment, and human-in-the-loop change proposals.

## Status

Phase 0 skeleton — solution builds, tests pass. See [`docs/architecture/overview.md`](docs/architecture/overview.md) for the full design.

## Quick start

```bash
dotnet restore ArchiMateAiStudio.slnx
dotnet build ArchiMateAiStudio.slnx
dotnet test ArchiMateAiStudio.slnx
dotnet run --project src/ArchiMateAiStudio.Api
```

API health: `GET http://localhost:5080/api/v1/health` (port may vary — check launchSettings).

## Repository layout

| Path | Purpose |
|------|---------|
| `src/ArchiMateAiStudio.*` | Backend modules |
| `tests/` | xUnit tests |
| `docs/architecture/` | Solution + enterprise + security architecture |
| `.cursor/` | Repo-local agents (`/project-architect`, `/tdd`, etc.) |

## Cursor agents

Open this folder as the workspace root (or add to multi-root) so agents in [`.cursor/README.md`](.cursor/README.md) are available.

## Reference model

Round-trip tests use the **ArchiSurance 3.2** case study from The Open Group ([Y194M](https://publications.opengroup.org/y194m)) — see [`tests/fixtures/archisurance/README.md`](tests/fixtures/archisurance/README.md).

## Architecture docs

Start at [`docs/architecture/overview.md`](docs/architecture/overview.md).

Workspace mirror (optional): `Architectures/archimate-ai-studio/` in the parent Cursor workspace.
