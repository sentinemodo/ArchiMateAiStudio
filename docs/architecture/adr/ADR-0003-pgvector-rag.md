# ADR-0003: PostgreSQL + pgvector for RAG

**Status:** Accepted  
**Date:** 2026-09-11  
**Context:** Need RAG over ArchiMate elements, ingested documents, and org standards; prefer operational simplicity.

## Decision

Store embeddings in **PostgreSQL 16** via **pgvector**, with **tsvector** hybrid search. Re-index on model patch apply (debounced).

## Rationale

- Single database for app data + vectors — no separate Pinecone/Weaviate at MVP.
- Neon hosts pgvector; matches GoodPlays pattern (ADR-0002 there).
- ArchiMate chunks are structured text — 1024-dim embeddings sufficient.

## Consequences

- IVFFlat index rebuild when corpus > ~100K chunks.
- Embedding calls still go to RunPod — DB stores vectors only.

## Alternatives rejected

- **Dedicated vector DB** — extra cost and sync complexity for MVP scale.
- **In-memory only** — no cross-session retrieval; fails ingestion use case.
