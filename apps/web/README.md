# ArchiMate AI Studio — Web SPA

Minimal React + Vite + TypeScript UI for model CRUD, NL generate, and proposal review.

## Prerequisites

- Node.js 20+
- API running on `http://localhost:5127` (see `src/ArchiMateAiStudio.Api/Properties/launchSettings.json`)

## Setup

```bash
cd apps/web
npm install
npm run dev
```

Open the printed Vite URL (usually `http://localhost:5173`). Requests to `/api` are proxied to the API.

## Scripts

| Command | Purpose |
|---------|---------|
| `npm run dev` | Dev server with `/api` → `http://localhost:5127` |
| `npm run build` | Typecheck + production build to `dist/` |
| `npm run preview` | Preview production build (same proxy) |

## Screens

- Model list, create empty model, import `.archimate`
- Model detail with digest + generate form
- Proposals list with approve / reject
- Export download link

Auth, EF, and PDF upload are intentionally out of scope for this scaffold.
