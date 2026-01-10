using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Receipts.Infrastructure;
using Xunit;

namespace Receipts.Infrastructure.Tests;

public class ReceiptsDbContextTests
{
    [Fact]
    public async Task Can_Persist_OutboxMessage()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ReceiptsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ReceiptsDbContext(options);
        var message = new OutboxMessage
        {
            Type = "TestType",
            Payload = "TestPayload",
            Status = OutboxStatus.New
        };

        // Act
        // We expect this to fail initially because DbSet is not added yet
        // Accessing the DbSet property dynamically or expecting it to be there
        
        // This line will fail to compile if I use context.OutboxMessages directly if it doesn't exist.
        // But since I want to test TDD style, I should write code that *expects* it to exist.
        // However, if it doesn't compile, the tool "run_shell_command" will return build error.
        // That is acceptable for "Red" phase.
        
        context.Set<OutboxMessage>().Add(message);
        await context.SaveChangesAsync();

        // Assert
        using var context2 = new ReceiptsDbContext(options);
        var savedMessage = await context2.Set<OutboxMessage>().FirstOrDefaultAsync();
        
        Assert.NotNull(savedMessage);
        Assert.Equal(message.Id, savedMessage.Id);
        Assert.Equal("TestType", savedMessage.Type);
    }
}
