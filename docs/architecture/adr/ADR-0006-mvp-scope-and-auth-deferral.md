# ADR-0006: MVP scope and auth deferral

**Status:** Accepted  
**Date:** 2026-09-22  
**Context:** Stakeholders locked a demoable MVP that is broader than Phase 0 alone (RAG, PDF, React, Postgres, real RunPod) while explicitly deferring identity. Prior docs assumed Clerk from day one and treated RAG/ingestion as later phases only.

## Decision

1. **MVP includes:** React SPA (Vite + TypeScript), PostgreSQL (Neon-compatible) + EF Core + **pgvector RAG**, **PDF ingest** (PdfPig text layer → LLM → `ChangeProposal`), and a **real RunPod** `ILlmChatClient` (stub retained for CI only).
2. **MVP excludes:** Clerk/auth, multi-tenant isolation, vision/whiteboard OCR, DOCX, multi-agent enrich, view consolidation, CRDT collab.
3. **Auth deferred:** Local/dev and MVP demo APIs are **open** (no JWT). Clerk (or equivalent) is **post-MVP**.
4. **Governance unchanged:** All AI mutations still require human-in-the-loop **ChangeProposal** approve/reject before apply ([ADR-0005](ADR-0005-human-in-the-loop-proposals.md)).

Canonical detail and demo scripts: [`../mvp.md`](../mvp.md).

## Rationale

- Demo stories need RAG + PDF + browser UI + live LLM; stub-only or API-only demos fail stakeholder criteria.
- Auth adds integration surface without proving the ArchiMate patch/RAG loop; single-user local/demo is acceptable for first ship.
- Keeping ChangeProposal avoids irreversible model pollution during demos.

## Consequences

- Overview phased delivery: **MVP = Phase 0 complete + Phase 1 + Phase 2 PDF/RAG subset** (not full Phase 2/3).
- API and technology docs must state MVP auth = none; production exposure without auth is unsupported.
- `/tdd` backlog prioritizes patch applicator, EF/pgvector, RunPod client, SPA, then PDF ingest ([`../mvp.md`](../mvp.md) §7).
- Post-MVP: introduce Clerk JWT, tenant claims, and RLS without changing `ModelPatch` / proposal contracts.

## Alternatives rejected

- **Auth-first MVP (Clerk in Phase 0)** — delays demo value; rejected for this milestone.
- **Defer RAG/PDF to post-MVP** — conflicts with locked demo stories.
- **SPA optional / API-only MVP** — browser UI is required.
- **Auto-apply LLM patches for faster demos** — rejected; governance still required.
