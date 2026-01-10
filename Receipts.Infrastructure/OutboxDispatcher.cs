using System.Text.Json;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Receipts.Infrastructure.Messages;

namespace Receipts.Infrastructure;

public class OutboxDispatcher(
    ReceiptsDbContext dbContext,
    IBackgroundJobClient backgroundJobClient) : IOutboxDispatcher
{
    public async Task DispatchAsync()
    {
        var messages = await dbContext.OutboxMessages
            .Where(m => m.Status == OutboxStatus.New || (m.Status == OutboxStatus.Failed && m.RetryCount < 3))
            .OrderBy(m => m.CreatedAt)
            .Take(20)
            .ToListAsync();

        foreach (var message in messages)
        {
            try
            {
                message.Status = OutboxStatus.Processing;

                if (message.Type == typeof(ProcessReceiptMessage).FullName)
                {
                    var payload = JsonSerializer.Deserialize<ProcessReceiptMessage>(message.Payload);
                    if (payload != null)
                    {
                        backgroundJobClient.Enqueue<IReceiptProcessor>(p => p.ProcessReceipt(payload.ReceiptId));
                    }
                }

                message.Status = OutboxStatus.Completed;
                message.ProcessedAt = DateTime.UtcNow;
                message.Error = null;
            }
            catch (Exception ex)
            {
                message.Status = OutboxStatus.Failed;
                message.RetryCount++;
                message.Error = ex.Message;
            }
        }

        await dbContext.SaveChangesAsync();
    }
}
