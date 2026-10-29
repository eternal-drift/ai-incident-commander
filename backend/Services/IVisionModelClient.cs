namespace IncidentCommander.Api.Services;

public interface IVisionModelClient
{
    // Give it the bytes of an uploaded image and get back a plain-language
    // description of what's visible. Should stick to what's actually in
    // frame - no guessing at causes, that's the reasoning model's job.
    Task<string> DescribeImageAsync(byte[] imageBytes, string contentType, CancellationToken cancellationToken = default);
}
