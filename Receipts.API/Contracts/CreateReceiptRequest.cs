using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Receipts.API.Contracts;

public class CreateReceiptRequest
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    [Range(typeof(decimal), "0.01", "1000000000000")]
    public decimal Amount { get; set; }

    [Required]
    [MaxLength(3)]
    public string Currency { get; set; } = "USD";

    [Required]
    public DateOnly ReceiptDate { get; set; }


    [Required]
    public IFormFile File { get; set; } = default!;
}
