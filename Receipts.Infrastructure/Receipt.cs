namespace Receipts.Infrastructure;

public class Receipt
{
    public Guid Id { get; set; }
    public ReceiptStatus Status { get; set; } = ReceiptStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? DocumentUrl { get; set; }
    public Guid UserId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public DateOnly ReceiptDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
}
