using Microsoft.AspNetCore.Http;

namespace Receipts.API.Services;

public interface IReceiptFileService
{
    bool TryValidate(IFormFile file, out string? error);
    Task<string> SimulateUploadAsync(IFormFile file, CancellationToken cancellationToken = default);
}
