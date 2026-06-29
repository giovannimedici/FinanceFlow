using Confluent.Kafka;
using Confluent.Kafka.Admin;
using FinanceFlow.Application.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FinanceFlow.Infrastructure.Messaging;

public sealed class KafkaTopicInitializer : IHostedService
{
    private const int _numPartitions = 3;
    private const short _replicationFactor = 1;

    private readonly string _bootstrapServers;
    private readonly ILogger<KafkaTopicInitializer> _logger;

    public KafkaTopicInitializer(
        IConfiguration configuration,
        ILogger<KafkaTopicInitializer> logger)
    {
        _logger = logger;
        _bootstrapServers = configuration["Kafka:BootstrapServers"]
            ?? throw new InvalidOperationException(
                "Kafka bootstrap servers not configured. Set Kafka:BootstrapServers.");
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var adminConfig = new AdminClientConfig { BootstrapServers = _bootstrapServers };

        using var adminClient = new AdminClientBuilder(adminConfig).Build();

        try
        {
            await adminClient.CreateTopicsAsync(
            [
                new TopicSpecification
                {
                    Name              = KafkaTopics.TransactionsCreated,
                    NumPartitions     = _numPartitions,
                    ReplicationFactor = _replicationFactor,
                }
            ]);

            _logger.LogInformation(
                "Kafka topic '{Topic}' created ({Partitions} partitions, replication factor {ReplicationFactor}).",
                KafkaTopics.TransactionsCreated,
                _numPartitions,
                _replicationFactor);
        }
        catch (CreateTopicsException ex)
            when (ex.Results.All(r => r.Error.Code == ErrorCode.TopicAlreadyExists))
        {
            _logger.LogInformation(
                "Kafka topic '{Topic}' already exists — skipping creation.",
                KafkaTopics.TransactionsCreated);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to create Kafka topic '{Topic}'. The API will continue starting up.",
                KafkaTopics.TransactionsCreated);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
