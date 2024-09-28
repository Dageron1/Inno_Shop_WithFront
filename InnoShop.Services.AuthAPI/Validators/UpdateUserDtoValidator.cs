using FluentValidation;
using InnoShop.Services.AuthAPI.Models.Dto;

namespace InnoShop.Services.AuthAPI.Validators
{
    public class UpdateUserDtoValidator : AbstractValidator<UpdateUserDto>
    {
        public UpdateUserDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required.")
                .Length(2, 20).WithMessage("Name must be between 2 and 50 characters.")
                .Must(name => !string.Equals(name, "string", StringComparison.OrdinalIgnoreCase))
                .WithMessage("Invalid name.");

            RuleFor(x => x.PhoneNumber)
                .NotEmpty().WithMessage("Phone number is required.")
                .Matches(@"^\+?[1-9]\d{9,14}$").WithMessage("Phone number must be valid and include a country code.");
        }
    }
}
