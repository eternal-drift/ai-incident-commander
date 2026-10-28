namespace IncidentCommander.Api.Models;

// One piece of raw input: the operator's text, an uploaded image, a voice
// note. Facts in the plan point back to these by id, which is what lets the
// UI walk from an action down to the original evidence.
public class EvidenceItem
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = "text"; // text | image | audio
    public string Description { get; set; } = string.Empty;
}
