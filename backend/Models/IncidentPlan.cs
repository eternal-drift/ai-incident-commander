namespace IncidentCommander.Api.Models;

// Mirrors the data contract in the build spec. Everything downstream (the UI,
// the verifier, the audit log) reads this shape instead of raw model text.

public class IncidentPlan
{
    public string IncidentId { get; set; } = string.Empty;
    public List<Fact> Facts { get; set; } = new();
    public List<Unknown> Unknowns { get; set; } = new();
    public List<Hypothesis> Hypotheses { get; set; } = new();
    public List<ProposedAction> Actions { get; set; } = new();
    public Verification Verification { get; set; } = new();
}

public class Fact
{
    public string Id { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public List<string> SourceIds { get; set; } = new();
    public double Confidence { get; set; }
}

public class Unknown
{
    public string Id { get; set; } = string.Empty;
    public string Question { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

public class Hypothesis
{
    public string Id { get; set; } = string.Empty;
    public string Claim { get; set; } = string.Empty;
    public List<string> SupportingFactIds { get; set; } = new();
    public double Confidence { get; set; }
}

public class ProposedAction
{
    public string Id { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public List<string> RationaleFactIds { get; set; } = new();
    public string Risk { get; set; } = "low";
    public bool RequiresApproval { get; set; } = true;

    // Not part of the model's output - this is set by a human in the UI and
    // persisted alongside the plan for the audit trail.
    public string Decision { get; set; } = "pending"; // pending | approved | rejected | edited
}

public class Verification
{
    public string Status { get; set; } = "pass"; // pass | warn | block
    public List<string> Findings { get; set; } = new();
}
