using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Receipts.Infrastructure;
using Receipts.Worker.Processors;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        var receiptsConnectionString = context.Configuration.GetConnectionString("ReceiptsDatabase");
        var hangfireConnectionString = context.Configuration.GetConnectionString("HangfireDatabase");

        services.AddDbContext<ReceiptsDbContext>(options =>
        
            options.UseSqlServer(receiptsConnectionString));

        services.AddHangfire(config =>
        {
            config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                  .UseSimpleAssemblyNameTypeSerializer()
                  .UseRecommendedSerializerSettings()
                  .UseSqlServerStorage(hangfireConnectionString, new Hangfire.SqlServer.SqlServerStorageOptions
                  {
                      DisableGlobalLocks = true
                  });
        });
        services.AddHangfireServer();

        // Register the processors
        services.AddScoped<IReceiptProcessor, ReceiptProcessor>();
        services.AddScoped<IOutboxProcessor, OutboxProcessor>();
    });

var host = builder.Build();

// Configure Hangfire recurring jobs
using (var scope = host.Services.CreateScope())
{
    var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
    recurringJobManager.AddOrUpdate<IOutboxProcessor>(
        "outbox-processor",
        processor => processor.ProcessMessagesAsync(),
        Cron.Minutely);
}

host.Run();
