using Microsoft.EntityFrameworkCore;

namespace Receipts.Infrastructure;

public class ReceiptsDbContext : DbContext
{
    public ReceiptsDbContext(DbContextOptions<ReceiptsDbContext> options) : base(options)
    {
    }

    public DbSet<Receipt> Receipts { get; set; } = null!;
    public DbSet<OutboxMessage> OutboxMessages { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Receipt>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Status).HasMaxLength(50);
            entity.Property(r => r.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(r => r.DocumentUrl).HasMaxLength(1024);
            entity.Property(r => r.Currency).HasMaxLength(3);
            entity.Property(r => r.Amount).HasColumnType("decimal(18,2)");
            entity.Property(r => r.ReceiptDate);
            entity.Property(r => r.Status).HasConversion<int>();
        });

        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ProcessedDate);
        });
    }
}
