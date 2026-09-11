# ADR-0001: C# / ASP.NET Core 9 backend

**Status:** Accepted  
**Date:** 2026-09-11  
**Context:** User preference for C#; need strong XML/XSD handling for ArchiMate Exchange Format.

## Decision

Use **ASP.NET Core 9** Web API + **EF Core 9** + **Hangfire** for all backend and worker logic. No Python sidecar for MVP.

## Rationale

- First-class XML schema validation and serialization in .NET.
- Aligns with other workspace products (GoodPlays, Parkme).
- Single language for parsers, validators, API, and background jobs.

## Consequences

- ML graph algorithms (Louvain) implemented in C# or via QuikGraph — no NetworkX.
- Vision/OCR uses RunPod or Tesseract — no Python OpenCV requirement at MVP.

## Alternatives rejected

- **Java + Archi EMF** — couples to desktop; Archi-LLM already proves pattern but wrong deployment model.
- **Node.js API** — weaker XSD tooling; splits stack from user preference.
