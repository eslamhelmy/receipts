namespace Receipts.Infrastructure;

public interface IOutboxProcessor
{
    Task ProcessMessagesAsync();
}
