using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Pigment.Core.Interfaces;
using Renci.SshNet;

namespace Pigment.Infrastructure.Services;

/// <summary>
/// Uploads HR files to Azure Blob Storage and optionally to an SFTP target.
/// </summary>
public sealed class BlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string            _containerName;
    private readonly string            _blobPrefix;
    private readonly IConfiguration    _configuration;
    private readonly ILogger<BlobStorageService> _logger;

    public BlobStorageService(
        BlobServiceClient blobServiceClient,
        string containerName,
        string blobPrefix,
        IConfiguration configuration,
        ILogger<BlobStorageService> logger)
    {
        _blobServiceClient = blobServiceClient;
        _containerName     = containerName;
        _blobPrefix        = blobPrefix;
        _configuration     = configuration;
        _logger            = logger;
    }

    /// <inheritdoc />
    public async Task<string> UploadFileAsync(
        string fileName,
        byte[] fileContent,
        string contentType = "text/plain",
        CancellationToken cancellationToken = default)
    {
        // ── Blob Storage ─────────────────────────────────────────────────────
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        await containerClient.CreateIfNotExistsAsync(
            PublicAccessType.None, cancellationToken: cancellationToken);

        var blobName   = $"{_blobPrefix?.TrimEnd('/')}/{fileName}";
        var blobClient = containerClient.GetBlobClient(blobName);

        using var blobStream = new MemoryStream(fileContent);
        try
        {
            await blobClient.UploadAsync(
                blobStream,
                new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
                },
                cancellationToken);

            _logger.LogInformation("Blob uploaded: {BlobUri}", blobClient.Uri);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Azure Blob Storage upload failed for {FileName}", fileName);
            throw;
        }

        // ── Optional SFTP delivery ────────────────────────────────────────────
        var sftpHost = _configuration["Sftp:Host"];
        if (!string.IsNullOrWhiteSpace(sftpHost))
        {
            await UploadToSftpAsync(fileName, fileContent);
        }

        return blobClient.Uri.ToString();
    }

    /// <inheritdoc />
    public async Task<byte[]> DownloadFileAsync(
        string fileName,
        CancellationToken cancellationToken = default)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        var blobName        = $"{_blobPrefix?.TrimEnd('/')}/{fileName}";
        var blobClient      = containerClient.GetBlobClient(blobName);

        using var memoryStream = new MemoryStream();
        await blobClient.DownloadToAsync(memoryStream, cancellationToken);
        return memoryStream.ToArray();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> ListFilesAsync(
        string? prefix = null,
        CancellationToken cancellationToken = default)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        var fileNames       = new List<string>();

        await foreach (var blobItem in containerClient.GetBlobsAsync(
            prefix: prefix, cancellationToken: cancellationToken))
        {
            fileNames.Add(blobItem.Name);
        }

        return fileNames;
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private Task UploadToSftpAsync(string fileName, byte[] fileContent)
    {
        return Task.Run(() =>
        {
            var host       = _configuration["Sftp:Host"]       ?? throw new InvalidOperationException("Sftp:Host missing.");
            var port       = int.Parse(_configuration["Sftp:Port"] ?? "22");
            var username   = _configuration["Sftp:Username"]   ?? throw new InvalidOperationException("Sftp:Username missing.");
            var password   = _configuration["Sftp:Password"]   ?? throw new InvalidOperationException("Sftp:Password missing.");
            var remotePath = _configuration["Sftp:RemotePath"] ?? "/";

            using var sftp = new SftpClient(host, port, username, password);
            sftp.Connect();

            using var sftpStream    = new MemoryStream(fileContent);
            var remoteFilePath = $"{remotePath.TrimEnd('/')}/{fileName}";

            try
            {
                sftp.UploadFile(sftpStream, remoteFilePath);
                _logger.LogInformation("SFTP upload succeeded: {RemotePath}", remoteFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SFTP upload failed for {FileName}", fileName);
                // Non-fatal — blob upload already succeeded
            }
            finally
            {
                sftp.Disconnect();
            }
        });
    }
}
