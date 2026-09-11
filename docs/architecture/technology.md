# ArchiMate AI Studio — technology stack

**Parent:** [`overview.md`](overview.md)  
**Last updated:** 2026-09-11

---

## 1. Stack summary

| Layer | Choice | Rationale |
|-------|--------|-----------|
| **Frontend** | React 19 + Vite + TypeScript | Rich diagram review UI; aligns with workspace conventions |
| **UI components** | Tailwind CSS + shadcn/ui | Fast iteration |
| **Diagram canvas** | **React Flow** (+ custom ArchiMate node shapes) | Layout preview, proposal review; not full Archi replacement |
| **API** | **ASP.NET Core 9** Web API | User preference; strong XML/XSD tooling |
| **ORM** | **EF Core 9** + Npgsql | Metadata, jobs, audit, vectors |
| **Auth** | **Clerk** (JWT) | Multi-tenant SaaS-ready |
| **Primary DB** | **PostgreSQL 16** (Neon) + **pgvector** + **pg_trgm** | RAG + hybrid search in one store |
| **Cache / queue** | **Redis** (Upstash) | Hangfire, rate limits, prompt cache |
| **Background jobs** | **Hangfire** | Ingestion, LLM, re-index |
| **Object storage** | **Cloudflare R2** | Raw uploads, exported models |
| **LLM inference** | **RunPod Serverless** | User constraint; GPU on demand |
| **PDF** | **PdfPig** | Pure C# text extraction |
| **DOCX** | **DocumentFormat.OpenXml** | Microsoft-compatible |
| **OCR fallback** | **Tesseract** via **Tesseract.Net** | Offline fallback |
| **XML/XSD** | **System.Xml.Schema** + optional **XmlSchemaClassGenerator** | Exchange format fidelity |
| **JSON schema** | **System.Text.Json** + **JsonSchema.Net** | ModelPatch validation |
| **Observability** | **Sentry** + **OpenTelemetry** | LLM job tracing |

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
| **React SPA (Vite)** | **Selected** — diagram review, ingestion UX, Clerk integration |
| **Blazor WASM** | Rejected — weaker diagram ecosystem |
| **Archi plugin fork** | Rejected — desktop-only; learn from Archi-LLM instead |

**Key screens:**

- Model dashboard (digest, recent proposals)
- Graph explorer (search, filter by layer)
- Ingestion upload + side-by-side review
- Proposal diff (elements/relationships added/changed)
- View consolidation wizard
- Agent run tracker

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

- Clerk orgs for tenant isolation
- Row-level security on `modelId` + `tenantId`
- Encrypt R2 objects with SSE; optional CMK Phase 2
- RunPod: no training on customer data; DPA review required for prod
- API rate limiting via Redis sliding window

See future `cybersecurity/security-requirements.md` after security agent pass.
