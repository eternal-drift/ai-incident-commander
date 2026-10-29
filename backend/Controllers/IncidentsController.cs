using System.Text.Json;
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
    private readonly IVisionModelClient _visionClient;
    private readonly IVoiceModelClient _voiceClient;
    private readonly IncidentPlanVerifier _verifier;
    private readonly EvidenceStorage _storage;
    private readonly AuditLogger _audit;
    private readonly ILogger<IncidentsController> _logger;

    public IncidentsController(
        AppDbContext db,
        IMetaModelClient modelClient,
        IVisionModelClient visionClient,
        IVoiceModelClient voiceClient,
        IncidentPlanVerifier verifier,
        EvidenceStorage storage,
        AuditLogger audit,
        ILogger<IncidentsController> logger)
    {
        _db = db;
        _modelClient = modelClient;
        _visionClient = visionClient;
        _voiceClient = voiceClient;
        _verifier = verifier;
        _storage = storage;
        _audit = audit;
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

    [HttpGet("{id}/audit")]
    public async Task<ActionResult<IEnumerable<AuditEntryDto>>> GetAudit(string id)
    {
        var incident = await _db.Incidents.FindAsync(id);
        if (incident is null) return NotFound();

        var entries = (await _db.AuditEntries.Where(a => a.IncidentId == id).ToListAsync())
            .OrderBy(a => a.Timestamp)
            .Select(a => new AuditEntryDto(a.Timestamp, a.EventType, a.Actor, a.Detail))
            .ToList();

        return Ok(entries);
    }

    [HttpPost]
    [RequestSizeLimit(30 * 1024 * 1024)]
    public async Task<ActionResult<IncidentDetailDto>> Create([FromForm] CreateIncidentForm form, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(form.Title) || string.IsNullOrWhiteSpace(form.Report))
        {
            return BadRequest("Title and report are required.");
        }

        var incident = new Incident
        {
            Title = form.Title.Trim(),
            Severity = string.IsNullOrWhiteSpace(form.Severity) ? "medium" : form.Severity,
            Report = form.Report.Trim(),
        };

        _db.Incidents.Add(incident);
        await _db.SaveChangesAsync();
        await _audit.LogAsync(incident.Id, "incident_created", new { incident.Title, incident.Severity });

        var evidence = new List<EvidenceItem>
        {
            new() { Id = "E1", Type = "text", Description = incident.Report },
        };

        if (form.Image is not null)
        {
            evidence.Add(await ProcessImageAsync(incident, form.Image, NextEvidenceId(evidence), ct));
        }

        if (form.Audio is not null)
        {
            evidence.Add(await ProcessAudioAsync(incident, form.Audio, NextEvidenceId(evidence), ct));
        }

        incident.EvidenceJson = JsonSerializer.Serialize(evidence);
        await _db.SaveChangesAsync();

        try
        {
            var plan = await _modelClient.GeneratePlanAsync(incident, evidence, ct);
            plan.Verification = _verifier.Verify(plan, evidence);
            incident.SetPlan(plan);
            incident.ModelName = "meta-model-api";
            await _db.SaveChangesAsync();
            await _audit.LogAsync(incident.Id, "plan_generated", new
            {
                factCount = plan.Facts.Count,
                actionCount = plan.Actions.Count,
                verificationStatus = plan.Verification.Status,
            });
        }
        catch (MetaModelException ex)
        {
            _logger.LogWarning(ex, "Model call failed for incident {IncidentId}", incident.Id);
            await _audit.LogAsync(incident.Id, "plan_generation_failed", new { error = ex.Message });
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
        await _audit.LogAsync(
            incident.Id,
            "action_decision",
            new { actionId, decision = request.Decision },
            actor: "human");

        return Ok(ToDetailDto(incident));
    }

    private static string NextEvidenceId(List<EvidenceItem> evidence) => $"E{evidence.Count + 1}";

    private async Task<EvidenceItem> ProcessImageAsync(Incident incident, IFormFile file, string evidenceId, CancellationToken ct)
    {
        try
        {
            var (storedFileName, bytes) = await _storage.SaveImageAsync(file, incident.Id, ct);
            var description = await _visionClient.DescribeImageAsync(bytes, file.ContentType, ct);

            await _audit.LogAsync(incident.Id, "evidence_processed", new { evidenceId, type = "image", status = "ready" });

            return new EvidenceItem
            {
                Id = evidenceId,
                Type = "image",
                Description = description,
                StoredFileName = storedFileName,
                OriginalFileName = file.FileName,
                Status = "ready",
            };
        }
        catch (Exception ex) when (ex is EvidenceStorageException or MetaModelException)
        {
            _logger.LogWarning(ex, "Image evidence processing failed for incident {IncidentId}", incident.Id);
            await _audit.LogAsync(incident.Id, "evidence_processed", new { evidenceId, type = "image", status = "failed", error = ex.Message });

            return new EvidenceItem
            {
                Id = evidenceId,
                Type = "image",
                Description = string.Empty,
                OriginalFileName = file.FileName,
                Status = "failed",
            };
        }
    }

    private async Task<EvidenceItem> ProcessAudioAsync(Incident incident, IFormFile file, string evidenceId, CancellationToken ct)
    {
        try
        {
            var (storedFileName, bytes) = await _storage.SaveAudioAsync(file, incident.Id, ct);
            var transcript = await _voiceClient.TranscribeAsync(bytes, file.ContentType, ct);

            await _audit.LogAsync(incident.Id, "evidence_processed", new { evidenceId, type = "audio", status = "ready" });

            return new EvidenceItem
            {
                Id = evidenceId,
                Type = "audio",
                Description = transcript,
                StoredFileName = storedFileName,
                OriginalFileName = file.FileName,
                Status = "ready",
            };
        }
        catch (Exception ex) when (ex is EvidenceStorageException or MetaModelException)
        {
            _logger.LogWarning(ex, "Audio evidence processing failed for incident {IncidentId}", incident.Id);
            await _audit.LogAsync(incident.Id, "evidence_processed", new { evidenceId, type = "audio", status = "failed", error = ex.Message });

            return new EvidenceItem
            {
                Id = evidenceId,
                Type = "audio",
                Description = string.Empty,
                OriginalFileName = file.FileName,
                Status = "failed",
            };
        }
    }

    private IncidentDetailDto ToDetailDto(Incident incident)
    {
        var evidence = incident.EvidenceJson is null
            ? new List<EvidenceItem>()
            : JsonSerializer.Deserialize<List<EvidenceItem>>(incident.EvidenceJson) ?? new List<EvidenceItem>();

        return new IncidentDetailDto(
            incident.Id,
            incident.Title,
            incident.Severity,
            incident.Report,
            incident.CreatedAt,
            evidence,
            incident.GetPlan());
    }
}

public class CreateIncidentForm
{
    public string Title { get; set; } = string.Empty;
    public string Severity { get; set; } = "medium";
    public string Report { get; set; } = string.Empty;
    public IFormFile? Image { get; set; }
    public IFormFile? Audio { get; set; }
}

public record ActionDecisionRequest(string Decision, string? EditedText);

public record IncidentSummaryDto(string Id, string Title, string Severity, DateTimeOffset CreatedAt);

public record IncidentDetailDto(
    string Id,
    string Title,
    string Severity,
    string Report,
    DateTimeOffset CreatedAt,
    List<EvidenceItem> Evidence,
    IncidentPlan? Plan);

public record AuditEntryDto(DateTimeOffset Timestamp, string EventType, string Actor, string Detail);
