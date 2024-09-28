using FluentValidation;
using InnoShop.Services.AuthAPI.Models.Dto;

namespace InnoShop.Services.AuthAPI.Validators
{
    public class PaginationParamsValidator : AbstractValidator<PaginationParams>
    {
        public PaginationParamsValidator()
        {
            RuleFor(x => x.PageNumber)
                .GreaterThanOrEqualTo(1).WithMessage("Page number must be greater than or equal to 1.");

            RuleFor(x => x.PageSize)
                .GreaterThan(0).WithMessage("Page size must be greater than 0.")
                .LessThanOrEqualTo(100).WithMessage("Page size must be less than or equal to 100.");
        }
    }
}
