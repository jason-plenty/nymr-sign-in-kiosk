using FluentValidation;
using NymrSignIn.Application.Register.Dtos;

namespace NymrSignIn.Application.Register.Validators;

public sealed class SignInRequestValidator : AbstractValidator<SignInRequest>
{
    public SignInRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters.");

        RuleFor(x => x.Organisation)
            .NotEmpty().WithMessage("Organisation is required.")
            .MaximumLength(200).WithMessage("Organisation must not exceed 200 characters.");
    }
}
