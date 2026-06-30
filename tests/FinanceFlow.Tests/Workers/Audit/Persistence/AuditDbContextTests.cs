using System.Text.Json;
using FinanceFlow.Workers.Audit.Domain;
using FinanceFlow.Tests.Workers.Audit.Integration;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace FinanceFlow.Tests.Workers.Audit.Persistence;

[Collection(nameof(AuditPostgreSqlCollection))]
public sealed class AuditDbContextTests : IAsyncLifetime
{
    private readonly AuditPostgreSqlFixture _fixture;
    private AuditDbContext _dbContext = null!;

    public AuditDbContextTests(AuditPostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync()
    {
        _dbContext = _fixture.CreateDbContext();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE audit_logs");
        await _dbContext.DisposeAsync();
    }

    [Fact]
    public async Task Database_WhenMigrated_ExposesAuditLogsDbSet()
    {
        (await _dbContext.Database.CanConnectAsync()).Should().BeTrue();

        var log = AuditLog.Create(
            Guid.NewGuid(),
            "finance.transactions.created",
            partition: 0,
            offset: 1,
            payload: """{"EventId":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"}""");

        _dbContext.AuditLogs.Add(log);
        await _dbContext.SaveChangesAsync();

        (await _dbContext.AuditLogs.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task SaveChangesAsync_PersistsAuditLogWithJsonbPayload()
    {
        const string payload = """
            {
              "EventId": "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
              "Type": "Deposit",
              "Amount": 250.75
            }
            """;

        var log = AuditLog.Create(
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            "finance.transactions.created",
            partition: 3,
            offset: 500,
            payload);

        _dbContext.AuditLogs.Add(log);
        await _dbContext.SaveChangesAsync();

        var persisted = await _dbContext.AuditLogs
            .AsNoTracking()
            .SingleAsync(l => l.Id == log.Id);

        persisted.EventId.Should().Be(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
        persisted.Topic.Should().Be("finance.transactions.created");
        persisted.Partition.Should().Be(3);
        persisted.Offset.Should().Be(500);

        using var json = JsonDocument.Parse(persisted.Payload);
        json.RootElement.GetProperty("EventId").GetString()
            .Should().Be("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        json.RootElement.GetProperty("Type").GetString().Should().Be("Deposit");
        json.RootElement.GetProperty("Amount").GetDecimal().Should().Be(250.75m);

        var payloadColumnType = await _dbContext.Database
            .SqlQueryRaw<string>(
                """
                SELECT data_type AS "Value"
                FROM information_schema.columns
                WHERE table_name = 'audit_logs' AND column_name = 'payload'
                """)
            .SingleAsync();

        payloadColumnType.Should().Be("jsonb");
    }

    [Fact]
    public async Task SaveChangesAsync_WhenTopicPartitionOffsetDuplicate_ThrowsDbUpdateException()
    {
        const string topic = "finance.transactions.created";
        const int partition = 0;
        const long offset = 777;

        var first = AuditLog.Create(Guid.NewGuid(), topic, partition, offset, """{"first":true}""");
        var duplicate = AuditLog.Create(Guid.NewGuid(), topic, partition, offset, """{"second":true}""");

        _dbContext.AuditLogs.Add(first);
        await _dbContext.SaveChangesAsync();

        _dbContext.AuditLogs.Add(duplicate);

        var act = () => _dbContext.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task SaveChangesAsync_AllowsSameOffsetOnDifferentPartitions()
    {
        const string topic = "finance.transactions.created";
        const long offset = 100;

        var partitionZero = AuditLog.Create(Guid.NewGuid(), topic, partition: 0, offset, """{"partition":0}""");
        var partitionOne = AuditLog.Create(Guid.NewGuid(), topic, partition: 1, offset, """{"partition":1}""");

        _dbContext.AuditLogs.AddRange(partitionZero, partitionOne);

        var act = () => _dbContext.SaveChangesAsync();

        await act.Should().NotThrowAsync();
        (await _dbContext.AuditLogs.CountAsync()).Should().Be(2);
    }
}
