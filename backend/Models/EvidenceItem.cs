namespace IncidentCommander.Api.Models;

// One piece of raw input: the operator's text, an uploaded image, a voice
// note. Facts in the plan point back to these by id, which is what lets the
// UI walk from an action down to the original evidence.
public class EvidenceItem
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = "text"; // text | image | audio

    // For text evidence this is the text itself. For image/audio it's what
    // the vision/voice model extracted - filled in after processing, empty
    // (and Status == "failed") if that extraction didn't happen.
    public string Description { get; set; } = string.Empty;

    // Where the uploaded file lives on disk, relative to the uploads root.
    // Null for text evidence, which has nothing to store beyond the text.
    public string? StoredFileName { get; set; }
    public string? OriginalFileName { get; set; }

    public string Status { get; set; } = "ready"; // ready | failed
}
