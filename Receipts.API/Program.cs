using Microsoft.EntityFrameworkCore;
using Hangfire;
using Hangfire.Dashboard;
using Receipts.API.Services;
using Receipts.Infrastructure;

var builder = WebApplication.CreateBuilder(args);


var receiptsConnectionString = builder.Configuration.GetConnectionString("ReceiptsDatabase");
var hangfireConnectionString = builder.Configuration.GetConnectionString("HangfireDatabase");

builder.Services.AddDbContext<ReceiptsDbContext>(options =>
    options.UseSqlServer(receiptsConnectionString));

builder.Services.AddHangfire(config =>
{
    config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
          .UseSimpleAssemblyNameTypeSerializer()
          .UseRecommendedSerializerSettings()
          .UseSqlServerStorage(hangfireConnectionString, new Hangfire.SqlServer.SqlServerStorageOptions
          {
              DisableGlobalLocks = true
          });
});

builder.Services.AddControllers();
builder.Services.AddScoped<IReceiptFileService, ReceiptFileService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // Public Hangfire dashboard (development only)
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = new[] { new AllowAllDashboardAuthorizationFilter() }
    });
}

app.UseAuthorization();

app.MapControllers();

app.Run();

file sealed class AllowAllDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context) => true;
}
