using FinanceFlow.Infrastructure.Messaging;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace FinanceFlow.Tests.Infrastructure.Messaging;

public class KafkaEventPublisherTests
{
    [Fact]
    public void Constructor_ThrowsWhenBootstrapServersMissing()
    {
        var configuration = new ConfigurationBuilder().Build();

        var act = () => new KafkaEventPublisher(
            configuration,
            NullLogger<KafkaEventPublisher>.Instance);

        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("*Kafka:BootstrapServers*");
    }

    [Fact]
    public void Constructor_SucceedsWhenBootstrapServersConfigured()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Kafka:BootstrapServers"] = "localhost:9092"
            })
            .Build();

        using var publisher = new KafkaEventPublisher(
            configuration,
            NullLogger<KafkaEventPublisher>.Instance);

        publisher.Should().NotBeNull();
    }
}
