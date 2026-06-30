using FinanceFlow.Workers.Audit.Consumers;
using FinanceFlow.Workers.Audit.Extensions;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Formatting.Json;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<AuditDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("FinanceFlow"))
       .UseSnakeCaseNamingConvention());

builder.Services.AddHostedService<AuditConsumerWorker>();

builder.Services.AddSerilog((services, config) => config
    .ReadFrom.Services(services)
    .WriteTo.Console(new JsonFormatter()));

var app = builder.Build();

await app.ApplyMigrationsAsync();

app.Run();
