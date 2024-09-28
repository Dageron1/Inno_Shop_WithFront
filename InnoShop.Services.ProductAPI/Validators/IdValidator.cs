using FluentValidation;

namespace InnoShop.Services.ProductAPI.Validators
{
    public class IdValidator : AbstractValidator<int>
    {
        public IdValidator()
        {
            RuleFor(x => x)
                .GreaterThan(0).WithMessage("The value must be greater than 0.");
        }
    }
}
