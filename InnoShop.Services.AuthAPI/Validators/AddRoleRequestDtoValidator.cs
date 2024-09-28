using FluentValidation;
using InnoShop.Services.AuthAPI.Models.Dto;

namespace InnoShop.Services.AuthAPI.Validators
{
    public class AddRoleRequestDtoValidator : AbstractValidator<AddRoleRequestDto>
    {
        public AddRoleRequestDtoValidator()
        {
            RuleFor(x => x.Role)
                .NotEmpty().WithMessage("Role is required.")
                .Length(2, 20).WithMessage("Role must be between 2 and 20 characters.");
        }
    }
}
