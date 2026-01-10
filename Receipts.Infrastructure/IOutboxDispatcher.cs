namespace Receipts.Infrastructure;

public interface IOutboxDispatcher
{
    Task DispatchAsync();
}
