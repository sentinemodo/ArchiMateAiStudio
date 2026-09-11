# Security maturity — ArchiMate EA Platform

**Product:** Enterprise architecture management (ArchiMate 3.2, LLM/RAG, multi-agent)  
**Last reviewed:** 2026-09-11  
**Reviewer:** `/cybersecurity-design`

---

## Architecture baseline status

| Document | Status |
|----------|--------|
| `overview.md` | **Missing** — run `/project-architect` |
| `technology.md` | **Missing** — run `/project-architect` |
| `modules-and-integrations.md` | **Missing** — run `/project-architect` |

Security requirements below are scoped from stated product intent (C# preferred, RunPod LLM, RAG, multi-agent ArchiMate modeling). When architecture docs land, re-align tier gates and technology-specific controls.

---

## Active tier

| Field | Value |
|-------|-------|
| **Current tier** | **MVP** (design / pre-implementation) |
| **Effective bar for EA data** | **Beta-equivalent** for Confidential and Restricted classifications (see `security-requirements.md` §1) |
| **Target next tier** | **Beta** (pilot tenants, external RunPod, multi-team RBAC) |
| **GA target** | **Production** |
| **Hardened** | Reserved for regulated tenants or on-prem air-gapped deployments |

---

## Tier progression triggers

| Advance to | When |
|------------|------|
| **Beta** | First multi-tenant pilot; ingestion of real customer architecture docs; RunPod or other third-party inference in production path |
| **Production** | General availability; SSO for enterprise customers; SLA commitments; backup/restore tested |
| **Hardened** | Compliance scope (ISO 27001, SOC 2 Type II, EU-only residency contract); pen-test before launch; CISO sign-off on agent merge workflow |

---

## Tier control summary (this product)

| Control area | MVP | Beta | Production | Hardened |
|--------------|-----|------|------------|----------|
| Data classification labels on artifacts | Must | Must | Must + automated enforcement | Must + DLP hooks |
| RBAC (EA / security / project roles) | Must (coarse) | Must (fine-grained) | Must + ABAC for model elements | Must + SoD rules |
| LLM prompt-injection defenses | Must (baseline) | Must (layered) | Must + red-team cadence | Must + formal threat model |
| RAG tenant isolation | Must | Must | Must + row-level security | Must + encryption per tenant |
| Agent output human approval | Must | Must | Must + configurable policy | Must + dual control for Restricted |
| Audit trail (human vs agent) | Must | Must | Must + immutable store | Must + SIEM export |
| Upload malware scanning | Should | Must | Must | Must + sandboxed OCR |
| Dependency CVE scan in CI | Should | Must | Must + SLA | Must + SBOM |
| RunPod private networking | Could | Should | Must | Must (VPC / no public egress) |

---

## Next review

- **Date:** When `/project-architect` publishes `technology.md` and first repo manifests exist
- **Actions:** Re-run dependency scan; confirm tier still matches rollout phase; advance tier if pilot starts
