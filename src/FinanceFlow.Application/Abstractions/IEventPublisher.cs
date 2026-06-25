namespace FinanceFlow.Application.Abstractions;

public interface IEventPublisher
{
    Task PublishAsync<T>(
        string topic,
        string key,
        T payload,
        CancellationToken ct = default);
}