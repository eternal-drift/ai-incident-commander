namespace IncidentCommander.Api.Services;

public interface IVoiceModelClient
{
    // Give it the bytes of an uploaded audio clip and get back a transcript.
    // The caller shows this to the operator so they can correct it before
    // it gets treated as evidence.
    Task<string> TranscribeAsync(byte[] audioBytes, string contentType, CancellationToken cancellationToken = default);
}
