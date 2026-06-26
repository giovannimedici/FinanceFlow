
using FluentValidation;

namespace FinanceFlow.API.Validators;

public class TransferRequestValidator : AbstractValidator<TransferRequest>
{
    public TransferRequestValidator()
    {
        RuleFor(x => x.SourceAccountId).NotEmpty();
        RuleFor(x => x.DestinationAccountId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0).LessThanOrEqualTo(999_999.99m);
        RuleFor(x => x)
            .Must(x => x.SourceAccountId != x.DestinationAccountId)
            .WithMessage("Source and destination accounts must differ.");
    }
}