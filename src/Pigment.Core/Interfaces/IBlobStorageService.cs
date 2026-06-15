namespace Pigment.Core.Interfaces;

/// <summary>
/// Azure Blob Storage abstraction — implemented in Pigment.Infrastructure.
/// </summary>
public interface IBlobStorageService
{
    /// <summary>Uploads raw bytes and returns the public blob URI.</summary>
    Task<string> UploadFileAsync(
        string fileName,
        byte[] fileContent,
        string contentType = "text/plain",
        CancellationToken cancellationToken = default);

    Task<byte[]> DownloadFileAsync(
        string fileName,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> ListFilesAsync(
        string? prefix = null,
        CancellationToken cancellationToken = default);
}
