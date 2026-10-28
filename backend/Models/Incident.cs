using System.Text.Json;

namespace IncidentCommander.Api.Models;

// The intake record plus whatever the pipeline produced for it. Stored as one
// row so the audit trail is just "give me this incident" rather than a join
// across five tables.
public class Incident
{
    public string Id { get; set; } = Guid.NewGuid().ToString("n");
    public string Title { get; set; } = string.Empty;
    public string Severity { get; set; } = "medium"; // low | medium | high | critical
    public string Report { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Raw model + evidence trail, kept as JSON blobs so we don't need a
    // separate table per nested list for a three-day build.
    public string? EvidenceJson { get; set; }
    public string? PlanJson { get; set; }
    public string? ModelName { get; set; }
    public string? PromptVersion { get; set; }

    public IncidentPlan? GetPlan() =>
        PlanJson is null ? null : JsonSerializer.Deserialize<IncidentPlan>(PlanJson);

    public void SetPlan(IncidentPlan plan) =>
        PlanJson = JsonSerializer.Serialize(plan);
}
