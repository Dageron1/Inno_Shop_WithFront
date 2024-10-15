using FluentValidation;

namespace InnoShop.Services.ProductAPI.Validators
{
    // rename
    public class StringValidator : AbstractValidator<string>
    {
        public StringValidator()
        {
            RuleFor(x => x)
                .NotEmpty().WithMessage("The field cannot be empty.")
                .Length(4, 20).WithMessage("The field must be between 4 and 20 characters.")
                .Matches("^[a-zA-Z0-9 -]*$").WithMessage("Only alphanumeric characters, spaces, and hyphens are allowed");
        }
    }
}
