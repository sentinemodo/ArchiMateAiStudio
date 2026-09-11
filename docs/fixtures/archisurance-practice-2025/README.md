# ArchiSurance modeling practice (2025 edition)

**Upstream:** [yasenstar/ArchiMate_ArchiSurance](https://github.com/yasenstar/ArchiMate_ArchiSurance) (MIT)  
**Author:** Xiaoqi Zhao — phased ArchiSurance rebuild aligned to TOGAF ADM in Archi.

---

## What is included here

| File | Description |
|------|-------------|
| [`case-study-outline.md`](case-study-outline.md) | Table of contents: ADM phases A–E/F, Figures 3–35 |

The **PDF case study narrative** is in [`../opengroup/archisurance-3.2-case-study.pdf`](../opengroup/archisurance-3.2-case-study.pdf) (Open Group Y231 — identical to upstream `doc/y231_ArchiSurance_v3.2_202304.pdf`).

---

## What to clone separately

The upstream repo contains ~440 KB consolidated model plus **38 phased `.archimate` files** (one per figure). These are too large to vendor in full; clone shallow when needed:

```bash
git clone --depth 1 https://github.com/yasenstar/ArchiMate_ArchiSurance.git _research/ArchiMate_ArchiSurance
```

| Upstream path | Use |
|---------------|-----|
| `ArchiSurance2025.archimate` | Full practice model |
| `model01-10/` … `model31-40/` | Incremental figure-level regression |
| `ArchiMate_Archisurance.mm` | FreePlane mind map (structure overview) |
| `ArchiSurance_HTML_Report/` | Browser-readable model report |

For CI round-trip, prefer official Open Group exchange XML → [`../../../tests/fixtures/archisurance/`](../../../tests/fixtures/archisurance/).
