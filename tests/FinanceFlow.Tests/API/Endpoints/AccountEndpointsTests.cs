using Xunit;
using Moq;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using FinanceFlow.Application.Services.Interfaces;

namespace FinanceFlow.Tests.API.Endpoints;

public class AccountEndpointsTests
{
    private readonly Mock<IAccountService> _mockAccountService;
    private readonly Mock<IValidator<CreateAccountRequest>> _mockValidator;
    private readonly CancellationToken _cancellationToken;

    public AccountEndpointsTests()
    {
        _mockAccountService = new Mock<IAccountService>();
        _mockValidator = new Mock<IValidator<CreateAccountRequest>>();
        _cancellationToken = CancellationToken.None;
    }

    #region POST /api/accounts Tests

    [Fact]
    public async Task CreateAccount_WithValidRequest_ReturnsCreatedResult()
    {
        // Arrange
        var request = new CreateAccountRequest("John Doe", "12345678900");
        var expectedResponse = new AccountResponse(
            Guid.NewGuid(),
            "John Doe",
            "12345678900",
            0m,
            "Active",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow
        );

        _mockValidator
            .Setup(v => v.ValidateAsync(request, _cancellationToken))
            .ReturnsAsync(new ValidationResult());

        _mockAccountService
            .Setup(s => s.CreateAsync(request, _cancellationToken))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await CreateAccountEndpoint(request);

        // Assert
        result.Should().BeOfType<Created<AccountResponse>>();
        var createdResult = result as Created<AccountResponse>;
        createdResult!.StatusCode.Should().Be(StatusCodes.Status201Created);
        createdResult.Location.Should().Be($"/api/accounts/{expectedResponse.Id}");
        createdResult.Value.Should().BeEquivalentTo(expectedResponse);

        _mockValidator.Verify(v => v.ValidateAsync(request, _cancellationToken), Times.Once);
        _mockAccountService.Verify(s => s.CreateAsync(request, _cancellationToken), Times.Once);
    }

