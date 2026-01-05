using System;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Receipts.Infrastructure.Tests
{
    public class ReceiptsDbContextTests
    {
        [Fact]
        public void ModelBuilder_ShouldConfigureOutboxMessage()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ReceiptsDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Use InMemory for model check
                .Options;

            using var context = new ReceiptsDbContext(options);

            // Act & Assert
            // This will throw if the DbContext doesn't have the DbSet or configuration is invalid
            var entityType = context.Model.FindEntityType(typeof(OutboxMessage));
            Assert.NotNull(entityType);
            
            // Check for index on ProcessedDate (Note: InMemory doesn't fully support relational indexes, 
            // but we can check the model metadata)
            var index = entityType.FindIndex(entityType.FindProperty(nameof(OutboxMessage.ProcessedDate))!);
            Assert.NotNull(index);
        }
    }
}
