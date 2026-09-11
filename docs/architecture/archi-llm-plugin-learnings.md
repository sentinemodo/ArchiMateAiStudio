# Lessons from Archi-LLM-plugin review

**Source repo:** [fideocam/Archi-LLM-plugin](https://github.com/fideocam/Archi-LLM-plugin) (cloned 2026-09-11)  
**Parent:** [`overview.md`](overview.md)

---

## 1. What ArchiGPT is

An **Eclipse plugin for Archi** that sends model context + user prompts to **Ollama** (local LLM), parses structured JSON responses, and mutates the in-memory ArchiMate model via EMF.

**Stack:** Java, Eclipse RCP, Archi EMF model, Ollama HTTP API.

---

## 2. File-level map (reuse reference)

| Plugin file | Purpose | C# port target |
|-------------|---------|----------------|
| `system-prompt.txt` | ArchiMate 3.2 LLM rules (3 response modes) | `prompts/archimate-system-v1.txt` |
| `ArchiMateLLMResult.java` | Patch DTO | `Domain/ModelPatch.cs` |
| `ArchiMateLLMResultParser.java` | JSON extraction | `Infrastructure/Llm/ModelPatchParser.cs` (prefer STJ) |
| `ArchiMateSchemaValidator.java` | Type alias + EClass validation | `Archimate/MetamodelValidator.cs` |
| `ArchiMateLLMImporter.java` | Apply patch to model | `Archimate/PatchApplicator.cs` |
| `ModelContextToXml.java` | Serialize selection/model to XML | `Archimate/ContextSerializer.cs` |
| `ModelContextDigest.java` | Token-efficient summary | `Archimate/ModelDigestBuilder.cs` |
| `ModelXmlChunker.java` | Split XML at `</view>` | `Archimate/XmlChunker.cs` |
| `ModelContextChunkPlanner.java` | Budget-aware context planning | `Llm/ContextBudgetService.cs` |
| `SelectionContextBuilder.java` | Selected element/view context | `Application/SelectionContextBuilder.cs` |
| `OllamaClient.java` | HTTP chat client | `RunPodOpenAiChatClient.cs` |
| `ChunkAnalysisPrompt.java` | Multi-chunk map-reduce | `Llm/ChunkAnalysisOrchestrator.cs` |
| `docs/TOON_ARCHIMATE_BPMN.md` | TOON for token savings | RAG/LLM layer |
| `docs/EXTERNAL_LLM_PLAN.md` | Provider abstraction design | `ILlmChatClient` |
| `docs/Skills.md` | Agent skills integration | Cursor skills in repo |

---

## 3. Patterns to reuse verbatim (conceptually)

### 3.1 Three response modes

The system prompt forces:

1. **ANALYSIS** — prose only
2. **EXPORT** — full Open Exchange XML
3. **CHANGES** — JSON patch only

This prevents the #1 failure mode: user asks to "add X" but LLM replies with prose and nothing is applied.

### 3.2 Named reference validation

LLM must verify diagram/element names exist in supplied XML before describing or modifying them — reduces hallucination. **Adopt in all patch prompts.**

### 3.3 ID format

`id-` + 32 hex chars — Archi compatibility. Server generates IDs; LLM temp refs mapped at apply time.

### 3.4 Diagram vs element distinction

`View` / `Diagram` are **not** element types — new views use separate `diagram` object in JSON. Document heavily in UI tooltips.

### 3.5 Remove variants

- `removeElementIds` — from model
- `removeElementFromDiagramIds` — from view only
- `removeDiagramNames` — delete views

### 3.6 Rich diagram expectation

When user asks to "design" a process, prompt asks for **7–12 elements**, not 4–5 — improves usefulness.

### 3.7 Alias normalization

Large alias map (Actor→BusinessActor, TechnologyNode→Node, UsedBy→Serving) — port from `ArchiMateSchemaValidator.java`.

---

## 4. Gaps to improve

| Plugin limitation | Studio improvement |
|-------------------|-------------------|
| Desktop-only, single user | Multi-tenant web app |
| Ollama localhost | RunPod serverless + mock for CI |
| No persistence of AI audit | Full provenance + ChangeProposal |
| Regex JSON parser | System.Text.Json + schema + retry |
| Truncated XML, no vector index | RAG + chunk map-reduce |
| No file ingestion | PDF/image/text pipeline |
| No agent integration | Agent Orchestrator |
| Synchronous UX in UI thread | Hangfire async jobs + SSE |
| EMF-coupled validation | Standalone metamodel + XSD |
| external-llm on branch only | RunPod as first-class from day one |

---

## 5. TOON adoption

From `docs/TOON_ARCHIMATE_BPMN.md`:

- Use TOON **only in LLM prompts**, not on disk.
- Tabular encoding for elements/relationships saves tokens vs XML.
- gzip does **not** reduce token billing — semantic compression matters.
- Consider .NET TOON library from [toon-format/spec](https://github.com/toon-format/spec) when available; else JSON minify + hand-rolled tables at MVP.

---

## 6. Test assets to port

Plugin tests in `archigpt-tests/` cover:

- Ollama response parsing
- Selection in prompt
- Schema validator aliases
- LLM result parser edge cases

**Action:** Create equivalent xUnit golden files from plugin test fixtures when implementing.

---

## 7. Compatibility goal

Export patches that Archi-LLM would accept and vice versa — **same JSON wire format** for elements/relationships/diagram object. Enables migration path: use Studio for ingestion, Archi for manual polish.

---

## 8. License note

Archi-LLM-plugin is **MIT**. Attribute when porting prompt text or alias maps; keep copyright notice in `NOTICE` file in implementation repo.
