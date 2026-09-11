# Archi-LLM-plugin reference (ArchiGPT)

**Upstream:** [fideocam/Archi-LLM-plugin](https://github.com/fideocam/Archi-LLM-plugin)  
**License:** MIT — attribute when porting prompt text or patterns.

Snapshot of upstream `docs/` plus `system-prompt.txt` (2026-09-11). Studio architecture summary: [`../../architecture/archi-llm-plugin-learnings.md`](../../architecture/archi-llm-plugin-learnings.md).

---

## Key files for implementation

| File | Studio mapping |
|------|----------------|
| [`system-prompt.txt`](system-prompt.txt) | `prompts/archimate-system-v1.txt` — ANALYSIS / EXPORT / CHANGES modes |
| [`EXTERNAL_LLM_PLAN.md`](EXTERNAL_LLM_PLAN.md) | `ILlmChatClient` + RunPod OpenAI-compatible adapter |
| [`TOON_ARCHIMATE_BPMN.md`](TOON_ARCHIMATE_BPMN.md) | Token-efficient prompt encoding (Phase 1+) |
| [`Skills.md`](Skills.md) | Cursor agent skills pattern (already mirrored in `.cursor/agents/`) |
| [`languagemodel.md`](languagemodel.md) | Context window / chunking expectations for enterprise models |

Other files (`BUILD.md`, `CONVERSATIONAL_CHAT.md`, etc.) document the desktop plugin; useful for parity testing when exporting patches between Archi and Studio.

---

## JSON patch contract

ArchiGPT CHANGES responses use:

```json
{"elements":[...],"relationships":[...],"diagram":{...},"removeElementIds":[...],...}
```

This is the canonical **ModelPatch** wire format (ADR-0004). Server-side implementation: `src/ArchiMateAiStudio.Domain/Patches/ModelPatch.cs`.
