using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Receipts.Infrastructure;
using Receipts.Worker.Processors;

var host = CreateHostBuilder(args).Build();

// Configure Hangfire Recurring Jobs
using (var scope = host.Services.CreateScope())
{
    var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
    recurringJobManager.AddOrUpdate<IOutboxDispatcher>(
        "outbox-dispatcher",
        dispatcher => dispatcher.DispatchAsync(),
        Cron.Minutely());
}

host.Run();

static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
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

            // Register the receipt processor
            services.AddScoped<IReceiptProcessor, ReceiptProcessor>();

            // Register Outbox Dispatcher
            services.AddScoped<IOutboxDispatcher, OutboxDispatcher>();
        });

public partial class Program { }