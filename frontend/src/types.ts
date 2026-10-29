export type Severity = "low" | "medium" | "high" | "critical";

export interface Fact {
  id: string;
  text: string;
  sourceIds: string[];
  confidence: number;
}

export interface Unknown {
  id: string;
  question: string;
  reason: string;
}

export interface Hypothesis {
  id: string;
  claim: string;
  supportingFactIds: string[];
  confidence: number;
}

export type ActionDecision = "pending" | "approved" | "rejected" | "edited";

export interface ProposedAction {
  id: string;
  action: string;
  owner: string;
  rationaleFactIds: string[];
  risk: "low" | "medium" | "high";
  requiresApproval: boolean;
  decision: ActionDecision;
}

export interface Verification {
  status: "pass" | "warn" | "block";
  findings: string[];
}

export interface IncidentPlan {
  incidentId: string;
  facts: Fact[];
  unknowns: Unknown[];
  hypotheses: Hypothesis[];
  actions: ProposedAction[];
  verification: Verification;
}

export type EvidenceType = "text" | "image" | "audio";
export type EvidenceStatus = "ready" | "failed";

export interface EvidenceItem {
  id: string;
  type: EvidenceType;
  description: string;
  storedFileName: string | null;
  originalFileName: string | null;
  status: EvidenceStatus;
}

export interface IncidentDetail {
  id: string;
  title: string;
  severity: Severity;
  report: string;
  createdAt: string;
  evidence: EvidenceItem[];
  plan: IncidentPlan | null;
}

export interface IncidentSummary {
  id: string;
  title: string;
  severity: Severity;
  createdAt: string;
}

export interface AuditEntry {
  timestamp: string;
  eventType: string;
  actor: string;
  detail: string;
}
