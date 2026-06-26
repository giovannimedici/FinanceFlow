namespace FinanceFlow.Application.Services.Interfaces;

public interface ITransferService
{
    Task<TransferResponse> TransferAsync(TransferRequest request, CancellationToken cancellationToken);
}