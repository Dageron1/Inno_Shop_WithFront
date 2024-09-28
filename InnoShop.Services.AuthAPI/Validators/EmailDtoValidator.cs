using FluentValidation;
using InnoShop.Services.AuthAPI.Models.Dto;

namespace InnoShop.Services.AuthAPI.Validators
{
    public class EmailDtoValidator : AbstractValidator<EmailDto>
    {
        public EmailDtoValidator()
        {
            RuleFor(x => x.Email)
               .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Invalid email format.");
        }
    }
}
