using IncidentCommander.Api.Data;
using IncidentCommander.Api.Models;
using IncidentCommander.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IncidentCommander.Api.Controllers;

[ApiController]
[Route("api/incidents")]
public class IncidentsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IMetaModelClient _modelClient;
    private readonly IncidentPlanVerifier _verifier;
    private readonly ILogger<IncidentsController> _logger;

    public IncidentsController(
        AppDbContext db,
        IMetaModelClient modelClient,
        IncidentPlanVerifier verifier,
        ILogger<IncidentsController> logger)
    {
        _db = db;
        _modelClient = modelClient;
        _verifier = verifier;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<IncidentSummaryDto>>> List()
    {
        // Small demo dataset - ordering in memory avoids SQLite's lack of
        // DateTimeOffset comparison support in generated SQL.
        var incidents = (await _db.Incidents.ToListAsync())
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => new IncidentSummaryDto(i.Id, i.Title, i.Severity, i.CreatedAt))
            .ToList();

        return Ok(incidents);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<IncidentDetailDto>> Get(string id)
    {
        var incident = await _db.Incidents.FindAsync(id);
        if (incident is null) return NotFound();

        return Ok(ToDetailDto(incident));
    }

    [HttpPost]
    public async Task<ActionResult<IncidentDetailDto>> Create([FromBody] CreateIncidentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Report))
        {
            return BadRequest("Title and report are required.");
        }

        var incident = new Incident
        {
            Title = request.Title.Trim(),
            Severity = string.IsNullOrWhiteSpace(request.Severity) ? "medium" : request.Severity,
            Report = request.Report.Trim(),
        };

        // Day 1: the operator's own text is evidence item E1. Image/audio
        // evidence gets added to this list once intake supports uploads.
        var evidence = new List<EvidenceItem>
        {
            new() { Id = "E1", Type = "text", Description = incident.Report },
        };
        incident.EvidenceJson = System.Text.Json.JsonSerializer.Serialize(evidence);

        _db.Incidents.Add(incident);
        await _db.SaveChangesAsync();

        try
        {
            var plan = await _modelClient.GeneratePlanAsync(incident, evidence);
            plan.Verification = _verifier.Verify(plan, evidence);
            incident.SetPlan(plan);
            incident.ModelName = "meta-model-api";
            await _db.SaveChangesAsync();
        }
        catch (MetaModelException ex)
        {
            _logger.LogWarning(ex, "Model call failed for incident {IncidentId}", incident.Id);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                incidentId = incident.Id,
                error = ex.Message,
            });
        }

        return CreatedAtAction(nameof(Get), new { id = incident.Id }, ToDetailDto(incident));
    }

    [HttpPost("{id}/actions/{actionId}/decision")]
    public async Task<ActionResult<IncidentDetailDto>> DecideAction(string id, string actionId, [FromBody] ActionDecisionRequest request)
    {
        var incident = await _db.Incidents.FindAsync(id);
        if (incident is null) return NotFound();

        var plan = incident.GetPlan();
        if (plan is null) return NotFound("No plan exists for this incident yet.");

        var action = plan.Actions.FirstOrDefault(a => a.Id == actionId);
        if (action is null) return NotFound("No such action on this incident.");

        if (request.Decision is not ("approved" or "rejected" or "edited"))
        {
            return BadRequest("Decision must be approved, rejected, or edited.");
        }

        action.Decision = request.Decision;
        if (request.Decision == "edited" && !string.IsNullOrWhiteSpace(request.EditedText))
        {
            action.Action = request.EditedText;
        }

        incident.SetPlan(plan);
        await _db.SaveChangesAsync();

        return Ok(ToDetailDto(incident));
    }

    private static IncidentDetailDto ToDetailDto(Incident incident) => new(
        incident.Id,
        incident.Title,
        incident.Severity,
        incident.Report,
        incident.CreatedAt,
        incident.GetPlan());
}

public record CreateIncidentRequest(string Title, string Severity, string Report);

public record ActionDecisionRequest(string Decision, string? EditedText);

public record IncidentSummaryDto(string Id, string Title, string Severity, DateTimeOffset CreatedAt);

public record IncidentDetailDto(
    string Id,
    string Title,
    string Severity,
    string Report,
    DateTimeOffset CreatedAt,
    IncidentPlan? Plan);
