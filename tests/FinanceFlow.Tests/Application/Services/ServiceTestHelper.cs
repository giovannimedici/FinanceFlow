using FinanceFlow.Application.Abstractions;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;

namespace FinanceFlow.Tests.Application.Services;

internal static class ServiceTestHelper
{
    public static Mock<IUnitOfWork> CreateUnitOfWorkMock(out Mock<IDbContextTransaction> dbTransactionMock)
    {
        dbTransactionMock = new Mock<IDbContextTransaction>();
        dbTransactionMock
            .Setup(t => t.CommitAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        dbTransactionMock
            .Setup(t => t.RollbackAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        dbTransactionMock
            .Setup(t => t.DisposeAsync())
            .Returns(ValueTask.CompletedTask);

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock
            .Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(dbTransactionMock.Object);
        unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return unitOfWorkMock;
    }
}
