using System.Text.Json;
using Confluent.Kafka;
using FinanceFlow.Application.Events;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Infrastructure.Audit;
using Microsoft.EntityFrameworkCore;

namespace FinanceFlow.Workers.Audit.Consumers;

public class AuditConsumerWorker : BackgroundService
{
    private readonly IConsumer<string, string> _consumer;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AuditConsumerWorker> _logger;

    public AuditConsumerWorker(IServiceProvider sp, ILogger<AuditConsumerWorker> logger, IConfiguration config)
        : this(sp, logger, CreateConsumer(config))
    {
    }

    internal AuditConsumerWorker(
        IServiceProvider sp,
        ILogger<AuditConsumerWorker> logger,
        IConsumer<string, string> consumer)
    {
        _serviceProvider = sp;
        _logger = logger;
        _consumer = consumer;
    }

    private static IConsumer<string, string> CreateConsumer(IConfiguration config)
    {
        var conf = new ConsumerConfig
        {
            BootstrapServers = config["Kafka:BootstrapServers"],
            GroupId = "audit-consumer-group",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };
        return new ConsumerBuilder<string, string>(conf).Build();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _consumer.Subscribe("finance.transactions.created");

        try
        {
            await Task.Run(async () =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var result = _consumer.Consume(stoppingToken);
                        _logger.LogInformation("Consumed message: {Topic}/{Partition}/{Offset}",
                            result.Topic, result.Partition.Value, result.Offset.Value);
                        await ProcessMessageAsync(result, stoppingToken);
                        _consumer.Commit(result);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error consuming audit message: offset will not be committed");
                    }
                }
            }, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Expected when the host shuts down while the consume loop is running.
        }
        finally
        {
            _consumer.Close();
        }
    }

    internal async Task ProcessMessageAsync(ConsumeResult<string, string> result, CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuditDbContext>();

        var evt = JsonSerializer.Deserialize<TransactionCreatedEvent>(result.Message.Value)!;
        
        _logger.LogInformation("Processing message: {EventId}", evt.EventId);

        var log = AuditLog.Create(
            evt.EventId, result.Topic, result.Partition.Value, result.Offset.Value, result.Message.Value);

        db.AuditLogs.Add(log);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            _logger.LogWarning("Duplicate message ignored: {Topic}/{Partition}/{Offset}",
                result.Topic, result.Partition.Value, result.Offset.Value);
        }
    }
}