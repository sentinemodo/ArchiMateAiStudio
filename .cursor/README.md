# ArchiMate AI Studio — Cursor agents & rules

Project-specific AI configuration. Open this repository root in Cursor so these agents override workspace defaults.

## Agents (`.cursor/agents/`)

| Agent | Invoke | Role |
|-------|--------|------|
| **project-architect** | `/project-architect` | Solution architecture; `docs/architecture/` |
| **enterprise-architect** | `/enterprise-architect` | TOGAF/SABSA/ArchiMate EA requirements |
| **cybersecurity-design** | `/cybersecurity-design` | RunPod, RAG, ingestion security (SEC-*) |
| **cicd-release** | `/cicd-release` | GitHub Actions, Railway/Vercel, ArchiSurance CI |
| **tdd** | `/tdd` | Test-first C# implementation |
| **architecture-compliance** | `/architecture-compliance` | Read-only pre-release check |

## Rules (`.cursor/rules/`)

| Rule | Scope |
|------|-------|
| `archimate-ai-studio-core.mdc` | Always — modules, subagents, conventions |
| `csharp-tdd.mdc` | `**/*.cs` |
| `cicd-archimate-ai-studio.mdc` | CI, delivery docs |

## Typical flows

- **New module/boundary:** `/project-architect` → `/tdd`
- **Security review:** `/cybersecurity-design` before RunPod in prod
- **Feature work:** `/tdd` with `csharp-tdd` rule
- **Ship:** `/cicd-release` + `/architecture-compliance`

## Reference model

**ArchiSurance 3.2** (Open Group publication Y194M) — official case study XML for round-trip tests. Download into `tests/fixtures/archisurance/` (see README there).
