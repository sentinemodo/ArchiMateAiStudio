# ArchiMate AI Studio — technology stack

**Parent:** [`overview.md`](overview.md)  
**MVP:** [`mvp.md`](mvp.md)  
**Last updated:** 2026-09-22

---

## 1. Stack summary

| Layer | Choice | Rationale |
|-------|--------|-----------|
| **Frontend** | **React 19 + Vite + TypeScript** | **Confirmed for MVP** — model shell, generate, PDF upload, proposal review |
| **UI components** | Tailwind CSS + shadcn/ui | Fast iteration |
| **Diagram canvas** | **React Flow** (+ custom ArchiMate node shapes) | Post-MVP polish OK; MVP may use simple lists/diffs |
| **API** | **ASP.NET Core 9** Web API | User preference; strong XML/XSD tooling |
| **ORM** | **EF Core 9** + Npgsql | Metadata, jobs, audit, vectors — **required for MVP** |
| **Auth** | **None (MVP)** → **Clerk** post-MVP | Auth deferred; open local/dev API |
| **Primary DB** | **PostgreSQL 16** (Neon) + **pgvector** + **pg_trgm** | RAG + hybrid search — **required for MVP** |
| **Cache / queue** | **Redis** (Upstash) | Hangfire; local in-process jobs acceptable for early MVP |
| **Background jobs** | **Hangfire** | Ingestion, LLM, re-index |
| **Object storage** | Local disk or **Cloudflare R2** | Raw uploads; local OK for MVP |
| **LLM inference** | **RunPod Serverless** | Real client for MVP; stub in CI |
| **PDF** | **PdfPig** | Pure C# text extraction — **MVP path** |
| **DOCX** | **DocumentFormat.OpenXml** | **Post-MVP** |
| **OCR fallback** | **Tesseract** via **Tesseract.Net** | **Post-MVP** (vision out of MVP) |
| **XML/XSD** | **System.Xml.Schema** + optional **XmlSchemaClassGenerator** | Exchange format fidelity |
| **JSON schema** | **System.Text.Json** + **JsonSchema.Net** | ModelPatch validation |
| **Observability** | **Sentry** + **OpenTelemetry** | LLM job tracing (light touch OK for MVP) |

---

## 2. Backend solution layout

```
ArchiMateAiStudio.sln
  src/ArchiMateAiStudio.Api/           # Controllers, auth, SSE
  src/ArchiMateAiStudio.Worker/        # Hangfire host (or merged at MVP)
  src/ArchiMateAiStudio.Domain/        # Entities, patch models, validators
  src/ArchiMateAiStudio.Application/   # Use cases, MediatR handlers
  src/ArchiMateAiStudio.Infrastructure/ # EF, RunPod, R2, Redis
  src/ArchiMateAiStudio.Archimate/     # XSD, parser, serializer, metamodel
  apps/web/                            # React SPA
  tests/
    ArchiMateAiStudio.Archimate.Tests/ # Round-trip, ArchiSurance fixture
    ArchiMateAiStudio.Llm.Tests/       # Prompt + parser golden files
```

---

## 3. RunPod integration

| Variable | Purpose |
|----------|---------|
| `RUNPOD_API_KEY` | Bearer for RunPod API |
| `RUNPOD_CHAT_ENDPOINT_ID` | Text LLM serverless endpoint |
| `RUNPOD_VISION_ENDPOINT_ID` | Vision serverless endpoint |
| `RUNPOD_EMBED_ENDPOINT_ID` | Embedding endpoint |
| `RUNPOD_OPENAI_BASE_URL` | Typically `https://api.runpod.ai/v2/{endpointId}/openai/v1` |

Use **OpenAI-compatible** handlers where available (vLLM, TGI) to minimize custom HTTP code.

---

## 4. Frontend options (decision)

| Option | Verdict |
|--------|---------|
| **React SPA (Vite)** | **Selected for MVP** — generate, PDF upload, proposal review; Clerk later |
| **Blazor WASM** | Rejected — weaker diagram ecosystem |
| **Archi plugin fork** | Rejected — desktop-only; learn from Archi-LLM instead |

**MVP screens:**

- Model list / import / export
- Generate (NL instruction) + job status
- Proposal list + approve/reject (+ simple patch summary)
- PDF upload + ingestion job status
- Optional: basic hybrid search

**Post-MVP screens:** graph explorer, view consolidation wizard, agent run tracker, Clerk sign-in.

---

## 5. Message queue

**MVP:** Hangfire on Redis — sufficient for single-worker deployment.

**Phase 3+ (if horizontal scale):** Consider **Redis Streams** or **MassTransit + RabbitMQ** for ingestion fan-out. Not required at bootstrap.

---

## 6. Version constraints

| Package | Version band |
|---------|--------------|
| .NET | 9.0 LTS track |
| EF Core | 9.x |
| Npgsql + pgvector | Latest stable |
| React | 19.x |
| Node | 22 LTS |

---

## 7. Security technology (handoff to `/cybersecurity-design`)

- **MVP:** no Clerk; single-user local/demo — do not expose open API on the public internet
- **Post-MVP:** Clerk orgs for tenant isolation; row-level security on `modelId` + `tenantId`
- Encrypt R2 objects with SSE; optional CMK when multi-tenant
- RunPod: no training on customer data; DPA review required for prod
- API rate limiting via Redis sliding window (post-MVP or when exposed)

See [`cybersecurity/security-requirements.md`](cybersecurity/security-requirements.md).
