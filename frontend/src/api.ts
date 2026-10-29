import type { ActionDecision, AuditEntry, IncidentDetail, IncidentSummary } from "./types";

const API_BASE = import.meta.env.VITE_API_BASE ?? "http://localhost:5080";

export class ApiError extends Error {
  status: number;

  constructor(message: string, status: number) {
    super(message);
    this.status = status;
  }
}

async function handle<T>(res: Response): Promise<T> {
  if (!res.ok) {
    const body = await res.json().catch(() => ({ error: res.statusText }));
    throw new ApiError(body.error ?? res.statusText, res.status);
  }
  return res.json() as Promise<T>;
}

export function listIncidents(): Promise<IncidentSummary[]> {
  return fetch(`${API_BASE}/api/incidents`).then((r) => handle(r));
}

export function getIncident(id: string): Promise<IncidentDetail> {
  return fetch(`${API_BASE}/api/incidents/${id}`).then((r) => handle(r));
}

export function getAudit(id: string): Promise<AuditEntry[]> {
  return fetch(`${API_BASE}/api/incidents/${id}/audit`).then((r) => handle(r));
}

export function createIncident(input: {
  title: string;
  severity: string;
  report: string;
  image?: File;
  audio?: File;
}): Promise<IncidentDetail> {
  const form = new FormData();
  form.set("title", input.title);
  form.set("severity", input.severity);
  form.set("report", input.report);
  if (input.image) form.set("image", input.image);
  if (input.audio) form.set("audio", input.audio);

  return fetch(`${API_BASE}/api/incidents`, {
    method: "POST",
    body: form,
  }).then((r) => handle(r));
}

export function decideAction(
  incidentId: string,
  actionId: string,
  decision: ActionDecision,
  editedText?: string
): Promise<IncidentDetail> {
  return fetch(`${API_BASE}/api/incidents/${incidentId}/actions/${actionId}/decision`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ decision, editedText }),
  }).then((r) => handle(r));
}
