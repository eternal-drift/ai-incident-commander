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

export interface IncidentDetail {
  id: string;
  title: string;
  severity: Severity;
  report: string;
  createdAt: string;
  plan: IncidentPlan | null;
}

export interface IncidentSummary {
  id: string;
  title: string;
  severity: Severity;
  createdAt: string;
}
