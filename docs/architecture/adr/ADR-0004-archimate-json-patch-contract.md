# ADR-0004: Archi-LLM-compatible JSON patch contract

**Status:** Accepted  
**Date:** 2026-09-11  
**Context:** Archi-LLM-plugin defines a proven LLM → model mutation JSON schema; interoperability with Archi workflow is valuable.

## Decision

Adopt the **ArchiMateLLMResult** JSON shape from Archi-LLM-plugin as the canonical **ModelPatch** wire format: `elements`, `relationships`, optional `diagram`, remove* arrays, optional `error`.

Port system prompt rules (three response modes, ID format, named reference validation) to server-side prompts.

## Rationale

- Battle-tested prompts reduce hallucination and parse failures.
- Users can move between Archi+plugin and Studio.
- Golden tests can be shared conceptually with plugin JUnit tests.

## Consequences

- C# reimplementation of parser and validator — not Java interop.
- Server assigns final `id-{32hex}` values; LLM may use temporary refs resolved at apply.

## Alternatives rejected

- **Custom patch DSL** — reinventing Archi-LLM lessons.
- **LLM edits XML directly** — fragile; rejected by plugin experience.
