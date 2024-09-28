using FluentValidation;
using InnoShop.Services.AuthAPI.Models.Dto;

namespace InnoShop.Services.AuthAPI.Validators
{
    public class RegistrationRequestDtoValidator : AbstractValidator<RegistrationRequestDto>
    {
        public RegistrationRequestDtoValidator()
        {
            RuleFor(x => x.Email)
               .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Invalid email format.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required.")
                .Length(2, 20).WithMessage("Name must be between 2 and 50 characters.")
                .Must(name => !string.Equals(name, "string", StringComparison.OrdinalIgnoreCase))
                .WithMessage("Invalid name.");

            RuleFor(x => x.PhoneNumber)
                .NotEmpty().WithMessage("Phone number is required.")
                .Matches(@"^\+?[1-9]\d{9,14}$").WithMessage("Phone number must be valid and include a country code.");

            RuleFor(x => x.Password)
               .NotEmpty().WithMessage("Password is required.")
                .MinimumLength(6).WithMessage("Password must be at least 6 characters long.")
                .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
                .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
                .Matches("[0-9]").WithMessage("Password must contain at least one digit.")
                .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");
        }
    }
}
