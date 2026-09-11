# ArchiMate AI Studio — API design

**Parent:** [`overview.md`](overview.md)  
**Last updated:** 2026-09-11

**Base URL:** `/api/v1`  
**Auth:** `Authorization: Bearer <Clerk JWT>`  
**Tenant:** Resolved from Clerk org claim → `tenantId`

---

## 1. Models

### `GET /models`

List models for tenant.

**Response 200:**

```json
{
  "items": [
    {"id": "uuid", "name": "Enterprise Architecture", "elementCount": 412, "updatedAt": "2026-09-11T12:00:00Z"}
  ]
}
```

### `POST /models`

Create empty model or import.

**Request:** `multipart/form-data` optional file `.archimate`, or JSON `{ "name": "..." }`.

**Response 201:** `{ "id": "uuid", "name": "..." }`

### `GET /models/{modelId}`

Metadata + digest.

**Response 200:**

```json
{
  "id": "uuid",
  "name": "...",
  "digest": "MODEL DIGEST\nDiagrams/views: 12\n...",
  "schemaVersion": "3.2",
  "stats": {"elements": 400, "relationships": 520, "views": 12}
}
```

### `GET /models/{modelId}/export`

Returns `application/xml` `.archimate` file.

### `PATCH /models/{modelId}`

Update name, documentation metadata.

---

## 2. Graph queries

### `GET /models/{modelId}/elements`

Query params: `type`, `folder`, `q` (search), `page`, `pageSize`.

### `GET /models/{modelId}/elements/{elementId}`

Element detail + related relationships.

### `GET /models/{modelId}/relationships`

Filter by `type`, `sourceId`, `targetId`.

---

## 3. Ingestion

### `POST /models/{modelId}/ingest`

**Request:** multipart `file`, optional `sourceLabel`, `autoApplyThreshold`.

**Response 202:**

```json
{"jobId": "uuid", "status": "uploaded"}
```

### `GET /ingestion-jobs/{jobId}`

```json
{
  "id": "uuid",
  "status": "mapping",
  "progress": 0.65,
  "factsExtracted": 24,
  "proposalId": "uuid-or-null",
  "error": null
}
```

---

## 4. LLM operations

### `POST /models/{modelId}/generate`

Extrapolate missing data.

**Request:**

```json
{
  "instruction": "Complete application layer for Order domain",
  "scope": {"folder": "Application", "elementIds": []},
  "mode": "patch"
}
```

**Response 202:** `{ "jobId": "uuid" }`

### `GET /llm-jobs/{jobId}`

Returns `{ "status", "proposalId", "analysisText" }` depending on mode.

---

## 5. Change proposals

### `GET /models/{modelId}/proposals?status=pending`

### `GET /proposals/{proposalId}`

```json
{
  "id": "uuid",
  "status": "pending",
  "patch": {"elements": [], "relationships": [], "diagram": null},
  "validation": {"errors": [], "warnings": []},
  "provenance": [{"source": "ingestion", "artifactId": "..."}],
  "preview": {"addedElements": 5, "addedRelationships": 7}
}
```

### `POST /proposals/{proposalId}/approve`

Applies patch; triggers re-index.

**Response 200:** `{ "modelVersion": 42 }`

### `POST /proposals/{proposalId}/reject`

---

## 6. Agent orchestration

### `POST /models/{modelId}/enrich/agents`

**Request:**

```json
{
  "goal": "Add security requirements for exposed APIs",
  "agents": ["cybersecurity-design", "project-architect"],
  "scope": {"viewNames": ["Application Cooperation"]}
}
```

**Response 201:**

```json
{
  "runId": "uuid",
  "briefDownloadUrl": "/api/v1/agent-runs/uuid/brief",
  "instructions": "Run listed agents in Cursor; upload results to complete endpoint"
}
```

### `GET /agent-runs/{runId}/brief`

Downloads `agent-brief.json`.

### `POST /agent-runs/{runId}/complete`

**Request:**

```json
{
  "results": [
    {"agent": "cybersecurity-design", "payload": { } },
    {"agent": "project-architect", "payload": { } }
  ]
}
```

**Response 202:** `{ "proposalId": "uuid" }`

---

## 7. Review

### `POST /models/{modelId}/review`

**Request:** `{ "scope": "full" | "view", "viewId": "optional", "includeLlm": true }`

**Response 202:** `{ "jobId": "uuid" }`

### `GET /review-jobs/{jobId}`

```json
{
  "findings": [
    {"severity": "warning", "code": "ORPHAN_ELEMENT", "message": "...", "elementId": "id-..."}
  ],
  "proposalId": "uuid-or-null"
}
```

---

## 8. Views

### `GET /models/{modelId}/views`

### `POST /models/{modelId}/views/consolidate`

**Request:**

```json
{
  "algorithm": "consolidate-bfs-v1",
  "anchorElementIds": ["id-..."],
  "maxNodes": 25,
  "viewpoint": "application_cooperation"
}
```

**Response 202:** `{ "jobId": "uuid" }`

---

## 9. Search & RAG

### `GET /models/{modelId}/search?q=payment+gateway`

Hybrid semantic + keyword.

### `POST /models/{modelId}/reindex`

Admin/background; rebuilds `rag_chunks`.

---

## 10. Real-time status

### `GET /jobs/{jobId}/stream`

**SSE** events: `progress`, `completed`, `failed`.

---

## 11. Error envelope

```json
{
  "error": {
    "code": "VALIDATION_FAILED",
    "message": "Human readable",
    "details": [{"path": "patch.elements[0].type", "issue": "Unknown type"}]
  }
}
```

HTTP codes: 400 validation, 401 auth, 403 tenant, 404, 409 concurrent edit, 429 rate limit, 502 RunPod upstream.
