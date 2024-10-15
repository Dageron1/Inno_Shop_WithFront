using FluentValidation;
using InnoShop.Services.ProductAPI.Models.Dto;

namespace InnoShop.Services.ProductAPI.Validators
{
    public class ProductDtoValidator : AbstractValidator<ProductDto>
    {
        public ProductDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Product name is required.")
                .Length(3, 100).WithMessage("Product name must be between 3 and 100 characters.")
                .Must(name => !string.Equals(name, "string", StringComparison.OrdinalIgnoreCase))
                .WithMessage("Invalid name.");

            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("Description is required.")
                .Length(5, 500).WithMessage("Description must be between 5 and 500 characters.")
                .Must(name => !string.Equals(name, "string", StringComparison.OrdinalIgnoreCase))
                .WithMessage("Invalid description.");

            RuleFor(x => x.CategoryName)
                .NotEmpty().WithMessage("Category name is required.")
                .Length(3, 20).WithMessage("This field must be between 3 and 50 characters.")
                .Matches("^[a-zA-Z0-9 ]*$").WithMessage("This field can only contain alphanumeric characters and spaces.");

            RuleFor(x => x.ImageUrl)
                .NotEmpty().WithMessage("Image URL is required.")
                .Must(BeAValidUrl).WithMessage("Invalid image URL.");

            RuleFor(x => x.Price)
                .GreaterThan(0).WithMessage("Price must be greater than 0.");
        }
        private bool BeAValidUrl(string imageUrl)
        {
            return Uri.TryCreate(imageUrl, UriKind.Absolute, out var _);
        }

    }
}
