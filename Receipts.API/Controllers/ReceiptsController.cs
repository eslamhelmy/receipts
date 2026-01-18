using Microsoft.AspNetCore.Mvc;
using Hangfire;
using Receipts.API.Services;
using Receipts.API.Contracts;
using Receipts.Infrastructure;
using Receipts.Infrastructure.Messages;
using System.Text.Json;

namespace Receipts.API.Controllers;

/// <summary>
/// Provides endpoints for managing and processing receipts.
/// </summary>
[ApiController]
[Route("api/receipts")]
public class ReceiptsController(
    ReceiptsDbContext dbContext,
    IReceiptFileService receiptFileService)
    : ControllerBase
{
    /// <summary>
    /// Receives a receipt file and associated metadata, saves the receipt record, 
    /// and queues a processing message via the transactional outbox pattern.
    /// </summary>
    /// <param name="request">The receipt upload request containing the file and metadata.</param>
    /// <returns>An accepted result with the receipt ID if successful.</returns>
    [HttpPost]
    public async Task<IActionResult> ProcessReceipt([FromForm] CreateReceiptRequest request)
    {
        if (receiptFileService.TryValidate(request.File, out var fileValidationError))
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

        // Use a transaction to ensure both the receipt and the outbox message are saved atomically.
        // This prevents data inconsistency where a receipt is saved but never processed.
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