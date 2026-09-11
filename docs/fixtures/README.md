# Reference fixtures

Curated **offline reference material** for ArchiMate AI Studio — Open Group publications, ArchiSurance practice models, and Archi-LLM-plugin prior art. Used for RAG ingestion, prompt engineering, round-trip tests, and agent context.

**Not runtime code.** Architecture decisions live in [`../architecture/`](../architecture/).

---

## Layout

| Path | Source | Purpose |
|------|--------|---------|
| [`opengroup/`](opengroup/) | [The Open Group](https://www.opengroup.org/) publications | Normative specs + case study + SABSA guide |
| [`archisurance-practice-2025/`](archisurance-practice-2025/) | [yasenstar/ArchiMate_ArchiSurance](https://github.com/yasenstar/ArchiMate_ArchiSurance) | TOGAF ADM phased modeling outline (2025 edition) |
| [`archi-llm-plugin/`](archi-llm-plugin/) | [fideocam/Archi-LLM-plugin](https://github.com/fideocam/Archi-LLM-plugin) (MIT) | LLM patch contract, prompts, TOON, provider plan |

See [`REVIEW.md`](REVIEW.md) for a structured review of both upstream repos (2026-09-11).

---

## Quick links

| Need | File |
|------|------|
| ArchiMate 4 spec (PDF) | `opengroup/archimate-4-specification.pdf` |
| ArchiSurance 3.2 case study (PDF, Y231) | `opengroup/archisurance-3.2-case-study.pdf` |
| SABSA × ArchiMate modeling guide (PDF) | `opengroup/sabsa-archimate-modeling-guide.pdf` |
| ArchiSurance figure/phase outline | `archisurance-practice-2025/case-study-outline.md` |
| LLM system prompt (ArchiGPT) | `archi-llm-plugin/system-prompt.txt` |
| JSON patch / RunPod provider design | `archi-llm-plugin/EXTERNAL_LLM_PLAN.md` |
| Token-efficient model encoding | `archi-llm-plugin/TOON_ARCHIMATE_BPMN.md` |

---

## Test models (separate path)

Executable `.archimate` XML for CI round-trip lives under [`../../tests/fixtures/archisurance/`](../../tests/fixtures/archisurance/) (gitignored binary). Download Y194M from The Open Group, or clone the full 2025 practice repo for phased models — see [`archisurance-practice-2025/README.md`](archisurance-practice-2025/README.md).

---

## License notes

- **Open Group PDFs** — respect publication terms; do not redistribute outside your licensed use.
- **Archi-LLM-plugin** — MIT; attribute when porting prompt text (see [`../architecture/archi-llm-plugin-learnings.md`](../architecture/archi-llm-plugin-learnings.md)).
- **ArchiMate_ArchiSurance** — MIT; models derived from Open Group ArchiSurance case study.
