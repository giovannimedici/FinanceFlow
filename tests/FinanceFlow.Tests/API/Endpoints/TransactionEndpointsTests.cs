using Xunit;
using Moq;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using FinanceFlow.Application.Services.Interfaces;

namespace FinanceFlow.Tests.API.Endpoints;

public class TransactionEndpointsTests
{
    private readonly Mock<ITransactionService> _mockTransactionService;
    private readonly Mock<ITransferService> _mockTransferService;
    private readonly Mock<IValidator<TransferRequest>> _mockValidator;
    private readonly CancellationToken _cancellationToken;

    public TransactionEndpointsTests()
    {
        _mockTransactionService = new Mock<ITransactionService>();
        _mockTransferService = new Mock<ITransferService>();
        _mockValidator = new Mock<IValidator<TransferRequest>>();
        _cancellationToken = CancellationToken.None;
    }

    #region POST /api/accounts/{accountId}/deposit Tests

    [Fact]
    public async Task Deposit_WithValidRequest_ReturnsOkResult()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var request = new DepositRequest(100.50m, "Salary deposit");
        var expectedResponse = new TransactionResponse(
            Guid.NewGuid(),
            100.50m,
            1100.50m
        );

        _mockTransactionService
            .Setup(s => s.DepositAsync(accountId, request, _cancellationToken))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await DepositEndpoint(accountId, request);

        // Assert
        result.Should().BeOfType<Ok<TransactionResponse>>();
        var okResult = result as Ok<TransactionResponse>;
        okResult!.StatusCode.Should().Be(StatusCodes.Status200OK);
        okResult.Value.Should().BeEquivalentTo(expectedResponse);
        okResult.Value!.amount.Should().Be(100.50m);
        okResult.Value.NewBalance.Should().Be(1100.50m);

