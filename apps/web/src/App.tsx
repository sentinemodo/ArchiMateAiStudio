import { useCallback, useEffect, useState } from 'react'
import {
  approveProposal,
  createEmptyModel,
  exportUrl,
  generateChanges,
  getModel,
  importArchimate,
  ingestPdf,
  listModels,
  listProposals,
  rejectProposal,
  searchModel,
  type ModelDetail,
  type ModelListItem,
  type ProposalItem,
  type SearchHit,
} from './api'
import './App.css'

type Screen = 'list' | 'detail'

function App() {
  const [screen, setScreen] = useState<Screen>('list')
  const [models, setModels] = useState<ModelListItem[]>([])
  const [selectedId, setSelectedId] = useState<string | null>(null)
  const [detail, setDetail] = useState<ModelDetail | null>(null)
  const [proposals, setProposals] = useState<ProposalItem[]>([])
  const [newName, setNewName] = useState('New Model')
  const [instruction, setInstruction] = useState(
    'Add ApplicationComponent named Claims Portal.',
  )
  const [searchQuery, setSearchQuery] = useState('Customer')
  const [searchHits, setSearchHits] = useState<SearchHit[]>([])
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [status, setStatus] = useState<string | null>(null)

  const refreshList = useCallback(async () => {
    const items = await listModels()
    setModels(items)
  }, [])

  const refreshDetail = useCallback(async (id: string) => {
    const [model, proposalItems] = await Promise.all([getModel(id), listProposals(id)])
    setDetail(model)
    setProposals(proposalItems)
  }, [])

  useEffect(() => {
    void (async () => {
      try {
        await refreshList()
      } catch (err) {
        setError(err instanceof Error ? err.message : String(err))
      }
    })()
  }, [refreshList])

  async function run(action: () => Promise<void>) {
    setBusy(true)
    setError(null)
    setStatus(null)
    try {
      await action()
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err))
    } finally {
      setBusy(false)
    }
  }

  function openModel(id: string) {
    setSelectedId(id)
    setScreen('detail')
    void run(async () => {
      await refreshDetail(id)
    })
  }

  async function showModel(id: string) {
    setSelectedId(id)
    setScreen('detail')
    await refreshDetail(id)
  }

  function backToList() {
    setScreen('list')
    setSelectedId(null)
    setDetail(null)
    setProposals([])
    setSearchHits([])
    void run(async () => {
      await refreshList()
    })
  }

  return (
    <div className="app">
      <header className="header">
        <h1>ArchiMate AI Studio</h1>
        <p className="subtitle">MVP model shell — generate &amp; proposals</p>
      </header>

      {error && <div className="banner error">{error}</div>}
      {status && <div className="banner ok">{status}</div>}

      {screen === 'list' && (
        <section className="panel">
          <h2>Models</h2>
          <div className="row">
            <input
              value={newName}
              onChange={(e) => setNewName(e.target.value)}
              placeholder="Model name"
              disabled={busy}
            />
            <button
              type="button"
              disabled={busy || !newName.trim()}
              onClick={() =>
                void run(async () => {
                  const created = await createEmptyModel(newName.trim())
                  setStatus(`Created ${created.name}`)
                  await refreshList()
                  await showModel(created.id)
                })
              }
            >
              Create empty
            </button>
          </div>

          <div className="row">
            <label className="file-label">
              Import .archimate
              <input
                type="file"
                accept=".archimate,application/xml,text/xml"
                disabled={busy}
                onChange={(e) => {
                  const file = e.target.files?.[0]
                  e.target.value = ''
                  if (!file) return
                  void run(async () => {
                    const created = await importArchimate(file)
                    setStatus(`Imported ${created.name}`)
                    await refreshList()
                    await showModel(created.id)
                  })
                }}
              />
            </label>
            <button type="button" disabled={busy} onClick={() => void run(refreshList)}>
              Refresh
            </button>
          </div>

          {models.length === 0 ? (
            <p className="muted">No models yet. Create an empty model or import a file.</p>
          ) : (
            <ul className="list">
              {models.map((model) => (
                <li key={model.id}>
                  <button type="button" className="linkish" onClick={() => openModel(model.id)}>
                    <strong>{model.name}</strong>
                    <span className="muted">
                      {model.elementCount} elements · {new Date(model.updatedAt).toLocaleString()}
                    </span>
                  </button>
                </li>
              ))}
            </ul>
          )}
        </section>
      )}

      {screen === 'detail' && selectedId && (
        <>
          <div className="row">
            <button type="button" onClick={backToList} disabled={busy}>
              ← Models
            </button>
            <a className="button-link" href={exportUrl(selectedId)}>
              Export .archimate
            </a>
          </div>

          <section className="panel">
            <h2>{detail?.name ?? 'Model'}</h2>
            {detail && (
              <>
                <p className="muted">
                  Schema {detail.schemaVersion} · {detail.stats.elements} elements ·{' '}
                  {detail.stats.relationships} relationships · {detail.stats.views} views
                </p>
                <h3>Digest</h3>
                <pre className="digest">{detail.digest}</pre>
              </>
            )}

            <h3>RAG search</h3>
            <div className="row">
              <input
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                placeholder="Search indexed chunks…"
                disabled={busy}
                style={{ flex: 1, minWidth: '12rem' }}
              />
              <button
                type="button"
                disabled={busy || !searchQuery.trim()}
                onClick={() =>
                  void run(async () => {
                    const hits = await searchModel(selectedId, searchQuery.trim())
                    setSearchHits(hits)
                    setStatus(`Found ${hits.length} chunk(s)`)
                  })
                }
              >
                Search
              </button>
            </div>
            {searchHits.length > 0 && (
              <ul className="list search-hits">
                {searchHits.map((hit) => (
                  <li key={`${hit.kind}:${hit.sourceId}`}>
                    <strong>
                      {hit.kind} · {hit.score.toFixed(3)}
                    </strong>
                    <pre className="digest small">{hit.text}</pre>
                  </li>
                ))}
              </ul>
            )}

            <h3>Generate</h3>
            <textarea
              rows={4}
              value={instruction}
              onChange={(e) => setInstruction(e.target.value)}
              disabled={busy}
              placeholder="Describe the model change…"
            />
            <div className="row">
              <button
                type="button"
                disabled={busy || !instruction.trim()}
                onClick={() =>
                  void run(async () => {
                    const result = await generateChanges(selectedId, instruction.trim())
                    setStatus(
                      `Created proposal ${result.proposalId} (${result.summary.elementCount} elements)`,
                    )
                    await refreshDetail(selectedId)
                  })
                }
              >
                Submit generate
              </button>
              <label className="file-label">
                Upload PDF
                <input
                  type="file"
                  accept="application/pdf,.pdf"
                  disabled={busy}
                  onChange={(e) => {
                    const file = e.target.files?.[0]
                    e.target.value = ''
                    if (!file) return
                    void run(async () => {
                      const result = await ingestPdf(selectedId, file)
                      setStatus(
                        `Ingested PDF → proposal ${result.proposalId} (${result.summary.elementCount} elements)`,
                      )
                      await refreshDetail(selectedId)
                    })
                  }}
                />
              </label>
            </div>
          </section>

          <section className="panel">
            <div className="row between">
              <h2>Proposals</h2>
              <button
                type="button"
                disabled={busy}
                onClick={() => void run(async () => refreshDetail(selectedId))}
              >
                Refresh
              </button>
            </div>
            {proposals.length === 0 ? (
              <p className="muted">No proposals for this model.</p>
            ) : (
              <ul className="list">
                {proposals.map((proposal) => {
                  const names =
                    proposal.patch?.elements?.map((el) => `${el.type}:${el.name}`).join(', ') ||
                    '(empty patch)'
                  return (
                    <li key={proposal.id} className="proposal">
                      <div>
                        <strong>{proposal.status}</strong> · {proposal.source} ·{' '}
                        <span className="muted">{names}</span>
                      </div>
                      {proposal.status === 'Pending' && (
                        <div className="row">
                          <button
                            type="button"
                            disabled={busy}
                            onClick={() =>
                              void run(async () => {
                                await approveProposal(proposal.id)
                                setStatus(`Approved ${proposal.id}`)
                                await refreshDetail(selectedId)
                              })
                            }
                          >
                            Approve
                          </button>
                          <button
                            type="button"
                            disabled={busy}
                            onClick={() =>
                              void run(async () => {
                                await rejectProposal(proposal.id)
                                setStatus(`Rejected ${proposal.id}`)
                                await refreshDetail(selectedId)
                              })
                            }
                          >
                            Reject
                          </button>
                        </div>
                      )}
                    </li>
                  )
                })}
              </ul>
            )}
          </section>
        </>
      )}
    </div>
  )
}

export default App
