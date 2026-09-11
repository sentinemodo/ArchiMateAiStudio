# ArchiMate AI Studio — dependency map

**Last updated:** 2026-09-11

---

## Repository relationships

| Repo / path | Relationship | Contract surface |
|-------------|--------------|------------------|
| **`GitHub/Repositories/ArchiMateAiStudio`** (planned) | Implementation | This architecture folder |
| **`Architectures/archimate-ai-studio/`** | Source of truth | Markdown + ADRs |
| **`Architectures/standards/`** | Upstream knowledge | Synced into RAG `standard` chunks |
| **`Architectures/_research/Archi-LLM-plugin/`** | Reference clone (research) | Prompt + parser patterns — not a runtime dependency |
| **Archi desktop** | External tool | `.archimate` import/export |
| **RunPod** | External SaaS | OpenAI-compatible HTTPS |
| **Neon PostgreSQL** | External DB | Connection string |
| **Clerk** | External auth | JWT issuer |

---

## Internal module dependencies (implementation)

```
Api → Application → Domain
                 ↘ Infrastructure → Archimate, RunPod, EF, R2
Worker → Application (same)
```

**Domain** must not reference Infrastructure or RunPod SDKs.

---

## Agent dependencies (development-time)

| Agent | Consumes | Produces |
|-------|----------|----------|
| `/project-architect` | Model brief, technology context | `solution-architecture-result.json` |
| `/cybersecurity-design` | Model slice, stack | `security-requirements-result.json` |
| `/architecture-compliance` | Model + these docs | Compliance report |
| `/project-initializer` | This architecture tree | Bootstrap repo |

---

## Package dependencies (high level)

See [`technology.md`](../technology.md) for versions.

| Package | Used by |
|---------|---------|
| Npgsql.EntityFrameworkCore.PostgreSQL | Infrastructure |
| Pgvector.EntityFrameworkCore | Infrastructure |
| Hangfire.AspNetCore | Api, Worker |
| PdfPig | Ingestion |
| System.Xml.Schema | Archimate |

No runtime dependency on Java/Eclipse/Archi libraries.
