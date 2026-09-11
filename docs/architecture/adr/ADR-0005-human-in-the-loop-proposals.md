# ADR-0005: Human-in-the-loop change proposals

**Status:** Accepted  
**Date:** 2026-09-11  
**Context:** AI ingestion and agent merge can introduce incorrect architecture; enterprise users need auditability.

## Decision

All AI-generated model mutations flow through **ChangeProposal** → user **approve/reject** before apply. Optional per-tenant auto-apply threshold (default: disabled).

## Rationale

- Archi-LLM applies changes immediately in desktop — acceptable for single architect, risky for shared models.
- Regulatory and EA governance require provenance.
- Enables diff review UI before commit.

## Consequences

- Extra API endpoints and UI for proposal queue.
- RAG index updates only after approval (except document chunks from ingestion).

## Alternatives rejected

- **Auto-apply all LLM output** — high risk of model pollution.
- **Manual copy-paste only** — defeats productivity goal.
