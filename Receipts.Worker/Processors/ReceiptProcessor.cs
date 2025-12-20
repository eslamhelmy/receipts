using Ardalis.GuardClauses;
using Microsoft.Extensions.Logging;
using Receipts.Infrastructure;

namespace Receipts.Worker.Processors;

public class ReceiptProcessor(ReceiptsDbContext dbContext, ILogger<ReceiptProcessor> logger)
    : IReceiptProcessor
{
    public async Task ProcessReceipt(Guid receiptId)
    {
        logger.LogInformation("Processing receipt {ReceiptId}...", receiptId);

        // Fetch the single receipt from the ReceiptsDatabase using the receiptId
        var receipt = await dbContext.Receipts.FindAsync(receiptId);
        Guard.Against.Null(receipt, nameof(receipt), $"Receipt with ID {receiptId} not found");

        // Simulate the long-running OCR processing
        logger.LogInformation("Starting OCR processing for receipt {ReceiptId}...", receiptId);
        await Task.Delay(5000);

        // Update the receipt's Status to 'Processed'
        receipt.Status = ReceiptStatus.Processed;

        // Save the changes
        await dbContext.SaveChangesAsync();

        logger.LogInformation("Receipt {ReceiptId} successfully processed", receiptId);
    }
}
