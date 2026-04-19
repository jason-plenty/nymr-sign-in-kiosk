using FluentValidation;
using NymrSignIn.Application.Register.Dtos;

namespace NymrSignIn.Application.Register.Validators;

public sealed class SubmitSiteCodeRequestValidator : AbstractValidator<SubmitSiteCodeRequest>
{
    public SubmitSiteCodeRequestValidator()
    {
        RuleFor(x => x.SiteCode)
            .NotEmpty().WithMessage("Site code is required.")
            .MaximumLength(32).WithMessage("Site code must not exceed 32 characters.");

        RuleFor(x => x.AdditionalInfo)
            .MaximumLength(2000).WithMessage("Additional information must not exceed 2000 characters.");
    }
}
