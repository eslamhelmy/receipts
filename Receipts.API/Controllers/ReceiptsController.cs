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
    IReceiptFileService receiptFileService)
    : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> ProcessReceipt([FromForm] CreateReceiptRequest request)
    {
        // [ApiController] handles model binding/validation responses; here we add file-specific validation
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

        using var transaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
            await dbContext.Receipts.AddAsync(receipt);

            var outboxMessage = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                OccurredOn = DateTime.UtcNow,
                Type = "ReceiptCreated",
                Payload = System.Text.Json.JsonSerializer.Serialize(new { ReceiptId = receipt.Id }),
                ProcessedDate = null
            };

            await dbContext.OutboxMessages.AddAsync(outboxMessage);

            await dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }

        return Accepted(new { receiptId = receipt.Id });
    }
}