using System.Text;
using System.Text.Json;
using IncidentCommander.Api.Models;
using Microsoft.Extensions.Options;

namespace IncidentCommander.Api.Services;

// Talks to the Meta Model API's chat-completions-style endpoint. The exact
// request/response shape here is our best guess from the hackathon docs
// (dev.meta.ai/docs/quickstart) - it's OpenAI-compatible as far as we can
// tell before we have a live key to test against. Once credits land, this is
// the one file that should need adjusting if the wire format differs.
public class MetaModelClient : IMetaModelClient
{
    private readonly HttpClient _http;
    private readonly MetaModelOptions _options;
    private readonly ILogger<MetaModelClient> _logger;

    private const string PromptVersion = "incident-plan-v1";

    public MetaModelClient(HttpClient http, IOptions<MetaModelOptions> options, ILogger<MetaModelClient> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IncidentPlan> GeneratePlanAsync(
        Incident incident,
        IReadOnlyList<EvidenceItem> evidence,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new MetaModelException(
                "META_MODEL_API_KEY is not set. Claim hackathon credits and set the env var before calling the model.");
        }

        var requestBody = new
        {
            model = _options.Model,
            messages = new object[]
            {
                new { role = "system", content = BuildSystemPrompt() },
                new { role = "user", content = BuildUserPrompt(incident, evidence) },
            },
            temperature = 0.2,
            response_format = new { type = "json_object" },
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl.TrimEnd('/')}/chat/completions")
        {
            Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"),
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _options.ApiKey);

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request, cancellationToken);
        }
        catch (TaskCanceledException ex)
        {
            throw new MetaModelException("Meta Model API call timed out.", ex);
        }
        catch (HttpRequestException ex)
        {
            throw new MetaModelException("Could not reach the Meta Model API.", ex);
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Meta Model API returned {StatusCode}: {Body}", response.StatusCode, body);
            throw new MetaModelException($"Meta Model API returned {(int)response.StatusCode}.");
        }

        var modelText = ExtractMessageContent(body);
        var plan = ParsePlan(modelText, incident.Id);
        return plan;
    }

    private static string ExtractMessageContent(string responseBody)
    {
        using var doc = JsonDocument.Parse(responseBody);
        return doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? throw new MetaModelException("Model response had no message content.");
    }

    private static IncidentPlan ParsePlan(string modelText, string incidentId)
    {
        try
        {
            var plan = JsonSerializer.Deserialize<IncidentPlan>(
                modelText,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (plan is null)
            {
                throw new MetaModelException("Model returned an empty plan.");
            }

            plan.IncidentId = incidentId;
            return plan;
        }
        catch (JsonException ex)
        {
            throw new MetaModelException("Model response was not valid JSON for the incident plan schema.", ex);
        }
    }

    private static string BuildSystemPrompt() => """
        You are the planning step of an incident response tool. You will be given an operator's
        report and a list of evidence items, each with an id. Respond with a single JSON object
        and nothing else, matching this shape:

        {
          "facts": [{"id": "F1", "text": "...", "sourceIds": ["E1"], "confidence": 0.0-1.0}],
          "unknowns": [{"id": "U1", "question": "...", "reason": "..."}],
          "hypotheses": [{"id": "H1", "claim": "...", "supportingFactIds": ["F1"], "confidence": 0.0-1.0}],
          "actions": [{"id": "A1", "action": "...", "owner": "Incident commander", "rationaleFactIds": ["F1"], "risk": "low|medium|high", "requiresApproval": true}]
        }

        Rules:
        - Every fact must cite at least one real evidence id from the list you were given.
        - Every hypothesis must cite at least one fact id. Every action must cite at least one fact id.
        - If something is unclear or unsupported, put it in "unknowns" instead of guessing.
        - Do not propose contacting emergency services, dispatching real-world responders, or any
          action outside this tool's own workspace. requiresApproval is always true for now.
        - Output JSON only, no prose before or after it.
        """;

    private static string BuildUserPrompt(Incident incident, IReadOnlyList<EvidenceItem> evidence)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Incident title: {incident.Title}");
        sb.AppendLine($"Severity: {incident.Severity}");
        sb.AppendLine("Operator report:");
        sb.AppendLine(incident.Report);
        sb.AppendLine();
        sb.AppendLine("Evidence:");
        foreach (var item in evidence)
        {
            sb.AppendLine($"- [{item.Id}] ({item.Type}) {item.Description}");
        }
        return sb.ToString();
    }
}
