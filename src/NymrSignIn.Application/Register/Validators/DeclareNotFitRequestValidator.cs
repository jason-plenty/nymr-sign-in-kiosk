using FluentValidation;
using NymrSignIn.Application.Register.Dtos;

namespace NymrSignIn.Application.Register.Validators;

public sealed class DeclareNotFitRequestValidator : AbstractValidator<DeclareNotFitRequest>
{
    public DeclareNotFitRequestValidator()
    {
        RuleFor(x => x.AdditionalInfo)
            .MaximumLength(2000).WithMessage("Additional information must not exceed 2000 characters.");
    }
}
