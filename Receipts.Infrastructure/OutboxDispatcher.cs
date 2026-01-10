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
            .Where(m => m.Status == OutboxStatus.New)
            .OrderBy(m => m.CreatedAt)
            .Take(20)
            .ToListAsync();

        foreach (var message in messages)
        {
            try
            {
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
            }
            catch (Exception)
            {
                message.Status = OutboxStatus.Failed;
            }
        }

        await dbContext.SaveChangesAsync();
    }
}
