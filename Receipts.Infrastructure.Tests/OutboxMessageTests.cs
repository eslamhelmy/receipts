using System;
using System.Text.Json;
using Receipts.Infrastructure;
using Receipts.Infrastructure.Messages;
using Xunit;

namespace Receipts.Infrastructure.Tests;

public class OutboxMessageTests
{
    [Fact]
    public void Can_Create_OutboxMessage_With_Valid_Properties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var type = "Receipts.Infrastructure.Messages.ProcessReceiptMessage";
        var payload = "{\"ReceiptId\":\"00000000-0000-0000-0000-000000000000\"}";
        var createdAt = DateTime.UtcNow;

        // Act
        var message = new OutboxMessage
        {
            Id = id,
            Type = type,
            Payload = payload,
            Status = OutboxStatus.New,
            CreatedAt = createdAt
        };

        // Assert
        Assert.Equal(id, message.Id);
        Assert.Equal(type, message.Type);
        Assert.Equal(payload, message.Payload);
        Assert.Equal(OutboxStatus.New, message.Status);
        Assert.Equal(createdAt, message.CreatedAt);
        Assert.Null(message.ProcessedAt);
    }

    [Fact]
    public void Can_Serialize_And_Deserialize_Payload()
    {
        // Arrange
        var request = new ProcessReceiptMessage(Guid.NewGuid());
        var json = JsonSerializer.Serialize(request);

        // Act
        var message = new OutboxMessage
        {
            Type = typeof(ProcessReceiptMessage).FullName!,
            Payload = json
        };

        // Assert
        var deserialized = JsonSerializer.Deserialize<ProcessReceiptMessage>(message.Payload);
        Assert.NotNull(deserialized);
        Assert.Equal(request.ReceiptId, deserialized.ReceiptId);
    }
}
