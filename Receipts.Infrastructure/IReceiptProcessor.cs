namespace Receipts.Infrastructure;

public interface IReceiptProcessor
{
    Task ProcessReceipt(Guid receiptId);
}
