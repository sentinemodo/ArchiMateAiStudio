# Upstream repo review (2026-09-11)

Review of [yasenstar/ArchiMate_ArchiSurance](https://github.com/yasenstar/ArchiMate_ArchiSurance) and [fideocam/Archi-LLM-plugin](https://github.com/fideocam/Archi-LLM-plugin) for ArchiMate AI Studio fixture and implementation alignment.

---

## 1. yasenstar/ArchiMate_ArchiSurance

**What it is:** A 2025-edition **ArchiSurance modeling practice** repository — step-by-step Archi models aligned to TOGAF ADM phases (A through E/F), built in Archi.

| Asset | Location in upstream | Pulled into fixtures? |
|-------|---------------------|----------------------|
| Consolidated model `ArchiSurance2025.archimate` | repo root (~440 KB) | No — use via clone or copy to `tests/fixtures/archisurance/` |
| 38 phased models (`model01-10/` … `model31-40/`) | per-figure `.archimate` files | No — too large for git; clone upstream |
| Case study outline (figures 3–35 by ADM phase) | `ArchiMate_Archisurance.md` | Yes → `archisurance-practice-2025/case-study-outline.md` |
| Y231 ArchiSurance 3.2 PDF | `doc/y231_ArchiSurance_v3.2_202304.pdf` | Duplicate of Open Group PDF → `opengroup/archisurance-3.2-case-study.pdf` |
| Mind map | `ArchiMate_Archisurance.mm` | No — requires FreePlane; link in practice README |
| HTML report | `ArchiSurance_HTML_Report/` | No — browse on GitHub Pages upstream |
| Figure 12 capability CSV reference | `ref/Figure12/` | No — optional for validator golden tests later |

**Relevance to Studio:**

- **Round-trip CI:** Official Y194M/Y231 `.archimate` remains canonical; yasenstar repo adds **phased regression slices** (one figure per file) useful for incremental patch tests.
- **RAG / agents:** `case-study-outline.md` gives ADM phase → figure mapping for chunk metadata (`framework: TOGAF`, `phase: B`, `figure: 12`).
- **SABSA:** No SABSA content in this repo; use `opengroup/sabsa-archimate-modeling-guide.pdf` instead.

**Gaps vs Studio architecture:**

- Desktop Archi workflow only; no API, patch JSON, or LLM integration.
- Models are 2025 refresh of Y231 case study — align test expectations with ArchiMate 3.2 metamodel (not 4.0 unless migrating).

---

## 2. fideocam/Archi-LLM-plugin (ArchiGPT)

**What it is:** Eclipse plugin for **Archi + Ollama** — sends model XML to an LLM, parses JSON patches, applies changes via EMF. MIT license.

| Asset | Location in upstream | Pulled into fixtures? |
|-------|---------------------|----------------------|
| System prompt (3 response modes) | `com.archimatetool.archigpt/system-prompt.txt` | Yes → `archi-llm-plugin/system-prompt.txt` |
| Provider abstraction plan | `docs/EXTERNAL_LLM_PLAN.md` | Yes |
| TOON encoding for prompts | `docs/TOON_ARCHIMATE_BPMN.md` | Yes |
| Agent skills pattern | `docs/Skills.md` | Yes |
| Model capability notes | `docs/languagemodel.md` | Yes |
| Build / chat / testing docs | `docs/*.md` | Yes (full `docs/` copy) |
| Java implementation | `com.archimatetool.archigpt/src/` | No — summarized in [`../architecture/archi-llm-plugin-learnings.md`](../architecture/archi-llm-plugin-learnings.md) |
| JUnit fixtures | `archigpt-tests/` | No — port to xUnit when implementing parsers |

**Relevance to Studio:**

- **ADR-0004** adopts `ArchiMateLLMResult` JSON as `ModelPatch` wire format — **direct compatibility** with ArchiGPT export/import.
- **RunPod** maps to `EXTERNAL_LLM_PLAN.md` “Custom (OpenAI-compatible)” provider — already planned in Studio `ILlmChatClient`.
- **TOON** reduces prompt tokens for large models — adopt in Phase 1 LLM layer, not on disk.
- **Named-reference validation** and **id- + 32 hex** rules in system prompt — port verbatim into server-side prompts and validators.

**Gaps vs Studio architecture:**

- Local Ollama only on `main`; cloud providers on `feature/external-llm` branch — Studio uses RunPod from day one.
- No human-in-the-loop proposals, audit trail, or multi-tenant persistence (ADR-0005).
- Regex JSON parser — Studio should use `System.Text.Json` + schema validation.
- No PDF/RAG ingestion — Studio Phase 2.

---

## 3. Open Group publications (local PDFs)

Already present in workspace; organized under `opengroup/`:

| File | Publication | Studio use |
|------|-------------|------------|
| `archimate-4-specification.pdf` | ArchiMate 4 | Forward-looking RAG; implementation targets 3.2 XSD today |
| `archisurance-3.2-case-study.pdf` | Y231 ArchiSurance 3.2 | Case study narrative + element inventory for tests/RAG |
| `sabsa-archimate-modeling-guide.pdf` | SABSA × ArchiMate guide | Security overlay viewpoint, SM-* cell tagging (FR-060, FR-061) |

---

## 4. Recommended next actions

1. **CI:** Download Y194M `.archimate` into `tests/fixtures/archisurance/archisurance-3.2.archimate` for round-trip job.
2. **Prompts:** Copy `system-prompt.txt` → `src/.../prompts/archimate-system-v1.txt` when implementing RunPod loop (Phase 1).
3. **RAG:** Ingest Open Group PDFs + `case-study-outline.md` as `standard` chunks with publication metadata.
4. **Optional:** Add yasenstar phased models as optional CI matrix (figure-level patch regression).
