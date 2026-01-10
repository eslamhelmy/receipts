using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Receipts.Infrastructure;
using Xunit;
using System.Collections.Generic;
using Hangfire;
using Moq;

namespace Receipts.Worker.Tests;

public class WorkerRegistrationTests
{
    [Fact]
    public void Services_Should_Include_OutboxDispatcher()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:ReceiptsDatabase"] = "Server=localhost;Database=ReceiptsDb;",
                ["ConnectionStrings:HangfireDatabase"] = "Server=localhost;Database=HangfireDb;"
            })
            .Build();

        // Simulate the registration logic from Program.cs
        services.AddScoped<IOutboxDispatcher, OutboxDispatcher>();
        services.AddDbContext<ReceiptsDbContext>(options => options.UseInMemoryDatabase("test"));
        
        // Mock IBackgroundJobClient as it is required by OutboxDispatcher constructor
        services.AddScoped<IBackgroundJobClient>(sp => new Mock<IBackgroundJobClient>().Object);
        
        services.AddLogging();

        var serviceProvider = services.BuildServiceProvider();

        // Act
        var dispatcher = serviceProvider.GetService<IOutboxDispatcher>();

        // Assert
        Assert.NotNull(dispatcher);
        Assert.IsType<OutboxDispatcher>(dispatcher);
    }
}
