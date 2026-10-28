import { useState } from "react";
import "./App.css";
import { ApiError, createIncident, decideAction } from "./api";
import type { IncidentDetail } from "./types";

const SEVERITIES = ["low", "medium", "high", "critical"];

function App() {
  const [title, setTitle] = useState("");
  const [severity, setSeverity] = useState("medium");
  const [report, setReport] = useState("");
  const [incident, setIncident] = useState<IncidentDetail | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setLoading(true);
    setError(null);
    try {
      const result = await createIncident({ title, severity, report });
      setIncident(result);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Something went wrong talking to the server.");
    } finally {
      setLoading(false);
    }
  }

  async function handleDecision(actionId: string, decision: "approved" | "rejected") {
    if (!incident) return;
    try {
      const updated = await decideAction(incident.id, actionId, decision);
      setIncident(updated);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Couldn't record that decision.");
    }
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
                      <span className="tag">{a.id}</span> {a.action}
                      <span className="owner">owner: {a.owner}</span>
                      <span className="risk">risk: {a.risk}</span>
                      <span className="sources">because {a.rationaleFactIds.join(", ")}</span>
                    </div>
                    {a.decision === "pending" ? (
                      <div className="decision-buttons">
                        <button onClick={() => handleDecision(a.id, "approved")}>Approve</button>
                        <button onClick={() => handleDecision(a.id, "rejected")}>Reject</button>
                      </div>
                    ) : (
                      <span className="decision-label">{a.decision}</span>
                    )}
                  </li>
                ))}
              </ul>
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
