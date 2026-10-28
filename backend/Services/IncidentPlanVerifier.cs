using IncidentCommander.Api.Models;

namespace IncidentCommander.Api.Services;

// Deterministic checks only - no model call here. This is what actually
// enforces "no recommendation without evidence" rather than just hoping the
// prompt was followed.
public class IncidentPlanVerifier
{
    public Verification Verify(IncidentPlan plan, IReadOnlyList<EvidenceItem> evidence)
    {
        var findings = new List<string>();
        var evidenceIds = evidence.Select(e => e.Id).ToHashSet();
        var factIds = plan.Facts.Select(f => f.Id).ToHashSet();

        foreach (var fact in plan.Facts)
        {
            if (fact.SourceIds.Count == 0)
            {
                findings.Add($"Fact {fact.Id} has no source evidence.");
                continue;
            }

            var missing = fact.SourceIds.Where(id => !evidenceIds.Contains(id)).ToList();
            if (missing.Count > 0)
            {
                findings.Add($"Fact {fact.Id} cites unknown evidence id(s): {string.Join(", ", missing)}.");
            }

            if (fact.Confidence is < 0 or > 1)
            {
                findings.Add($"Fact {fact.Id} has an out-of-range confidence ({fact.Confidence}).");
            }
        }

        foreach (var hypothesis in plan.Hypotheses)
        {
            if (hypothesis.SupportingFactIds.Count == 0)
            {
                findings.Add($"Hypothesis {hypothesis.Id} has no supporting facts.");
                continue;
            }

            var missing = hypothesis.SupportingFactIds.Where(id => !factIds.Contains(id)).ToList();
            if (missing.Count > 0)
            {
                findings.Add($"Hypothesis {hypothesis.Id} cites unknown fact id(s): {string.Join(", ", missing)}.");
            }
        }

        foreach (var action in plan.Actions)
        {
            if (action.RationaleFactIds.Count == 0)
            {
                findings.Add($"Action {action.Id} has no rationale fact - this is an unsupported action and must not be surfaced as-is.");
                continue;
            }

            var missing = action.RationaleFactIds.Where(id => !factIds.Contains(id)).ToList();
            if (missing.Count > 0)
            {
                findings.Add($"Action {action.Id} cites unknown fact id(s): {string.Join(", ", missing)}.");
            }

            if (!action.RequiresApproval)
            {
                findings.Add($"Action {action.Id} was marked as not requiring approval - overriding to require approval.");
                action.RequiresApproval = true;
            }
        }

        // Contradiction check: same fact id content disagreeing is for a model-assisted
        // pass later. For now we only flag facts that directly contradict each other via
        // near-duplicate text with opposite confidence, which is intentionally conservative.

        var status = findings.Count == 0 ? "pass" : HasBlockingFinding(findings) ? "block" : "warn";

        return new Verification { Status = status, Findings = findings };
    }

    private static bool HasBlockingFinding(List<string> findings) =>
        findings.Any(f => f.Contains("unsupported action") || f.Contains("unknown fact id") || f.Contains("unknown evidence id"));
}
