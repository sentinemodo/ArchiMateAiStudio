# ArchiMate AI Studio — CI/CD conventions

**Last updated:** 2026-09-11  
**Maintained with:** `/cicd-release` (when repo exists)

---

## Branches

| Branch | Purpose |
|--------|---------|
| `main` | Production-ready |
| `develop` | Integration |
| `feature/<ticket>-<slug>` | Feature work |
| `fix/<ticket>-<slug>` | Bug fixes |

---

## Environments

| Env | Trigger | URL pattern |
|-----|---------|-------------|
| **dev** | Local | `localhost:5173` / `localhost:5080` |
| **preview** | PR to `develop` | Railway + Vercel preview |
| **test** | Merge to `develop` | `test.archimate-ai.example` |
| **prod** | Tag `v*` on `main` | Production domain |

---

## CI pipeline (GitHub Actions)

1. **build** — `dotnet build`, `npm run build`
2. **test** — xUnit + Playwright smoke (mock RunPod)
3. **archimate-roundtrip** — ArchiSurance 3.2 fixture import/export XSD validate
4. **lint** — `dotnet format --verify-no-changes`, ESLint
5. **deploy-preview** — on PR
6. **deploy-test/prod** — on merge/tag

---

## Versioning

Semantic versioning: `MAJOR.MINOR.PATCH`

- API: `/api/v1` until breaking change → v2
- Model patch schema: `outputSchemaVersion` in agent briefs

---

## Config nomenclature

| Prefix | Example |
|--------|---------|
| `RUNPOD_*` | Inference endpoints |
| `DATABASE_URL` | Neon connection |
| `REDIS_URL` | Upstash |
| `R2_*` | Object storage |
| `CLERK_*` | Auth |
| `LLM_*` | Model names, token limits |

Secrets in Railway/Vercel — never committed.

---

## Pre-deploy gates

- [ ] Architecture compliance pass on PRs touching `src/Archimate/`
- [ ] Security scan (`dotnet list package --vulnerable`)
- [ ] No secrets in diff
- [ ] Migrations applied in test before prod
