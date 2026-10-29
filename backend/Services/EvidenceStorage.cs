namespace IncidentCommander.Api.Services;

public class EvidenceStorageException : Exception
{
    public EvidenceStorageException(string message) : base(message) { }
}

// Saves uploaded evidence files under a per-incident folder, using a
// generated name rather than whatever the browser sent us - that's the
// "sanitize filenames" line from the spec taken literally rather than
// trying to clean up an attacker-controlled string.
public class EvidenceStorage
{
    private static readonly Dictionary<string, string> AllowedImageTypes = new()
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp",
    };

    private static readonly Dictionary<string, string> AllowedAudioTypes = new()
    {
        ["audio/mpeg"] = ".mp3",
        ["audio/wav"] = ".wav",
        ["audio/x-wav"] = ".wav",
        ["audio/mp4"] = ".m4a",
        ["audio/ogg"] = ".ogg",
        ["audio/webm"] = ".webm",
    };

    private const long MaxImageBytes = 8 * 1024 * 1024;
    private const long MaxAudioBytes = 15 * 1024 * 1024;

    private readonly string _rootPath;

    public EvidenceStorage(IWebHostEnvironment env)
    {
        _rootPath = Path.Combine(env.ContentRootPath, "uploads");
        Directory.CreateDirectory(_rootPath);
    }

    public async Task<(string StoredFileName, byte[] Bytes)> SaveImageAsync(IFormFile file, string incidentId, CancellationToken ct)
    {
        return await SaveAsync(file, incidentId, AllowedImageTypes, MaxImageBytes, "image", ct);
    }

    public async Task<(string StoredFileName, byte[] Bytes)> SaveAudioAsync(IFormFile file, string incidentId, CancellationToken ct)
    {
        return await SaveAsync(file, incidentId, AllowedAudioTypes, MaxAudioBytes, "audio", ct);
    }

    private async Task<(string StoredFileName, byte[] Bytes)> SaveAsync(
        IFormFile file,
        string incidentId,
        Dictionary<string, string> allowedTypes,
        long maxBytes,
        string kind,
        CancellationToken ct)
    {
        if (file.Length == 0)
        {
            throw new EvidenceStorageException($"Uploaded {kind} file is empty.");
        }

        if (file.Length > maxBytes)
        {
            throw new EvidenceStorageException($"Uploaded {kind} file is too large (max {maxBytes / 1024 / 1024} MB).");
        }

        if (!allowedTypes.TryGetValue(file.ContentType, out var extension))
        {
            throw new EvidenceStorageException($"Unsupported {kind} content type: {file.ContentType}.");
        }

        var incidentDir = Path.Combine(_rootPath, incidentId);
        Directory.CreateDirectory(incidentDir);

        var storedFileName = $"{Guid.NewGuid():n}{extension}";
        var fullPath = Path.Combine(incidentDir, storedFileName);

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        var bytes = ms.ToArray();

        await File.WriteAllBytesAsync(fullPath, bytes, ct);

        return (storedFileName, bytes);
    }
}
