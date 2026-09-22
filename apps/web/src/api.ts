export type ModelListItem = {
  id: string
  name: string
  elementCount: number
  updatedAt: string
}

export type ModelDetail = {
  id: string
  name: string
  digest: string
  schemaVersion: string
  stats: {
    elements: number
    relationships: number
    views: number
  }
  updatedAt: string
}

export type ProposalItem = {
  id: string
  modelId: string
  status: string
  source: string
  createdAt: string
  patch: {
    elements?: Array<{ type: string; name: string }>
    relationships?: unknown[]
  }
}

export type GenerateResult = {
  proposalId: string
  status: string
  source: string
  summary: {
    elementCount: number
    relationshipCount: number
    diagramCount: number
    elementNames: string[]
  }
}

async function readError(response: Response): Promise<string> {
  try {
    const body = await response.json()
    if (typeof body?.error === 'string') return body.error
    if (Array.isArray(body?.errors)) return body.errors.join('; ')
  } catch {
    // ignore
  }
  return `${response.status} ${response.statusText}`
}

export async function listModels(): Promise<ModelListItem[]> {
  const response = await fetch('/api/v1/models')
  if (!response.ok) throw new Error(await readError(response))
  const body = await response.json()
  return body.items ?? []
}

export async function createEmptyModel(name: string): Promise<{ id: string; name: string }> {
  const response = await fetch('/api/v1/models', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ name }),
  })
  if (!response.ok) throw new Error(await readError(response))
  return response.json()
}

export async function importArchimate(file: File): Promise<{ id: string; name: string }> {
  const form = new FormData()
  form.append('file', file)
  const response = await fetch('/api/v1/models', {
    method: 'POST',
    body: form,
  })
  if (!response.ok) throw new Error(await readError(response))
  return response.json()
}

export async function getModel(id: string): Promise<ModelDetail> {
  const response = await fetch(`/api/v1/models/${id}`)
  if (!response.ok) throw new Error(await readError(response))
  return response.json()
}

export async function generateChanges(
  modelId: string,
  instruction: string,
): Promise<GenerateResult> {
  const response = await fetch(`/api/v1/models/${modelId}/generate`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ instruction }),
  })
  if (!response.ok) throw new Error(await readError(response))
  return response.json()
}

export async function listProposals(modelId: string): Promise<ProposalItem[]> {
  const response = await fetch(`/api/v1/proposals?modelId=${encodeURIComponent(modelId)}`)
  if (!response.ok) throw new Error(await readError(response))
  const body = await response.json()
  return body.items ?? []
}

export async function approveProposal(id: string): Promise<ProposalItem> {
  const response = await fetch(`/api/v1/proposals/${id}/approve`, { method: 'POST' })
  if (!response.ok) throw new Error(await readError(response))
  return response.json()
}

export async function rejectProposal(id: string): Promise<ProposalItem> {
  const response = await fetch(`/api/v1/proposals/${id}/reject`, { method: 'POST' })
  if (!response.ok) throw new Error(await readError(response))
  return response.json()
}

export function exportUrl(modelId: string): string {
  return `/api/v1/models/${modelId}/export`
}
