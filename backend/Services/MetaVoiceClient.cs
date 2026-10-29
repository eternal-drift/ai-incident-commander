using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace IncidentCommander.Api.Services;

// Meta's ASR endpoint: POST /v1/asr/transcribe, multipart with a JSON "request"
// part (mode/model/audioEncoding) and an "audio" file part. Audio must be
// mono 16-bit PCM WAV at 16kHz or 24kHz. Response has the text at "transcript".
// See https://dev.meta.ai/docs/speech-to-text.
public class MetaVoiceClient : IVoiceModelClient
{
    private readonly HttpClient _http;
    private readonly MetaModelOptions _options;
    private readonly ILogger<MetaVoiceClient> _logger;

    public MetaVoiceClient(HttpClient http, IOptions<MetaModelOptions> options, ILogger<MetaVoiceClient> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> TranscribeAsync(byte[] audioBytes, string contentType, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new MetaModelException(
                "META_MODEL_API_KEY is not set. Claim hackathon credits and set the env var before calling the model.");
        }

        using var content = new MultipartFormDataContent();

        var requestSettings = JsonSerializer.Serialize(new
        {
            mode = "PUSH_TO_TALK",
            model = _options.VoiceModel,
            audioEncoding = "WAV",
        });
        var requestPart = new StringContent(requestSettings, Encoding.UTF8, "application/json");
        content.Add(requestPart, "request");

        var fileContent = new ByteArrayContent(audioBytes);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        content.Add(fileContent, "audio", "evidence-audio.wav");

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl.TrimEnd('/')}/asr/transcribe")
        {
            Content = content,
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _options.ApiKey);

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request, cancellationToken);
        }
        catch (TaskCanceledException ex)
        {
            throw new MetaModelException("Voice transcription call timed out.", ex);
        }
        catch (HttpRequestException ex)
        {
            throw new MetaModelException("Could not reach the Meta Model API for transcription.", ex);
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Voice transcription call returned {StatusCode}: {Body}", response.StatusCode, body);
            throw new MetaModelException($"Voice transcription call returned {(int)response.StatusCode}.");
        }

        try
        {
            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.GetProperty("transcript").GetString()
                   ?? throw new MetaModelException("Transcription response had no transcript.");
        }
        catch (JsonException ex)
        {
            throw new MetaModelException("Transcription response was not valid JSON.", ex);
        }
    }
}
