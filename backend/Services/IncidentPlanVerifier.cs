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

        findings.AddRange(FindContradictions(plan.Facts));

        var status = findings.Count == 0 ? "pass" : HasBlockingFinding(findings) ? "block" : "warn";

        return new Verification { Status = status, Findings = findings };
    }

    private static bool HasBlockingFinding(List<string> findings) =>
        findings.Any(f => f.Contains("unsupported action") || f.Contains("unknown fact id") || f.Contains("unknown evidence id"));

    // Deliberately dumb keyword-opposite check, not real NLP contradiction
    // detection. It's here so two facts that flatly disagree ("service is up"
    // vs "service is down") get flagged as a warning instead of silently
    // feeding both into the same plan. False positives are expected and
    // fine - better to over-flag than to assert false certainty.
    private static readonly (string A, string B)[] OppositePairs =
    [
        ("online", "offline"),
        ("up", "down"),
        ("increased", "decreased"),
        ("open", "closed"),
        ("confirmed", "unconfirmed"),
        ("present", "absent"),
        ("working", "broken"),
        ("resolved", "unresolved"),
        ("stable", "unstable"),
    ];

    private static List<string> FindContradictions(List<Fact> facts)
    {
        var findings = new List<string>();

        for (var i = 0; i < facts.Count; i++)
        {
            for (var j = i + 1; j < facts.Count; j++)
            {
                var a = facts[i].Text.ToLowerInvariant();
                var b = facts[j].Text.ToLowerInvariant();

                foreach (var (word, opposite) in OppositePairs)
                {
                    var aHasWord = a.Contains(word) && !b.Contains(word);
                    var bHasOpposite = b.Contains(opposite) && !a.Contains(opposite);
                    var aHasOpposite = a.Contains(opposite) && !b.Contains(opposite);
                    var bHasWord = b.Contains(word) && !a.Contains(word);

                    if ((aHasWord && bHasOpposite) || (aHasOpposite && bHasWord))
                    {
                        findings.Add(
                            $"Facts {facts[i].Id} and {facts[j].Id} may contradict each other (\"{word}\" vs \"{opposite}\") - treat as unresolved, don't average them into one conclusion.");
                    }
                }
            }
        }

        return findings;
    }
}
