using IncidentCommander.Api.Models;
using IncidentCommander.Api.Services;

namespace IncidentCommander.Api.Tests;

public class IncidentPlanVerifierTests
{
    private readonly IncidentPlanVerifier _verifier = new();

    private static List<EvidenceItem> Evidence(params string[] ids) =>
        ids.Select(id => new EvidenceItem { Id = id, Type = "text", Description = "x" }).ToList();

    [Fact]
    public void Pass_WhenEveryClaimIsGroundedAndNoContradictions()
    {
        var plan = new IncidentPlan
        {
            Facts = [new Fact { Id = "F1", Text = "Service is up.", SourceIds = ["E1"], Confidence = 0.9 }],
            Hypotheses = [new Hypothesis { Id = "H1", Claim = "All good.", SupportingFactIds = ["F1"], Confidence = 0.8 }],
            Actions = [new ProposedAction { Id = "A1", Action = "Monitor.", RationaleFactIds = ["F1"], RequiresApproval = true }],
        };

        var result = _verifier.Verify(plan, Evidence("E1"));

        Assert.Equal("pass", result.Status);
        Assert.Empty(result.Findings);
    }

    [Fact]
    public void Block_WhenFactCitesUnknownEvidenceId()
    {
        var plan = new IncidentPlan
        {
            Facts = [new Fact { Id = "F1", Text = "Something happened.", SourceIds = ["E99"], Confidence = 0.9 }],
        };

        var result = _verifier.Verify(plan, Evidence("E1"));

        Assert.Equal("block", result.Status);
        Assert.Contains(result.Findings, f => f.Contains("F1") && f.Contains("unknown evidence id"));
    }

    [Fact]
    public void Warn_WhenFactHasNoSourceIds()
    {
        var plan = new IncidentPlan
        {
            Facts = [new Fact { Id = "F1", Text = "Unsupported claim.", SourceIds = [], Confidence = 0.9 }],
        };

        var result = _verifier.Verify(plan, Evidence("E1"));

        Assert.Equal("warn", result.Status);
        Assert.Contains(result.Findings, f => f.Contains("F1") && f.Contains("no source evidence"));
    }

    [Fact]
    public void Block_WhenActionHasNoRationaleFacts()
    {
        var plan = new IncidentPlan
        {
            Actions = [new ProposedAction { Id = "A1", Action = "Do something risky.", RationaleFactIds = [] }],
        };

        var result = _verifier.Verify(plan, Evidence());

        Assert.Equal("block", result.Status);
        Assert.Contains(result.Findings, f => f.Contains("A1") && f.Contains("unsupported action"));
    }

    [Fact]
    public void Block_WhenActionCitesUnknownFactId()
    {
        var plan = new IncidentPlan
        {
            Facts = [new Fact { Id = "F1", Text = "A real fact.", SourceIds = ["E1"], Confidence = 0.9 }],
            Actions = [new ProposedAction { Id = "A1", Action = "Do something.", RationaleFactIds = ["F99"] }],
        };

        var result = _verifier.Verify(plan, Evidence("E1"));

        Assert.Equal("block", result.Status);
        Assert.Contains(result.Findings, f => f.Contains("A1") && f.Contains("unknown fact id"));
    }

    [Fact]
    public void ForcesRequiresApproval_EvenIfModelSaidOtherwise()
    {
        var plan = new IncidentPlan
        {
            Facts = [new Fact { Id = "F1", Text = "A real fact.", SourceIds = ["E1"], Confidence = 0.9 }],
            Actions = [new ProposedAction { Id = "A1", Action = "Do something.", RationaleFactIds = ["F1"], RequiresApproval = false }],
        };

        _verifier.Verify(plan, Evidence("E1"));

        Assert.True(plan.Actions[0].RequiresApproval);
    }

    [Fact]
    public void Warn_WhenHypothesisHasNoSupportingFacts()
    {
        var plan = new IncidentPlan
        {
            Hypotheses = [new Hypothesis { Id = "H1", Claim = "A guess.", SupportingFactIds = [] }],
        };

        var result = _verifier.Verify(plan, Evidence());

        Assert.Equal("warn", result.Status);
        Assert.Contains(result.Findings, f => f.Contains("H1") && f.Contains("no supporting facts"));
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    public void Warn_WhenFactConfidenceIsOutOfRange(double confidence)
    {
        var plan = new IncidentPlan
        {
            Facts = [new Fact { Id = "F1", Text = "A fact.", SourceIds = ["E1"], Confidence = confidence }],
        };

        var result = _verifier.Verify(plan, Evidence("E1"));

        Assert.Contains(result.Findings, f => f.Contains("F1") && f.Contains("out-of-range confidence"));
    }

    [Fact]
    public void Warn_WhenTwoFactsContradictEachOther()
    {
        var plan = new IncidentPlan
        {
            Facts =
            [
                new Fact { Id = "F1", Text = "The service is up.", SourceIds = ["E1"], Confidence = 0.9 },
                new Fact { Id = "F2", Text = "The service is down.", SourceIds = ["E2"], Confidence = 0.9 },
            ],
        };

        var result = _verifier.Verify(plan, Evidence("E1", "E2"));

        Assert.Equal("warn", result.Status);
        Assert.Contains(result.Findings, f => f.Contains("F1") && f.Contains("F2") && f.Contains("contradict"));
    }

    [Fact]
    public void Pass_WhenFactsShareNoOppositeKeywords()
    {
        var plan = new IncidentPlan
        {
            Facts =
            [
                new Fact { Id = "F1", Text = "CPU usage is high.", SourceIds = ["E1"], Confidence = 0.9 },
                new Fact { Id = "F2", Text = "Disk usage is normal.", SourceIds = ["E2"], Confidence = 0.9 },
            ],
        };

        var result = _verifier.Verify(plan, Evidence("E1", "E2"));

        Assert.Equal("pass", result.Status);
        Assert.Empty(result.Findings);
    }
}
