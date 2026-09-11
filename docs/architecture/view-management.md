# ArchiMate AI Studio — view complexity management

**Parent:** [`overview.md`](overview.md)  
**Last updated:** 2026-09-11

---

## 1. Goals

- Reduce cognitive load for stakeholders
- Preserve traceability to canonical model
- Support migration storytelling (plateaus, gaps)
- Auto-generate views without destroying manually curated diagrams

---

## 2. View types

| Type | Mutable | Source |
|------|---------|--------|
| **Canonical view** | User / import | Archi diagram |
| **Derived view** | Regenerated | View Manager algorithms |
| **Consolidated view** | Snapshot on approve | Subgraph extraction |
| **Migration view** | Diff engine | Plateau A vs B |

Derived views stored as new `<view>` elements with property `derivedFrom: algorithmId` — exportable in `.archimate`.

---

## 3. Viewpoint filtering

Map ArchiMate 3.2 viewpoints to layer/aspect filters:

| Viewpoint | Include types (simplified) |
|-----------|---------------------------|
| Application Cooperation | Application layer + serving/realization to Business |
| Infrastructure | Technology + realizing Application |
| Motivation | Motivation + influenced elements |
| Migration | Implementation & Migration + referenced plateaus |

Implementation: filter graph → induced subgraph on allowed types and relationship types.

---

## 4. Consolidation algorithms

### 4.1 Depth-limited BFS (`consolidate-bfs-v1`)

**Input:** anchor element IDs, depth `d=2`, maxNodes `N=25`, relationship whitelist `R`.

1. BFS from anchors following `R`.
2. If |V| > N: score nodes by degree + betweenness; keep top N.
3. Apply viewpoint mask.
4. Split connected components → separate view proposals.

### 4.2 Community detection (`consolidate-louvain-v1`)

**Input:** Application-layer subgraph.

1. Build undirected graph from `ServingRelationship`, `FlowRelationship`, `AssociationRelationship`.
2. Run Louvain clustering (implement or use QuikGraph + library).
3. One view per community above size threshold (≥ 5 nodes).
4. LLM names view from top 3 element names.

### 4.3 Role-based simplification (`consolidate-role-v1`)

**Input:** stakeholder role (`business` | `security` | `infra` | `executive`).

| Role | Hide layers | Collapse |
|------|-------------|----------|
| business | Technology detail | Grouping for apps |
| security | — | Highlight Requirement links |
| infra | Business detail | Show realization chain only |
| executive | All detail | Capability + Value Stream only |

### 4.4 Duplicate detection (`dedupe-elements-v1`)

Normalize names (lowercase, strip punctuation). Cluster duplicates → suggest merge patch with `alias` property on survivor.

---

## 5. Layout

| Mode | Engine |
|------|--------|
| **Auto layout** | Layered (Sugiyama) via React Flow / custom C# layout for export coordinates |
| **Preserve manual** | Copy coordinates from source view when element overlap ≥ 80% |
| **Hybrid** | Auto layout new nodes; fixed positions for anchors |

Layout output: `diagram.nodes[]` compatible with Archi-LLM JSON patch format.

---

## 6. View metrics (review integration)

| Metric | Threshold | Finding severity |
|--------|-----------|------------------|
| Nodes per view | > 40 | warning |
| Orphan elements (not in any view) | > 10% of model | error |
| Cross-layer broken chain | Business proc without app service | warning |
| Duplicate view names | any | error |
| Empty view | 0 nodes | info |

---

## 7. User workflow

1. Select scope (folder, layer, or hand-picked elements).
2. Choose algorithm + parameters.
3. Preview derived view in React Flow.
4. Edit (hide node, relayout).
5. Approve → patch adds new `<view>` to model.

---

## 8. Migration views

For `Plateau` elements with date/version:

1. Compute element sets per plateau (property or folder convention).
2. Diff: added/removed/changed elements between plateau A and B.
3. Emit **Gap** relationships where metamodel supports migration modeling.
4. Generate view showing only delta subgraph.
