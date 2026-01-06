using System.Text.Json;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Receipts.Infrastructure;

namespace Receipts.Worker.Processors;

public class OutboxProcessor(
    ReceiptsDbContext dbContext,
    IBackgroundJobClient backgroundJobClient,
    ILogger<OutboxProcessor> logger) : IOutboxProcessor
{
    public async Task ProcessMessagesAsync()
    {
        logger.LogInformation("Polling outbox messages...");

        var messages = await dbContext.OutboxMessages
            .Where(m => m.ProcessedDate == null)
            .OrderBy(m => m.OccurredOn)
            .Take(20)
            .ToListAsync();

        if (messages.Count == 0)
        {
            return;
        }

        foreach (var message in messages)
        {
            try
            {
                if (message.Type == "ReceiptCreated")
                {
                    var payload = JsonSerializer.Deserialize<JsonElement>(message.Payload);
                    var receiptId = payload.GetProperty("ReceiptId").GetGuid();

                    backgroundJobClient.Enqueue<IReceiptProcessor>(p => p.ProcessReceipt(receiptId));
                }

                message.ProcessedDate = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing outbox message {MessageId}", message.Id);
            }
        }

        await dbContext.SaveChangesAsync();
    }
}
