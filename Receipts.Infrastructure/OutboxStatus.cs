namespace Receipts.Infrastructure;

public enum OutboxStatus
{
    New,
    Processing,
    Completed,
    Failed
}
