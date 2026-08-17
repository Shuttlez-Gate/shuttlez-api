namespace Shuttlez.Application.Common.Interfaces;

public interface IFileStorageService
{
    /// <summary>
    /// يحفظ الملف تحت مجلد نسبي (مثل drivers/{id}) ويعيد المسار العام /uploads/...
    /// </summary>
    Task<StoredFileResult> SaveAsync(
        Stream content,
        string fileName,
        string contentType,
        string relativeFolder,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default);
}

public record StoredFileResult(
    string StoragePath,
    string FileName,
    string ContentType,
    long SizeBytes);
