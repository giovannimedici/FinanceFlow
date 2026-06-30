using System.Text.Json;
using Confluent.Kafka;
using FinanceFlow.Application.Events;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Infrastructure.Audit;
using FinanceFlow.Workers.Audit.Consumers;
using FinanceFlow.Tests.Workers.Audit.Integration;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FinanceFlow.Tests.Workers.Audit.Consumers;

public class AuditConsumerWorkerTests
{
    [Fact]
    public void Constructor_SucceedsWhenBootstrapServersConfigured()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Kafka:BootstrapServers"] = "localhost:9092"
            })
            .Build();

        var services = new ServiceCollection().BuildServiceProvider();
        using var worker = new AuditConsumerWorker(
            services,
            NullLogger<AuditConsumerWorker>.Instance,
            configuration);

        worker.Should().NotBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_WhenStopped_ClosesConsumer()
    {
        var cts = new CancellationTokenSource();
        var consumerMock = new Mock<IConsumer<string, string>>();
        consumerMock
            .Setup(c => c.Consume(It.IsAny<CancellationToken>()))
            .Callback(() => cts.Cancel())
            .Throws(new OperationCanceledException());

        var services = new ServiceCollection().BuildServiceProvider();
        var worker = new AuditConsumerWorker(
            services,
            NullLogger<AuditConsumerWorker>.Instance,
            consumerMock.Object);

        await worker.StartAsync(CancellationToken.None);
        await worker.StopAsync(CancellationToken.None);

        consumerMock.Verify(c => c.Subscribe("finance.transactions.created"), Times.Once);
        consumerMock.Verify(c => c.Close(), Times.Once);
    }
}

[Collection(nameof(AuditPostgreSqlCollection))]
public sealed class AuditConsumerWorkerIntegrationTests : IAsyncLifetime
{
    private readonly AuditPostgreSqlFixture _fixture;
    private IServiceProvider _serviceProvider = null!;
    private AuditDbContext _dbContext = null!;

    public AuditConsumerWorkerIntegrationTests(AuditPostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync()
    {
        _dbContext = _fixture.CreateDbContext();

        var services = new ServiceCollection();
        services.AddDbContext<AuditDbContext>(opt =>
            opt.UseNpgsql(_fixture.ConnectionString).UseSnakeCaseNamingConvention());
        _serviceProvider = services.BuildServiceProvider();

        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE audit_logs");
        await _dbContext.DisposeAsync();

        if (_serviceProvider is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }
        else if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    [Fact]
    public async Task ProcessMessageAsync_WhenEventIsValid_PersistsAuditLog()
    {
        var eventId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var @event = new TransactionCreatedEvent(
            EventId: eventId,
            OccurredAt: DateTimeOffset.UtcNow,
            TransactionId: Guid.NewGuid(),
            AccountId: Guid.NewGuid(),
            Type: "Deposit",
            Amount: 100m,
            BalanceAfter: 500m,
            RelatedAccountId: null);

        var payload = JsonSerializer.Serialize(@event);
        var result = CreateConsumeResult("finance.transactions.created", 0, 42, payload);

        var consumerMock = new Mock<IConsumer<string, string>>();
        var worker = new AuditConsumerWorker(
            _serviceProvider,
            NullLogger<AuditConsumerWorker>.Instance,
            consumerMock.Object);

        await worker.ProcessMessageAsync(result, CancellationToken.None);

        var persisted = await _dbContext.AuditLogs
            .AsNoTracking()
            .SingleAsync(l => l.EventId == eventId);

        persisted.Topic.Should().Be("finance.transactions.created");
        persisted.Partition.Should().Be(0);
        persisted.Offset.Should().Be(42);
        persisted.ReceivedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));

        var deserialized = JsonSerializer.Deserialize<TransactionCreatedEvent>(persisted.Payload);
        deserialized.Should().NotBeNull();
        deserialized!.EventId.Should().Be(eventId);
        deserialized.Type.Should().Be("Deposit");
        deserialized.Amount.Should().Be(100m);
        deserialized.BalanceAfter.Should().Be(500m);
    }

    [Fact]
    public async Task ProcessMessageAsync_WhenDuplicateTopicPartitionOffset_DoesNotThrow()
    {
        var eventId = Guid.NewGuid();
        var payload = JsonSerializer.Serialize(CreateEvent(eventId));
        var result = CreateConsumeResult("finance.transactions.created", 1, 99, payload);

        var consumerMock = new Mock<IConsumer<string, string>>();
        var worker = new AuditConsumerWorker(
            _serviceProvider,
            NullLogger<AuditConsumerWorker>.Instance,
            consumerMock.Object);

        await worker.ProcessMessageAsync(result, CancellationToken.None);

        var act = () => worker.ProcessMessageAsync(result, CancellationToken.None);

        await act.Should().NotThrowAsync();

        (await _dbContext.AuditLogs.CountAsync()).Should().Be(1);
    }

    private static TransactionCreatedEvent CreateEvent(Guid eventId) =>
        new(
            EventId: eventId,
            OccurredAt: DateTimeOffset.UtcNow,
            TransactionId: Guid.NewGuid(),
            AccountId: Guid.NewGuid(),
            Type: "Withdrawal",
            Amount: 50m,
            BalanceAfter: 450m,
            RelatedAccountId: null);

    private static ConsumeResult<string, string> CreateConsumeResult(
        string topic, int partition, long offset, string payload) =>
        new()
        {
            Topic = topic,
            Partition = new Partition(partition),
            Offset = new Offset(offset),
            Message = new Message<string, string> { Value = payload }
        };
}
