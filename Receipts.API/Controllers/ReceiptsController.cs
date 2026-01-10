using Microsoft.AspNetCore.Mvc;
using Hangfire;
using Receipts.API.Services;
using Receipts.API.Contracts;
using Receipts.Infrastructure;
using Receipts.Infrastructure.Messages;
using System.Text.Json;

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

        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = typeof(ProcessReceiptMessage).FullName!,
            Payload = JsonSerializer.Serialize(new ProcessReceiptMessage(receipt.Id)),
            Status = OutboxStatus.New,
            CreatedAt = DateTime.UtcNow
        };

        using var transaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
            await dbContext.Receipts.AddAsync(receipt);
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