using FluentValidation;
using NymrSignIn.Application.Register.Dtos;

namespace NymrSignIn.Application.Register.Validators;

public sealed class ConfirmFitRequestValidator : AbstractValidator<ConfirmFitRequest>
{
    public ConfirmFitRequestValidator()
    {
        RuleFor(x => x.AdditionalInfo)
            .MaximumLength(2000).WithMessage("Additional information must not exceed 2000 characters.");

        RuleFor(x => x.SiteCode)
            .MaximumLength(32).WithMessage("Site code must not exceed 32 characters.");
    }
}
