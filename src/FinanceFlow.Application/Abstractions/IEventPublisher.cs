namespace FinanceFlow.Application.Abstractions;

public interface IEventPublisher
{
    Task PublishAsync<T>(
        Guid TransactionId,
        string topic,
        string key,
        T payload,
        CancellationToken ct = default);
}