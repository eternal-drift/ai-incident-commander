using IncidentCommander.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;

namespace IncidentCommander.Api.Tests;

public class EvidenceStorageTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly EvidenceStorage _storage;

    public EvidenceStorageTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "ic-evidence-tests-" + Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(_tempRoot);
        _storage = new EvidenceStorage(new FakeWebHostEnvironment(_tempRoot));
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
    }

    private static IFormFile MakeFile(byte[] bytes, string contentType, string fileName = "upload.bin")
    {
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType,
        };
    }

    [Fact]
    public async Task SaveImageAsync_RejectsEmptyFile()
    {
        var file = MakeFile([], "image/png");

        await Assert.ThrowsAsync<EvidenceStorageException>(
            () => _storage.SaveImageAsync(file, "inc-1", CancellationToken.None));
    }

    [Fact]
    public async Task SaveImageAsync_RejectsOversizedFile()
    {
        var file = MakeFile(new byte[9 * 1024 * 1024], "image/png");

        var ex = await Assert.ThrowsAsync<EvidenceStorageException>(
            () => _storage.SaveImageAsync(file, "inc-1", CancellationToken.None));
        Assert.Contains("too large", ex.Message);
    }

    [Fact]
    public async Task SaveImageAsync_RejectsUnsupportedContentType()
    {
        var file = MakeFile([1, 2, 3], "application/pdf");

        var ex = await Assert.ThrowsAsync<EvidenceStorageException>(
            () => _storage.SaveImageAsync(file, "inc-1", CancellationToken.None));
        Assert.Contains("Unsupported image content type", ex.Message);
    }

    [Fact]
    public async Task SaveImageAsync_SavesValidFile_UsingGeneratedNameNotOriginal()
    {
        var file = MakeFile([1, 2, 3, 4], "image/png", fileName: "../../etc/passwd.png");

        var (storedFileName, bytes) = await _storage.SaveImageAsync(file, "inc-1", CancellationToken.None);

        Assert.Equal([1, 2, 3, 4], bytes);
        Assert.EndsWith(".png", storedFileName);
        Assert.DoesNotContain("passwd", storedFileName);
        Assert.DoesNotContain("..", storedFileName);
        Assert.True(File.Exists(Path.Combine(_tempRoot, "uploads", "inc-1", storedFileName)));
    }

    [Fact]
    public async Task SaveAudioAsync_RejectsFileOverAudioLimit()
    {
        var file = MakeFile(new byte[16 * 1024 * 1024], "audio/wav");

        await Assert.ThrowsAsync<EvidenceStorageException>(
            () => _storage.SaveAudioAsync(file, "inc-1", CancellationToken.None));
    }

    [Fact]
    public async Task SaveAudioAsync_AcceptsSupportedType()
    {
        var file = MakeFile([5, 6, 7], "audio/wav");

        var (storedFileName, bytes) = await _storage.SaveAudioAsync(file, "inc-1", CancellationToken.None);

        Assert.Equal([5, 6, 7], bytes);
        Assert.EndsWith(".wav", storedFileName);
    }

    private class FakeWebHostEnvironment(string contentRootPath) : IWebHostEnvironment
    {
        public string ContentRootPath { get; set; } = contentRootPath;
        public string EnvironmentName { get; set; } = "Testing";
        public string ApplicationName { get; set; } = "IncidentCommander.Api.Tests";
        public string WebRootPath { get; set; } = contentRootPath;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }
}
