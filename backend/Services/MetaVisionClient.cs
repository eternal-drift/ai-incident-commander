using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace IncidentCommander.Api.Services;

// Same caveat as MetaModelClient: this assumes an OpenAI-style vision
// request (image as a base64 data URL inside a chat message) because that's
// the closest documented shape we could find before getting a live key.
// Re-check this against the real Meta Model API docs once credits land.
public class MetaVisionClient : IVisionModelClient
{
    private readonly HttpClient _http;
    private readonly MetaModelOptions _options;
    private readonly ILogger<MetaVisionClient> _logger;

    public MetaVisionClient(HttpClient http, IOptions<MetaModelOptions> options, ILogger<MetaVisionClient> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> DescribeImageAsync(byte[] imageBytes, string contentType, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new MetaModelException(
                "META_MODEL_API_KEY is not set. Claim hackathon credits and set the env var before calling the model.");
        }

        var dataUrl = $"data:{contentType};base64,{Convert.ToBase64String(imageBytes)}";

        var requestBody = new
        {
            model = _options.ImageModel,
            messages = new object[]
            {
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new
                        {
                            type = "text",
                            text = "Describe only what is visibly present in this image that would matter to an " +
                                   "incident responder - error messages, physical conditions, equipment state, " +
                                   "anything out of place. Do not guess at causes you can't see. Keep it factual " +
                                   "and concise, a few sentences.",
                        },
                        new { type = "image_url", image_url = new { url = dataUrl } },
                    },
                },
            },
            temperature = 0.1,
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
            throw new MetaModelException("Vision model call timed out.", ex);
        }
        catch (HttpRequestException ex)
        {
            throw new MetaModelException("Could not reach the Meta Model API for image description.", ex);
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Vision model call returned {StatusCode}: {Body}", response.StatusCode, body);
            throw new MetaModelException($"Vision model call returned {(int)response.StatusCode}.");
        }

        try
        {
            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString()
                   ?? throw new MetaModelException("Vision model response had no content.");
        }
        catch (JsonException ex)
        {
            throw new MetaModelException("Vision model response was not valid JSON.", ex);
        }
    }
}
