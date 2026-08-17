using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Shuttlez.Application.Common.Interfaces;

namespace Shuttlez.Infrastructure.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<LocalFileStorageService> _logger;

    public LocalFileStorageService(IWebHostEnvironment env, ILogger<LocalFileStorageService> logger)
    {
        _env = env;
        _logger = logger;
    }

    public async Task<StoredFileResult> SaveAsync(
        Stream content,
        string fileName,
        string contentType,
        string relativeFolder,
        CancellationToken cancellationToken = default)
    {
        var safeName = SanitizeFileName(fileName);
        var folder = Path.Combine(GetUploadsRoot(), NormalizeFolder(relativeFolder));
        Directory.CreateDirectory(folder);

        var storedName = $"{Guid.NewGuid():N}_{safeName}";
        var fullPath = Path.Combine(folder, storedName);

        await using (var fs = File.Create(fullPath))
        {
            await content.CopyToAsync(fs, cancellationToken);
        }

        var size = new FileInfo(fullPath).Length;
        var publicPath = $"/uploads/{NormalizeFolder(relativeFolder).Replace('\\', '/')}/{storedName}"
            .Replace("//", "/");

        return new StoredFileResult(publicPath, safeName, contentType, size);
    }

    public Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storagePath) || !storagePath.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
        {
            return Task.CompletedTask;
        }

        var relative = storagePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        // storagePath = /uploads/... → relative under content root web root
        var fullPath = Path.Combine(GetWebRoot(), relative);
        try
        {
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete stored file {Path}", fullPath);
        }

        return Task.CompletedTask;
    }

    private string GetWebRoot()
    {
        var webRoot = _env.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRoot))
        {
            webRoot = Path.Combine(_env.ContentRootPath, "wwwroot");
            Directory.CreateDirectory(webRoot);
        }

        return webRoot;
    }

    private string GetUploadsRoot()
    {
        var root = Path.Combine(GetWebRoot(), "uploads");
        Directory.CreateDirectory(root);
        return root;
    }

    private static string NormalizeFolder(string relativeFolder)
    {
        var cleaned = relativeFolder
            .Replace('\\', '/')
            .Trim('/')
            .Replace("..", string.Empty);
        return cleaned.Replace('/', Path.DirectorySeparatorChar);
    }

    private static string SanitizeFileName(string fileName)
    {
        var name = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(name))
        {
            name = "file.bin";
        }

        foreach (var c in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(c, '_');
        }

        return name.Length > 120 ? name[^120..] : name;
    }
}
