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

        // Register the receipt processor
        services.AddScoped<IReceiptProcessor, ReceiptProcessor>();
    });

var host = builder.Build();

host.Run();