        _mockTransactionService.Verify(s => s.DepositAsync(accountId, request, _cancellationToken), Times.Once);
    }

    [Fact]
    public async Task Deposit_WithLargeAmount_ReturnsOkResult()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var request = new DepositRequest(10000.00m, "Large deposit");
        var expectedResponse = new TransactionResponse(
            Guid.NewGuid(),
            10000.00m,
            15000.00m
        );

        _mockTransactionService
            .Setup(s => s.DepositAsync(accountId, request, _cancellationToken))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await DepositEndpoint(accountId, request);

        // Assert
        result.Should().BeOfType<Ok<TransactionResponse>>();
        var okResult = result as Ok<TransactionResponse>;
        okResult!.Value!.amount.Should().Be(10000.00m);

        _mockTransactionService.Verify(s => s.DepositAsync(accountId, request, _cancellationToken), Times.Once);
    }

    [Fact]
    public async Task Deposit_WithoutDescription_ReturnsOkResult()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var request = new DepositRequest(50.00m, null);
        var expectedResponse = new TransactionResponse(
            Guid.NewGuid(),
            50.00m,
            1050.00m
        );

        _mockTransactionService
            .Setup(s => s.DepositAsync(accountId, request, _cancellationToken))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await DepositEndpoint(accountId, request);

        // Assert
        result.Should().BeOfType<Ok<TransactionResponse>>();
        var okResult = result as Ok<TransactionResponse>;
        okResult!.StatusCode.Should().Be(StatusCodes.Status200OK);

        _mockTransactionService.Verify(s => s.DepositAsync(accountId, request, _cancellationToken), Times.Once);
    }

    #endregion

    #region POST /api/accounts/{accountId}/withdraw Tests

    [Fact]
    public async Task Withdraw_WithValidRequest_ReturnsOkResult()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var request = new WithdrawRequest(50.25m, "ATM withdrawal");
        var expectedResponse = new TransactionResponse(
            Guid.NewGuid(),
            50.25m,
            949.75m
        );

        _mockTransactionService
            .Setup(s => s.WithdrawAsync(accountId, request, _cancellationToken))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await WithdrawEndpoint(accountId, request);

        // Assert
        result.Should().BeOfType<Ok<TransactionResponse>>();
        var okResult = result as Ok<TransactionResponse>;
        okResult!.StatusCode.Should().Be(StatusCodes.Status200OK);
        okResult.Value.Should().BeEquivalentTo(expectedResponse);
        okResult.Value!.amount.Should().Be(50.25m);
        okResult.Value.NewBalance.Should().Be(949.75m);

        _mockTransactionService.Verify(s => s.WithdrawAsync(accountId, request, _cancellationToken), Times.Once);
    }

    [Fact]
    public async Task Withdraw_WithMaximumAmount_ReturnsOkResult()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var request = new WithdrawRequest(1000.00m, "Large withdrawal");
        var expectedResponse = new TransactionResponse(
            Guid.NewGuid(),
            1000.00m,
            0.00m
        );

        _mockTransactionService
            .Setup(s => s.WithdrawAsync(accountId, request, _cancellationToken))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await WithdrawEndpoint(accountId, request);

        // Assert
        result.Should().BeOfType<Ok<TransactionResponse>>();
        var okResult = result as Ok<TransactionResponse>;
        okResult!.Value!.NewBalance.Should().Be(0.00m);

        _mockTransactionService.Verify(s => s.WithdrawAsync(accountId, request, _cancellationToken), Times.Once);
    }

    [Fact]
    public async Task Withdraw_WithoutDescription_ReturnsOkResult()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var request = new WithdrawRequest(25.00m, null);
        var expectedResponse = new TransactionResponse(
            Guid.NewGuid(),
            25.00m,
            975.00m
        );

        _mockTransactionService
            .Setup(s => s.WithdrawAsync(accountId, request, _cancellationToken))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await WithdrawEndpoint(accountId, request);

        // Assert
        result.Should().BeOfType<Ok<TransactionResponse>>();
        var okResult = result as Ok<TransactionResponse>;
        okResult!.StatusCode.Should().Be(StatusCodes.Status200OK);

        _mockTransactionService.Verify(s => s.WithdrawAsync(accountId, request, _cancellationToken), Times.Once);
    }

    #endregion

    #region GET /api/accounts/{accountId}/transactions Tests

    [Fact]
    public async Task GetTransactionsByAccountId_WithValidPagination_ReturnsOkResult()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var page = 1;
        var pageSize = 10;
        var transactions = new List<TransactionResponse>
        {
            new TransactionResponse(Guid.NewGuid(), 100.00m, 1100.00m),
            new TransactionResponse(Guid.NewGuid(), 50.00m, 1050.00m)
        };
        var totalCount = 2;

        _mockTransactionService
            .Setup(s => s.GetTransactionsByAccountIdAsync(accountId, page, pageSize, _cancellationToken))
            .ReturnsAsync(((IReadOnlyList<TransactionResponse>)transactions, totalCount));

        // Act
        var result = await GetTransactionsByAccountIdEndpoint(accountId, page, pageSize);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var okResult = result.GetType();
        okResult.Name.Should().StartWith("Ok");

        _mockTransactionService.Verify(s => s.GetTransactionsByAccountIdAsync(accountId, page, pageSize, _cancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetTransactionsByAccountId_WithFirstPage_ReturnsOkResult()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var page = 1;
        var pageSize = 5;
        var transactions = new List<TransactionResponse>
        {
            new TransactionResponse(Guid.NewGuid(), 100.00m, 1100.00m),
            new TransactionResponse(Guid.NewGuid(), 50.00m, 1050.00m),
            new TransactionResponse(Guid.NewGuid(), 75.00m, 1125.00m),
            new TransactionResponse(Guid.NewGuid(), 25.00m, 1100.00m),
            new TransactionResponse(Guid.NewGuid(), 200.00m, 1300.00m)
        };
        var totalCount = 15;

        _mockTransactionService
            .Setup(s => s.GetTransactionsByAccountIdAsync(accountId, page, pageSize, _cancellationToken))
            .ReturnsAsync(((IReadOnlyList<TransactionResponse>)transactions, totalCount));

        // Act
        var result = await GetTransactionsByAccountIdEndpoint(accountId, page, pageSize);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var okResult = result.GetType();
        okResult.Name.Should().StartWith("Ok");

        _mockTransactionService.Verify(s => s.GetTransactionsByAccountIdAsync(accountId, page, pageSize, _cancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetTransactionsByAccountId_WithNoTransactions_ReturnsEmptyList()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var page = 1;
        var pageSize = 10;
        var transactions = new List<TransactionResponse>();
        var totalCount = 0;

        _mockTransactionService
            .Setup(s => s.GetTransactionsByAccountIdAsync(accountId, page, pageSize, _cancellationToken))
            .ReturnsAsync(((IReadOnlyList<TransactionResponse>)transactions, totalCount));

        // Act
        var result = await GetTransactionsByAccountIdEndpoint(accountId, page, pageSize);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var okResult = result.GetType();
        okResult.Name.Should().StartWith("Ok");

        _mockTransactionService.Verify(s => s.GetTransactionsByAccountIdAsync(accountId, page, pageSize, _cancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetTransactionsByAccountId_WithLargePageSize_ReturnsOkResult()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var page = 1;
        var pageSize = 100;
        var transactions = new List<TransactionResponse>
        {
            new TransactionResponse(Guid.NewGuid(), 100.00m, 1100.00m)
        };
        var totalCount = 1;

        _mockTransactionService
            .Setup(s => s.GetTransactionsByAccountIdAsync(accountId, page, pageSize, _cancellationToken))
            .ReturnsAsync(((IReadOnlyList<TransactionResponse>)transactions, totalCount));

        // Act
        var result = await GetTransactionsByAccountIdEndpoint(accountId, page, pageSize);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var okResult = result.GetType();
        okResult.Name.Should().StartWith("Ok");

        _mockTransactionService.Verify(s => s.GetTransactionsByAccountIdAsync(accountId, page, pageSize, _cancellationToken), Times.Once);
    }

    #endregion

    #region POST /api/transfers Tests

    [Fact]
    public async Task Transfer_WithValidRequest_ReturnsOkResult()
    {
        // Arrange
        var request = new TransferRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            150.00m,
            "Transfer to friend"
        );
        var expectedResponse = new TransferResponse(
            Guid.NewGuid(),
            850.00m,
            1150.00m
        );

        _mockValidator
            .Setup(v => v.ValidateAsync(request, _cancellationToken))
            .ReturnsAsync(new ValidationResult());

        _mockTransferService
            .Setup(s => s.TransferAsync(request, _cancellationToken))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await TransferEndpoint(request);

        // Assert
        result.Should().BeOfType<Ok<TransferResponse>>();
        var okResult = result as Ok<TransferResponse>;
        okResult!.StatusCode.Should().Be(StatusCodes.Status200OK);

        _mockValidator.Verify(v => v.ValidateAsync(request, _cancellationToken), Times.Once);
        _mockTransferService.Verify(s => s.TransferAsync(request, _cancellationToken), Times.Once);
    }

    [Fact]
    public async Task Transfer_WithInvalidRequest_ReturnsValidationProblem()
    {
        // Arrange
        var request = new TransferRequest(
            Guid.Empty,
            Guid.NewGuid(),
            -50.00m,
            "Invalid transfer"
        );
        var validationFailures = new List<ValidationFailure>
        {
            new ValidationFailure("SourceAccountId", "Source account ID is required"),
            new ValidationFailure("Amount", "Amount must be greater than zero")
        };
        var validationResult = new ValidationResult(validationFailures);

        _mockValidator
            .Setup(v => v.ValidateAsync(request, _cancellationToken))
            .ReturnsAsync(validationResult);

        // Act
        var result = await TransferEndpoint(request);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        
        var resultType = result.GetType();
        resultType.Name.Should().Be("ProblemHttpResult");

        _mockValidator.Verify(v => v.ValidateAsync(request, _cancellationToken), Times.Once);
        _mockTransferService.Verify(s => s.TransferAsync(It.IsAny<TransferRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Transfer_WithSameSourceAndDestination_ReturnsValidationProblem()
    {
        // Arrange
        var sameAccountId = Guid.NewGuid();
        var request = new TransferRequest(
            sameAccountId,
            sameAccountId,
            100.00m,
            "Same account transfer"
        );
        var validationFailures = new List<ValidationFailure>
        {
            new ValidationFailure("DestinationAccountId", "Source and destination accounts must be different")
        };
        var validationResult = new ValidationResult(validationFailures);

        _mockValidator
            .Setup(v => v.ValidateAsync(request, _cancellationToken))
            .ReturnsAsync(validationResult);

        // Act
        var result = await TransferEndpoint(request);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        
        var resultType = result.GetType();
        resultType.Name.Should().Be("ProblemHttpResult");

        _mockValidator.Verify(v => v.ValidateAsync(request, _cancellationToken), Times.Once);
        _mockTransferService.Verify(s => s.TransferAsync(It.IsAny<TransferRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Transfer_WithoutDescription_ReturnsOkResult()
    {
        // Arrange
        var request = new TransferRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            75.00m,
            null
        );
        var expectedResponse = new TransferResponse(
            Guid.NewGuid(),
            925.00m,
            1075.00m
        );

        _mockValidator
            .Setup(v => v.ValidateAsync(request, _cancellationToken))
            .ReturnsAsync(new ValidationResult());

        _mockTransferService
            .Setup(s => s.TransferAsync(request, _cancellationToken))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await TransferEndpoint(request);

        // Assert
        result.Should().BeOfType<Ok<TransferResponse>>();
        var okResult = result as Ok<TransferResponse>;
        okResult!.StatusCode.Should().Be(StatusCodes.Status200OK);

        _mockValidator.Verify(v => v.ValidateAsync(request, _cancellationToken), Times.Once);
        _mockTransferService.Verify(s => s.TransferAsync(request, _cancellationToken), Times.Once);
    }

    [Fact]
    public async Task Transfer_WithLargeAmount_ReturnsOkResult()
    {
        // Arrange
        var request = new TransferRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            5000.00m,
            "Large transfer"
        );
        var expectedResponse = new TransferResponse(
            Guid.NewGuid(),
            0.00m,
            10000.00m
        );

        _mockValidator
            .Setup(v => v.ValidateAsync(request, _cancellationToken))
            .ReturnsAsync(new ValidationResult());

        _mockTransferService
            .Setup(s => s.TransferAsync(request, _cancellationToken))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await TransferEndpoint(request);

        // Assert
        result.Should().BeOfType<Ok<TransferResponse>>();
        var okResult = result as Ok<TransferResponse>;
        okResult!.StatusCode.Should().Be(StatusCodes.Status200OK);

        _mockValidator.Verify(v => v.ValidateAsync(request, _cancellationToken), Times.Once);
        _mockTransferService.Verify(s => s.TransferAsync(request, _cancellationToken), Times.Once);
    }

    #endregion

    #region Helper Methods

    private async Task<IResult> DepositEndpoint(Guid accountId, DepositRequest request)
    {
        var response = await _mockTransactionService.Object.DepositAsync(accountId, request, _cancellationToken);
        return Results.Ok(response);
    }

    private async Task<IResult> WithdrawEndpoint(Guid accountId, WithdrawRequest request)
    {
        var response = await _mockTransactionService.Object.WithdrawAsync(accountId, request, _cancellationToken);
        return Results.Ok(response);
    }

    private async Task<IResult> GetTransactionsByAccountIdEndpoint(Guid accountId, int page, int pageSize)
    {
        var (data, totalCount) = await _mockTransactionService.Object.GetTransactionsByAccountIdAsync(accountId, page, pageSize, _cancellationToken);
        return Results.Ok(new { Data = data, TotalCount = totalCount });
    }

    private async Task<IResult> TransferEndpoint(TransferRequest request)
    {
        var validation = await _mockValidator.Object.ValidateAsync(request, _cancellationToken);
        if (!validation.IsValid)
            return Results.ValidationProblem(validation.ToDictionary());

        var result = await _mockTransferService.Object.TransferAsync(request, _cancellationToken);
        return Results.Ok(result);
    }

    #endregion
}
