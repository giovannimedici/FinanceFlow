using Confluent.Kafka;
using FinanceFlow.API.Extensions;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;
using Serilog.Context;
using Serilog.Events;
using Serilog.Formatting.Json;
using FinanceFlow.Infrastructure;
using FinanceFlow.Application;
using Microsoft.OpenApi.Models;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(new JsonFormatter(renderMessage: true))
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, _, configuration) =>
    {
        var defaultLevel = ParseLogLevel(context.Configuration["Logging:LogLevel:Default"]);
        var aspNetCoreLevel = ParseLogLevel(
            context.Configuration["Logging:LogLevel:Microsoft.AspNetCore"],
            LogEventLevel.Warning);

        configuration
            .MinimumLevel.Is(defaultLevel)
            .MinimumLevel.Override("Microsoft.AspNetCore", aspNetCoreLevel)
            .Enrich.FromLogContext()
            .WriteTo.Console(new JsonFormatter(renderMessage: true));
    });

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "FinanceFlow API",
            Version = "v1",
            Description = "Financial transaction processing API — event-driven with Kafka"
        });
    });
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddApplication();
    builder.Services.AddHealthChecks()
        .AddNpgSql(builder.Configuration.GetConnectionString("FinanceFlow")!)
        .AddKafka(new ProducerConfig
        {
            BootstrapServers = builder.Configuration["Kafka:BootstrapServers"]
        });


    var app = builder.Build();

    await app.ApplyMigrationsAsync();

    app.Use(async (context, next) =>
    {
        using (LogContext.PushProperty("requestId", context.TraceIdentifier))
        {
            await next(context);
        }
    });

    app.UseSerilogRequestLogging(options =>
    {
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("requestId", httpContext.TraceIdentifier);
        };
    });

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();

    app.UseMiddleware<ExceptionHandlingMiddleware>();

    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    });

    app.MapAccountEndpoints();
    app.MapTransactionEndpoints();

    Log.Information("FinanceFlow API starting");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

static LogEventLevel ParseLogLevel(string? level, LogEventLevel fallback = LogEventLevel.Information) =>
    Enum.TryParse<LogEventLevel>(level, ignoreCase: true, out var parsed) ? parsed : fallback;
