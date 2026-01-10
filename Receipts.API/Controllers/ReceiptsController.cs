using Microsoft.AspNetCore.Mvc;
using Hangfire;
using Receipts.API.Services;
using Receipts.API.Contracts;
using Receipts.Infrastructure;

namespace Receipts.API.Controllers;

[ApiController]
[Route("api/receipts")]
public class ReceiptsController(
    ReceiptsDbContext dbContext,
    IBackgroundJobClient backgroundJobClient,
    IReceiptFileService receiptFileService)
    : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> ProcessReceipt([FromForm] CreateReceiptRequest request)
    {
        if (!receiptFileService.TryValidate(request.File, out var fileValidationError))
        {
            return BadRequest(fileValidationError);
        }

        var receipt = new Receipt
        {
            Id = Guid.NewGuid(),
            Status = ReceiptStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            DocumentUrl = await receiptFileService.SimulateUploadAsync(request.File),
            UserId = request.UserId,
            Amount = request.Amount,
            Currency = request.Currency.ToUpperInvariant(),
            ReceiptDate = request.ReceiptDate
        };

        await dbContext.Receipts.AddAsync(receipt);
        await dbContext.SaveChangesAsync();

        backgroundJobClient.Enqueue<IReceiptProcessor>(processor => processor.ProcessReceipt(receipt.Id));

        return Accepted(new { receiptId = receipt.Id });
    }
}