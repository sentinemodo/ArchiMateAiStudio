# ADR-0002: RunPod serverless for LLM inference

**Status:** Accepted  
**Date:** 2026-09-11  
**Context:** User requires RunPod for LLM; avoid local Ollama-only desktop pattern from Archi-LLM-plugin.

## Decision

Deploy **three RunPod serverless endpoints**: text chat (72B class), vision (7B VL), embeddings (bge-large). Integrate via **OpenAI-compatible HTTP clients**.

## Rationale

- Pay-per-use GPU fits intermittent EA modeling workloads.
- OpenAI-compatible API minimizes custom integration code (extends Archi-LLM external LLM plan).
- Separates inference from app hosting (Railway).

## Consequences

- Cold starts (minutes) require async jobs and optional min workers in prod.
- Tenant consent required before sending model content to RunPod.
- CI uses mock LLM unless `RUNPOD_INTEGRATION_TEST=true`.

## Alternatives rejected

- **Ollama on app server** — poor multi-tenant scaling; GPU tied to API container.
- **OpenAI direct** — user constraint specifies RunPod.
