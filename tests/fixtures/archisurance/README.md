# ArchiSurance 3.2 fixture

**ArchiSurance** is the official Open Group ArchiMate **case study model** (publication [Y194M](https://publications.opengroup.org/y194m)). It is **not** a typo of "ArchiMate".

## Setup

1. Download the ArchiMate 3.2 ArchiSurance Exchange File from The Open Group ([Y194M](https://publications.opengroup.org/y194m)).
2. Place the `.archimate` file in this directory as `archisurance-3.2.archimate`.
3. CI job `archimate-roundtrip` (see `docs/architecture/delivery/cicd-conventions.md`) will import, export, and XSD-validate.

**Alternative:** Phased 2025 practice models from [yasenstar/ArchiMate_ArchiSurance](https://github.com/yasenstar/ArchiMate_ArchiSurance) — see [`docs/fixtures/archisurance-practice-2025/`](../../../docs/fixtures/archisurance-practice-2025/). Case study PDF: [`docs/fixtures/opengroup/archisurance-3.2-case-study.pdf`](../../../docs/fixtures/opengroup/archisurance-3.2-case-study.pdf).

## License

Respect The Open Group terms for the case study distribution.
