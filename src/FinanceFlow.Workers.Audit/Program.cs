using FinanceFlow.Infrastructure;
using FinanceFlow.Workers.Audit.Consumers;
using FinanceFlow.Workers.Audit.Extensions;
using Serilog;
using Serilog.Formatting.Json;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddAuditPersistence(builder.Configuration);

builder.Services.AddHostedService<AuditConsumerWorker>();

builder.Services.AddSerilog((services, config) => config
    .ReadFrom.Services(services)
    .WriteTo.Console(new JsonFormatter()));

var app = builder.Build();

await app.ApplyMigrationsAsync();

app.Run();
