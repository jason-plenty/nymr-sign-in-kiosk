using FluentValidation;
using NymrSignIn.Application.Register.Admin.Dtos;

namespace NymrSignIn.Application.Register.Admin.Validators;

public sealed class RegisterSearchCriteriaValidator : AbstractValidator<RegisterSearchCriteria>
{
    public RegisterSearchCriteriaValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("Page must be 1 or greater.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 200).WithMessage("PageSize must be between 1 and 200.");

        RuleFor(x => x.Search)
            .MaximumLength(200).WithMessage("Search must not exceed 200 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Search));

        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.SortBy).IsInEnum();

        RuleFor(x => x)
            .Must(x => !x.FromDate.HasValue || !x.ToDate.HasValue || x.FromDate.Value <= x.ToDate.Value)
            .WithMessage("FromDate must be on or before ToDate.");
    }
}
