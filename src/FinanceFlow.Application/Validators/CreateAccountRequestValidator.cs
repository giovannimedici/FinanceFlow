using FluentValidation;

namespace FinanceFlow.Application.Validators;

public class CreateAccountRequestValidator : AbstractValidator<CreateAccountRequest>
{
    public CreateAccountRequestValidator()
    {
        RuleFor(x => x.OwnerName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.DocumentNumber)
            .NotEmpty()
            .Matches(@"^\d{11}$|^\d{14}$")
            .WithMessage("DocumentNumber must have 11 or 14 digits.");
    }
}