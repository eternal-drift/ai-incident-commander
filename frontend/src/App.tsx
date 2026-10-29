import { useState } from "react";
import "./App.css";
import { ApiError, createIncident, decideAction, getAudit } from "./api";
import type { AuditEntry, IncidentDetail } from "./types";

const SEVERITIES = ["low", "medium", "high", "critical"];

function App() {
  const [title, setTitle] = useState("");
  const [severity, setSeverity] = useState("medium");
  const [report, setReport] = useState("");
  const [image, setImage] = useState<File | null>(null);
  const [audio, setAudio] = useState<File | null>(null);
  const [incident, setIncident] = useState<IncidentDetail | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [editingActionId, setEditingActionId] = useState<string | null>(null);
  const [editText, setEditText] = useState("");
  const [audit, setAudit] = useState<AuditEntry[] | null>(null);
  const [auditLoading, setAuditLoading] = useState(false);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setLoading(true);
    setError(null);
    try {
      const result = await createIncident({
        title,
        severity,
        report,
        image: image ?? undefined,
        audio: audio ?? undefined,
      });
      setIncident(result);
      setAudit(null);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Something went wrong talking to the server.");
    } finally {
      setLoading(false);
    }
  }

  async function handleDecision(actionId: string, decision: "approved" | "rejected" | "edited", editedText?: string) {
    if (!incident) return;
    try {
      const updated = await decideAction(incident.id, actionId, decision, editedText);
      setIncident(updated);
      setEditingActionId(null);
      setEditText("");
      if (audit) {
        await loadAudit(incident.id);
      }
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Couldn't record that decision.");
    }
  }

  function startEdit(actionId: string, currentText: string) {
    setEditingActionId(actionId);
    setEditText(currentText);
  }

  function submitEdit(actionId: string) {
    if (!editText.trim()) return;
    handleDecision(actionId, "edited", editText.trim());
  }

  async function loadAudit(incidentId: string) {
    setAuditLoading(true);
    try {
      const entries = await getAudit(incidentId);
      setAudit(entries);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Couldn't load the audit log.");
    } finally {
      setAuditLoading(false);
    }
  }

  function toggleAudit() {
    if (!incident) return;
    if (audit) {
      setAudit(null);
      return;
    }
    loadAudit(incident.id);
  }

  return (
    <div className="layout">
      <header className="header">
        <h1>AI Incident Commander</h1>
        <p>Decision support for incident response. Every action needs a human sign-off.</p>
      </header>

      <div className="columns">
        <section className="intake">
          <h2>Report an incident</h2>
          <form onSubmit={handleSubmit}>
            <label>
              Title
              <input value={title} onChange={(e) => setTitle(e.target.value)} required />
            </label>

            <label>
              Severity
              <select value={severity} onChange={(e) => setSeverity(e.target.value)}>
                {SEVERITIES.map((s) => (
                  <option key={s} value={s}>
                    {s}
                  </option>
                ))}
              </select>
            </label>

            <label>
              What's happening
              <textarea
                value={report}
                onChange={(e) => setReport(e.target.value)}
                rows={8}
                placeholder="Describe what you're seeing - alerts, symptoms, timeline, anything relevant."
                required
              />
            </label>

            <label>
              Image evidence (optional)
              <input
                type="file"
                accept="image/jpeg,image/png,image/webp"
                onChange={(e) => setImage(e.target.files?.[0] ?? null)}
              />
            </label>

            <label>
              Audio evidence (optional)
              <input
                type="file"
                accept="audio/mpeg,audio/wav,audio/mp4,audio/ogg,audio/webm"
                onChange={(e) => setAudio(e.target.files?.[0] ?? null)}
              />
            </label>

            <button type="submit" disabled={loading}>
              {loading ? "Generating plan..." : "Generate incident plan"}
            </button>
          </form>

          {error && <p className="error">{error}</p>}
        </section>

        <section className="board">
          <h2>Command board</h2>
          {!incident && <p className="empty">Submit an incident to see the plan here.</p>}

          {incident && incident.plan && (
            <>
              <VerificationBanner status={incident.plan.verification.status} findings={incident.plan.verification.findings} />

              {incident.evidence.length > 0 && (
                <>
                  <h3>Evidence</h3>
                  <ul className="evidence">
                    {incident.evidence.map((ev) => (
                      <li key={ev.id} className={`evidence-${ev.status}`}>
                        <span className="tag">{ev.id}</span>
                        <span className="evidence-type">{ev.type}</span>
                        {ev.status === "failed" ? (
                          <span className="evidence-failed">processing failed</span>
                        ) : (
                          <span>{ev.description}</span>
                        )}
                      </li>
                    ))}
                  </ul>
                </>
              )}

              <h3>Facts</h3>
              {incident.plan.facts.length === 0 && <p className="empty">No facts extracted.</p>}
              <ul className="facts">
                {incident.plan.facts.map((f) => (
                  <li key={f.id}>
                    <span className="tag">{f.id}</span> {f.text}
                    <span className="confidence">conf {f.confidence.toFixed(2)}</span>
                    <span className="sources">from {f.sourceIds.join(", ")}</span>
                  </li>
                ))}
              </ul>

              {incident.plan.unknowns.length > 0 && (
                <>
                  <h3>Unknowns</h3>
                  <ul className="unknowns">
                    {incident.plan.unknowns.map((u) => (
                      <li key={u.id}>
                        <span className="tag">{u.id}</span> {u.question} - <em>{u.reason}</em>
                      </li>
                    ))}
                  </ul>
                </>
              )}

              <h3>Hypotheses</h3>
              <ul className="hypotheses">
                {incident.plan.hypotheses.map((h) => (
                  <li key={h.id}>
                    <span className="tag">{h.id}</span> {h.claim}
                    <span className="confidence">conf {h.confidence.toFixed(2)}</span>
                    <span className="sources">supported by {h.supportingFactIds.join(", ")}</span>
                  </li>
                ))}
              </ul>

              <h3>Proposed actions</h3>
              <ul className="actions">
                {incident.plan.actions.map((a) => (
                  <li key={a.id} className={`decision-${a.decision}`}>
                    <div>
                      <span className="tag">{a.id}</span>{" "}
                      {editingActionId === a.id ? (
                        <textarea
                          className="edit-textarea"
                          value={editText}
                          onChange={(e) => setEditText(e.target.value)}
                          rows={2}
                        />
                      ) : (
                        a.action
                      )}
                      <span className="owner">owner: {a.owner}</span>
                      <span className="risk">risk: {a.risk}</span>
                      <span className="sources">because {a.rationaleFactIds.join(", ")}</span>
                    </div>
                    {a.decision === "pending" ? (
                      editingActionId === a.id ? (
                        <div className="decision-buttons">
                          <button onClick={() => submitEdit(a.id)}>Save</button>
                          <button onClick={() => setEditingActionId(null)}>Cancel</button>
                        </div>
                      ) : (
                        <div className="decision-buttons">
                          <button onClick={() => handleDecision(a.id, "approved")}>Approve</button>
                          <button onClick={() => handleDecision(a.id, "rejected")}>Reject</button>
                          <button onClick={() => startEdit(a.id, a.action)}>Edit</button>
                        </div>
                      )
                    ) : (
                      <span className="decision-label">{a.decision}</span>
                    )}
                  </li>
                ))}
              </ul>

              <div className="audit-toggle">
                <button onClick={toggleAudit} disabled={auditLoading}>
                  {audit ? "Hide audit log" : auditLoading ? "Loading audit log..." : "Show audit log"}
                </button>
              </div>

              {audit && (
                <ul className="audit-log">
                  {audit.map((entry, i) => (
                    <li key={i}>
                      <span className="audit-time">{new Date(entry.timestamp).toLocaleTimeString()}</span>
                      <span className="tag">{entry.eventType}</span>
                      <span className="audit-actor">{entry.actor}</span>
                      <span className="audit-detail">{entry.detail}</span>
                    </li>
                  ))}
                </ul>
              )}
            </>
          )}
        </section>
      </div>
    </div>
  );
}

function VerificationBanner({ status, findings }: { status: string; findings: string[] }) {
  return (
    <div className={`verification verification-${status}`}>
      <strong>Verifier: {status.toUpperCase()}</strong>
      {findings.length > 0 && (
        <ul>
          {findings.map((f, i) => (
            <li key={i}>{f}</li>
          ))}
        </ul>
      )}
    </div>
  );
}

export default App;