    [Fact]
    public async Task CreateAccount_WithInvalidRequest_ReturnsValidationProblem()
    {
        // Arrange
        var request = new CreateAccountRequest("", "12345678900");
        var validationFailures = new List<ValidationFailure>
        {
            new ValidationFailure("OwnerName", "Owner name is required")
        };
        var validationResult = new ValidationResult(validationFailures);

        _mockValidator
            .Setup(v => v.ValidateAsync(request, _cancellationToken))
            .ReturnsAsync(validationResult);

        // Act
        var result = await CreateAccountEndpoint(request);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        
        var resultType = result.GetType();
        resultType.Name.Should().Be("ProblemHttpResult");

        _mockValidator.Verify(v => v.ValidateAsync(request, _cancellationToken), Times.Once);
        _mockAccountService.Verify(s => s.CreateAsync(It.IsAny<CreateAccountRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region GET /api/accounts/{id} Tests

    [Fact]
    public async Task GetAccountById_WithExistingId_ReturnsOkResult()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var expectedResponse = new AccountResponse(
            accountId,
            "John Doe",
            "12345678900",
            100.50m,
            "Active",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow
        );

        _mockAccountService
            .Setup(s => s.GetByIdAsync(accountId, _cancellationToken))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await GetAccountByIdEndpoint(accountId);

        // Assert
        result.Should().BeOfType<Ok<AccountResponse>>();
        var okResult = result as Ok<AccountResponse>;
        okResult!.StatusCode.Should().Be(StatusCodes.Status200OK);
        okResult.Value.Should().BeEquivalentTo(expectedResponse);

        _mockAccountService.Verify(s => s.GetByIdAsync(accountId, _cancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetAccountById_WithNonExistingId_ReturnsNotFound()
    {
        // Arrange
        var accountId = Guid.NewGuid();

        _mockAccountService
            .Setup(s => s.GetByIdAsync(accountId, _cancellationToken))
            .ReturnsAsync((AccountResponse?)null);

        // Act
        var result = await GetAccountByIdEndpoint(accountId);

        // Assert
        result.Should().BeOfType<NotFound<ProblemDetails>>();
        var notFoundResult = result as NotFound<ProblemDetails>;
        notFoundResult!.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        notFoundResult.Value!.Title.Should().Be("Account not found");
        notFoundResult.Value.Status.Should().Be(404);

        _mockAccountService.Verify(s => s.GetByIdAsync(accountId, _cancellationToken), Times.Once);
    }

    #endregion

    #region GET /api/accounts Tests

    [Fact]
    public async Task GetAllAccounts_WithoutFilter_ReturnsAllAccounts()
    {
        // Arrange
        var expectedAccounts = new List<AccountResponse>
        {
            new AccountResponse(
                Guid.NewGuid(),
                "John Doe",
                "12345678900",
                100.50m,
                "Active",
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow
            ),
            new AccountResponse(
                Guid.NewGuid(),
                "Jane Smith",
                "98765432100",
                250.75m,
                "Blocked",
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow
            )
        };

        _mockAccountService
            .Setup(s => s.GetAllAsync(null, _cancellationToken))
            .ReturnsAsync(expectedAccounts);

        // Act
        var result = await GetAllAccountsEndpoint(null);

        // Assert
        result.Should().BeOfType<Ok<IEnumerable<AccountResponse>>>();
        var okResult = result as Ok<IEnumerable<AccountResponse>>;
        okResult!.StatusCode.Should().Be(StatusCodes.Status200OK);
        okResult.Value.Should().BeEquivalentTo(expectedAccounts);

        _mockAccountService.Verify(s => s.GetAllAsync(null, _cancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetAllAccounts_WithStatusFilter_ReturnsFilteredAccounts()
    {
        // Arrange
        var status = "Active";
        var expectedAccounts = new List<AccountResponse>
        {
            new AccountResponse(
                Guid.NewGuid(),
                "John Doe",
                "12345678900",
                100.50m,
                "Active",
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow
            )
        };

        _mockAccountService
            .Setup(s => s.GetAllAsync(status, _cancellationToken))
            .ReturnsAsync(expectedAccounts);

        // Act
        var result = await GetAllAccountsEndpoint(status);

        // Assert
        result.Should().BeOfType<Ok<IEnumerable<AccountResponse>>>();
        var okResult = result as Ok<IEnumerable<AccountResponse>>;
        okResult!.StatusCode.Should().Be(StatusCodes.Status200OK);
        okResult.Value.Should().BeEquivalentTo(expectedAccounts);
        okResult.Value.Should().AllSatisfy(a => a.Status.Should().Be("Active"));

        _mockAccountService.Verify(s => s.GetAllAsync(status, _cancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetAllAccounts_WhenNoAccountsExist_ReturnsEmptyList()
    {
        // Arrange
        _mockAccountService
            .Setup(s => s.GetAllAsync(null, _cancellationToken))
            .ReturnsAsync(new List<AccountResponse>());

        // Act
        var result = await GetAllAccountsEndpoint(null);

        // Assert
        result.Should().BeOfType<Ok<IEnumerable<AccountResponse>>>();
        var okResult = result as Ok<IEnumerable<AccountResponse>>;
        okResult!.StatusCode.Should().Be(StatusCodes.Status200OK);
        okResult.Value.Should().BeEmpty();

        _mockAccountService.Verify(s => s.GetAllAsync(null, _cancellationToken), Times.Once);
    }

    #endregion

    #region PATCH /api/accounts/{id}/status Tests

    [Fact]
    public async Task UpdateAccountStatus_WithValidRequest_ReturnsOkResult()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var request = new UpdateAccountStatusRequest("Blocked");
        var expectedResponse = new AccountResponse(
            accountId,
            "John Doe",
            "12345678900",
            100.50m,
            "Blocked",
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow
        );

        _mockAccountService
            .Setup(s => s.UpdateStatusAsync(accountId, request, _cancellationToken))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await UpdateAccountStatusEndpoint(accountId, request);

        // Assert
        result.Should().BeOfType<Ok<AccountResponse>>();
        var okResult = result as Ok<AccountResponse>;
        okResult!.StatusCode.Should().Be(StatusCodes.Status200OK);
        okResult.Value.Should().BeEquivalentTo(expectedResponse);
        okResult.Value!.Status.Should().Be("Blocked");

        _mockAccountService.Verify(s => s.UpdateStatusAsync(accountId, request, _cancellationToken), Times.Once);
    }

    [Fact]
    public async Task UpdateAccountStatus_ToActivate_ReturnsOkResult()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var request = new UpdateAccountStatusRequest("Active");
        var expectedResponse = new AccountResponse(
            accountId,
            "John Doe",
            "12345678900",
            100.50m,
            "Active",
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow
        );

        _mockAccountService
            .Setup(s => s.UpdateStatusAsync(accountId, request, _cancellationToken))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await UpdateAccountStatusEndpoint(accountId, request);

        // Assert
        result.Should().BeOfType<Ok<AccountResponse>>();
        var okResult = result as Ok<AccountResponse>;
        okResult!.StatusCode.Should().Be(StatusCodes.Status200OK);
        okResult.Value!.Status.Should().Be("Active");

        _mockAccountService.Verify(s => s.UpdateStatusAsync(accountId, request, _cancellationToken), Times.Once);
    }

    [Fact]
    public async Task UpdateAccountStatus_ToClosed_ReturnsOkResult()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var request = new UpdateAccountStatusRequest("Closed");
        var expectedResponse = new AccountResponse(
            accountId,
            "John Doe",
            "12345678900",
            0m,
            "Closed",
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow
        );

        _mockAccountService
            .Setup(s => s.UpdateStatusAsync(accountId, request, _cancellationToken))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await UpdateAccountStatusEndpoint(accountId, request);

        // Assert
        result.Should().BeOfType<Ok<AccountResponse>>();
        var okResult = result as Ok<AccountResponse>;
        okResult!.StatusCode.Should().Be(StatusCodes.Status200OK);
        okResult.Value!.Status.Should().Be("Closed");

        _mockAccountService.Verify(s => s.UpdateStatusAsync(accountId, request, _cancellationToken), Times.Once);
    }

    #endregion

    #region Helper Methods - Simulando os endpoints

    private async Task<IResult> CreateAccountEndpoint(CreateAccountRequest request)
    {
        var validation = await _mockValidator.Object.ValidateAsync(request, _cancellationToken);
        if (!validation.IsValid)
            return Results.ValidationProblem(validation.ToDictionary());

        var account = await _mockAccountService.Object.CreateAsync(request, _cancellationToken);
        return Results.Created($"/api/accounts/{account.Id}", account);
    }

    private async Task<IResult> GetAccountByIdEndpoint(Guid id)
    {
        var account = await _mockAccountService.Object.GetByIdAsync(id, _cancellationToken);
        return account is null
            ? Results.NotFound(new ProblemDetails { Title = "Account not found", Status = 404 })
            : Results.Ok(account);
    }

    private async Task<IResult> GetAllAccountsEndpoint(string? status)
    {
        var accounts = await _mockAccountService.Object.GetAllAsync(status, _cancellationToken);
        return Results.Ok(accounts);
    }

    private async Task<IResult> UpdateAccountStatusEndpoint(Guid id, UpdateAccountStatusRequest request)
    {
        var account = await _mockAccountService.Object.UpdateStatusAsync(id, request, _cancellationToken);
        return Results.Ok(account);
    }

    #endregion
}
