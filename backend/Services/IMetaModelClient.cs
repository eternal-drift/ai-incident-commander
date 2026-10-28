using IncidentCommander.Api.Models;

namespace IncidentCommander.Api.Services;

public interface IMetaModelClient
{
    // Takes the raw intake + whatever evidence we've extracted so far and
    // comes back with a structured plan. Throws MetaModelException on
    // anything that stops us from getting a usable plan - a missing key,
    // a timeout, a response that isn't valid JSON.
    Task<IncidentPlan> GeneratePlanAsync(
        Incident incident,
        IReadOnlyList<EvidenceItem> evidence,
        CancellationToken cancellationToken = default);
}

public class MetaModelException : Exception
{
    public MetaModelException(string message, Exception? inner = null) : base(message, inner) { }
}
