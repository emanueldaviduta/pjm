using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using ProjectManager.Application.Interfaces;

namespace ProjectManager.Infrastructure.Services;

/// <summary>Bound from the "CloudinarySettings" configuration section; the secret comes from the environment, never from files in the repository.</summary>
public class CloudinaryOptions
{
    public const string Section = "CloudinarySettings";

    public string? CloudName { get; set; }
    public string? ApiKey { get; set; }
    public string? ApiSecret { get; set; }

    public bool IsComplete =>
        !string.IsNullOrWhiteSpace(CloudName) && !string.IsNullOrWhiteSpace(ApiKey) && !string.IsNullOrWhiteSpace(ApiSecret);
}

/// <summary>
/// Uploads to Cloudinary with public ("upload") delivery, so the returned secure URL opens without signing in and
/// never expires. Cloudinary keeps the original file name and adds a random suffix, so links cannot be guessed.
/// Images go up as images; everything else as "raw", so the exact bytes are kept and files Cloudinary cannot
/// render (such as unusual PDFs) are not rejected.
/// </summary>
public class CloudinaryFileStorage(CloudinaryOptions options) : IFileStorage
{
    private readonly Cloudinary? _cloudinary = options.IsComplete
        ? new Cloudinary(new Account(options.CloudName, options.ApiKey, options.ApiSecret)) { Api = { Secure = true } }
        : null;

    public bool IsConfigured => _cloudinary != null;

    public async Task<StoredFile> UploadAsync(Stream content, string fileName, string contentType, string folder,
        CancellationToken cancellationToken = default)
    {
        if (_cloudinary == null)
        {
            throw new InvalidOperationException("File storage is not configured.");
        }

        RawUploadParams upload = contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
            ? new AutoUploadParams()
            : new RawUploadParams();
        upload.File = new FileDescription(fileName, content);
        upload.Folder = folder;
        upload.Type = "upload";
        upload.UseFilename = true;
        upload.UniqueFilename = true;
        upload.Overwrite = false;
        var result = await _cloudinary.UploadAsync(upload, cancellationToken: cancellationToken);
        if (result.Error != null || result.SecureUrl == null)
        {
            // Cloudinary's message, never the credentials.
            throw new IOException($"Upload rejected: {result.Error?.Message ?? "no URL returned"}");
        }

        return new StoredFile(result.PublicId, result.SecureUrl.ToString(), result.Bytes);
    }
}
