using System.Text.Json;
using Confluent.Kafka;
using FinanceFlow.Application.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FinanceFlow.Infrastructure.Messaging;
public sealed class KafkaEventPublisher : IEventPublisher, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaEventPublisher> _logger;

    public KafkaEventPublisher(
        IConfiguration configuration,
        ILogger<KafkaEventPublisher> logger)
    {
        _logger = logger;

        var config = new ProducerConfig
        {
            BootstrapServers                    = configuration["Kafka__BootstrapServers"],
            Acks                                = Acks.All,
            EnableIdempotence                   = true,
            MaxInFlight                         = 5,
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task PublishAsync<T>(
        string topic,
        string key,
        T payload,
        CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(payload);

        var message = new Message<string, string>
        {
            Key   = key,
            Value = json
        };

        var result = await _producer.ProduceAsync(topic, message, ct);

        _logger.LogInformation(
            "Event published. Topic={Topic} Partition={Partition} Offset={Offset}",
            result.Topic,
            result.Partition.Value,
            result.Offset.Value);
    }

    public void Dispose() => _producer.Dispose();
}