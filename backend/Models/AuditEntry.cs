namespace IncidentCommander.Api.Models;

// One row per notable thing that happened to an incident. This is what lets
// a judge (or anyone else) reconstruct what the system did and when,
// instead of trusting a rewritten-in-place plan blob.
public class AuditEntry
{
    public int Id { get; set; }
    public string IncidentId { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public string EventType { get; set; } = string.Empty; // incident_created | evidence_processed | plan_generated | plan_generation_failed | action_decision
    public string Actor { get; set; } = "system"; // system | human
    public string Detail { get; set; } = string.Empty;
}
