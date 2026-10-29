namespace IncidentCommander.Api.Services;

// Bound from appsettings + environment. The API key deliberately has no
// appsettings default - it only ever comes from META_MODEL_API_KEY so it
// never ends up committed by accident.
public class MetaModelOptions
{
    public const string SectionName = "MetaModel";

    public string BaseUrl { get; set; } = "https://api.meta.ai/v1";
    public string Model { get; set; } = "muse-spark";
    public string ImageModel { get; set; } = "muse-image";
    public string VoiceModel { get; set; } = "muse-voice-transcribe";
    public string ApiKey { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 60;
}
