namespace ProjectManager.Application.Interfaces;

public record StoredFile(string PublicId, string Url, long Bytes);

/// <summary>External storage for task attachments. Files are served from the returned public URL.</summary>
public interface IFileStorage
{
    /// <summary>False when the storage account is not configured; no upload may be attempted then.</summary>
    bool IsConfigured { get; }

    Task<StoredFile> UploadAsync(Stream content, string fileName, string contentType, string folder, CancellationToken cancellationToken = default);
}
