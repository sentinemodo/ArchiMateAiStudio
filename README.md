# ArchiMate AI Studio

AI-assisted enterprise architecture management in **ArchiMate 3.2** notation — native `.archimate` exchange files, RunPod LLM inference, RAG over models and documentation, multi-agent enrichment, and human-in-the-loop change proposals.

## Status

Phase 0 / early Phase 1 MVP slice:

- Archimate engine, model CRUD, ChangeProposal approve/reject
- NL **generate** (`POST /api/v1/models/{id}/generate`)
- PDF **ingest** (`POST /api/v1/models/{id}/ingest`) via PdfPig text layer → LLM → `ChangeProposal` (`source: pdf-ingest`)
- Real **RunPod** OpenAI-compatible client when `RUNPOD_API_KEY` is set; otherwise `StubLlmChatClient` (CI default)
- EF Core + PostgreSQL (optional) and React SPA scaffold

**Deferred this slice:** pgvector / RAG chunk retrieve into generate prompts (see below). **MVP scope locked:** [`docs/architecture/mvp.md`](docs/architecture/mvp.md). Full design: [`docs/architecture/overview.md`](docs/architecture/overview.md).

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

### RunPod LLM

Leave RunPod env vars unset to keep the stub client (offline / CI).

```bash
export RUNPOD_API_KEY=...                    # required to enable real client
export RUNPOD_OPENAI_BASE_URL=https://api.runpod.ai/v2/<endpointId>/openai/v1
# or:
export RUNPOD_CHAT_ENDPOINT_ID=<endpointId>  # builds the URL above
export LLM_CHAT_MODEL=qwen2.5-72b-instruct   # optional
```

Never commit real API keys.

### PDF ingest

`POST /api/v1/models/{id}/ingest` with multipart field `file` (PDF). Text is extracted with PdfPig, sent to the LLM with a ModelPatch instruction, and stored as a pending proposal (`source: pdf-ingest`). The SPA model detail page has an **Upload PDF** control.

### RAG (deferred)

Minimal RAG (`RagChunk` + stub embeddings + retrieve into generate) is **not** in this commit. Prefer shipping RunPod + PDF first; add pgvector retrieval when Postgres is the default path for demos.

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

Open this folder as the workspace root (or add to multi-root) so agents in [`.cursor/README.md`](`.cursor/README.md`) are available.

## Reference model

Round-trip tests use the **ArchiSurance 3.2** case study from The Open Group ([Y194M](https://publications.opengroup.org/y194m)) — see [`tests/fixtures/archisurance/README.md`](tests/fixtures/archisurance/README.md).

## Architecture docs

Start at [`docs/architecture/mvp.md`](docs/architecture/mvp.md), then [`docs/architecture/overview.md`](docs/architecture/overview.md).

Workspace mirror (optional): `Architectures/archimate-ai-studio/` in the parent Cursor workspace.
