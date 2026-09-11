# ArchiMate AI Studio — ingestion pipeline

**Parent:** [`overview.md`](overview.md)  
**Last updated:** 2026-09-11

---

## 1. Pipeline stages

| Stage | Worker | Input | Output |
|-------|--------|-------|--------|
| **S0 Upload** | API | Multipart file | R2 key, `IngestionJob` |
| **S1 Classify** | Ingestion | MIME, magic bytes | `SourceType` enum |
| **S2 Extract** | Ingestion | Raw bytes | `ExtractedContent[]` (text blocks, page images) |
| **S3 Chunk** | Ingestion | Text blocks | `DocumentChunk[]` with page/section metadata |
| **S4 Embed** | RAG worker | Chunks | Vectors in `document_chunks` table |
| **S5 Fact extract** | Extraction | Chunks + optional images | `CandidateFact[]` JSON |
| **S6 Map to ArchiMate** | Extraction | Facts + RAG model context | `ModelPatch` JSON |
| **S7 Propose** | Extraction | Patch + validation | `ChangeProposal` |

---

## 2. Extractors

### 2.1 Text / Markdown

- UTF-8 read; split on `#` headings for section metadata.
- Max chunk size: **512 tokens** (~2000 chars) with 64-token overlap.

### 2.2 PDF

1. **PdfPig** — extract text per page.
2. If page text length < 50 chars → render page to PNG (PdfPig + SkiaSharp or ImageSharp pipeline).
3. Store page images in R2 for vision pass.

### 2.3 DOCX

- OpenXml: body paragraphs + tables → markdown-like text.
- Table rows → candidate ApplicationComponent inventory rows (heuristic).

### 2.4 Images (PNG/JPG)

- No text layer → direct to vision LLM.
- Optional EXIF rotation fix.

### 2.5 Vision LLM prompt (diagram)

System: return JSON only.

```json
{
  "notation": "archimate|flowchart|unknown",
  "shapes": [{"id":"s1","label":"Order Process","geometry":"rounded_rect","bbox":[x,y,w,h]}],
  "connectors": [{"from":"s1","to":"s2","label":"serves","arrow":"forward"}],
  "ocrText": ["..."],
  "confidence": 0.82
}
```

Second pass maps shapes → ArchiMate types using RAG (similar elements in model).

---

## 3. OCR fallback

When vision confidence < 0.6 or tenant disables cloud vision:

1. Tesseract with `eng` (+ optional `pol` etc.).
2. Line clustering for labels.
3. Weaker mapping — always `requiresReview: true`.

---

## 4. Fact extraction prompt

User message structure:

```
SOURCE ARTIFACT: {filename}, pages {range}
ORG CONTEXT (RAG):
{retrieved chunks}

EXISTING MODEL DIGEST:
{digest}

CONTENT:
{chunk text or image reference}

Extract CandidateFact[] JSON. Do not invent ArchiMate ids.
Tag each fact with confidence 0-1 and source locator.
```

---

## 5. ArchiMate mapping

Uses same **CHANGES JSON contract** as Archi-LLM-plugin (`ArchiMateLLMResult`):

- Server replaces LLM-generated ids with `id-{32hex}` before validation.
- `tempRef` in LLM output resolved in mapper.

Dedup rules:

- Match existing by `(normalizedName, type)` → skip or suggest rename.
- Match by semantic similarity (RAG cosine > 0.92) → link instead of create.

---

## 6. Retry and failure

| Failure | Action |
|---------|--------|
| RunPod timeout | Halve chunk size; retry once |
| Invalid JSON | Repair prompt with error snippet; max 2 retries |
| XSD reject on apply | Roll back; attach validator errors to proposal |
| Vision unreadable | Flag manual tracing UI |

---

## 7. Retention

| Artifact | Retention |
|----------|-----------|
| Raw upload | 90 days default (configurable) |
| Extracted text | Life of model |
| Page images | 30 days after successful mapping |
| Candidate facts | Permanent in audit |
