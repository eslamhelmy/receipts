using System;
using Receipts.Infrastructure;
using Xunit;

namespace Receipts.Infrastructure.Tests
{
    public class OutboxMessageTests
    {
        [Fact]
        public void OutboxMessage_ShouldHaveCorrectProperties()
        {
            // Arrange
            var occurredOn = DateTime.UtcNow;
            var type = "ReceiptCreated";
            var payload = "{\"Id\": \"123\"}";

            // Act
            var message = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                OccurredOn = occurredOn,
                Type = type,
                Payload = payload,
                ProcessedDate = null
            };

            // Assert
            Assert.NotEqual(Guid.Empty, message.Id);
            Assert.Equal(occurredOn, message.OccurredOn);
            Assert.Equal(type, message.Type);
            Assert.Equal(payload, message.Payload);
            Assert.Null(message.ProcessedDate);
        }
    }
}
