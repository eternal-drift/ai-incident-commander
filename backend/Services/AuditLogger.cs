using System.Text.Json;
using IncidentCommander.Api.Data;
using IncidentCommander.Api.Models;

namespace IncidentCommander.Api.Services;

public class AuditLogger
{
    private readonly AppDbContext _db;

    public AuditLogger(AppDbContext db)
    {
        _db = db;
    }

    public async Task LogAsync(string incidentId, string eventType, object detail, string actor = "system")
    {
        _db.AuditEntries.Add(new AuditEntry
        {
            IncidentId = incidentId,
            EventType = eventType,
            Actor = actor,
            Detail = JsonSerializer.Serialize(detail),
        });
        await _db.SaveChangesAsync();
    }
}
