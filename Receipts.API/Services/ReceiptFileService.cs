using Microsoft.AspNetCore.Http;

namespace Receipts.API.Services;

public class ReceiptFileService : IReceiptFileService
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "image/jpeg",
        "image/png"
    };

    private const int MaxFileBytes = 1_000_000; // 1 MB limit for sample

    public bool TryValidate(IFormFile file, out string? error)
    {
        error = null;

        if (file == null || file.Length == 0)
        {
            error = "File is required.";
            return false;
        }

        if (file.Length > MaxFileBytes)
        {
            error = $"File too large. Limit: {MaxFileBytes} bytes.";
            return false;
        }

        if (!AllowedContentTypes.Contains(file.ContentType))
        {
            error = $"Unsupported content type '{file.ContentType}'. Allowed: {string.Join(", ", AllowedContentTypes)}";
            return false;
        }

        return true;
    }

    public async Task<string> SimulateUploadAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        // In production, stream directly to blob/object storage. Here we simulate and discard the content.
        await using var stream = new MemoryStream();
        await file.CopyToAsync(stream, cancellationToken);

        var extension = Path.HasExtension(file.FileName) ? Path.GetExtension(file.FileName) : ".bin";
        var normalizedExtension = string.IsNullOrWhiteSpace(extension) ? ".bin" : extension;
        var storedFileName = $"{Guid.NewGuid():N}{normalizedExtension}";

        return $"https://storage.example.com/receipts/{storedFileName}?contentType={Uri.EscapeDataString(file.ContentType)}";
    }
}
