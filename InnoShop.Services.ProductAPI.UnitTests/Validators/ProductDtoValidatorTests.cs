using FluentValidation.TestHelper;
using InnoShop.Services.ProductAPI.Models.Dto;
using InnoShop.Services.ProductAPI.Validators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoShop.Services.ProductAPI.UnitTests.Validators
{
    [TestFixture]
    public class ProductDtoValidatorTests
    {
        private ProductDtoValidator _validator;

        [SetUp]
        public void Setup()
        {
            _validator = new ProductDtoValidator();
        }

        [Test]
        public void Should_Have_Error_When_Name_Is_Empty()
        {
            // Arrange
            var model = new ProductDto { Name = string.Empty };

            // Act
            var result = _validator.TestValidate(model);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Name)
                  .WithErrorMessage("Product name is required.");
        }

        [Test]
        public void Should_Not_Have_Error_When_Name_Is_Valid()
        {
            // Arrange
            var model = new ProductDto { Name = "Valid Product Name" };

            // Act
            var result = _validator.TestValidate(model);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Name);
        }

        [Test]
        public void Should_Have_Error_When_Description_Is_Empty()
        {
            // Arrange
            var model = new ProductDto { Description = string.Empty };

            // Act
            var result = _validator.TestValidate(model);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Description)
                  .WithErrorMessage("Description is required.");
        }

        [Test]
        public void Should_Have_Error_When_Description_Length_Is_Invalid()
        {
            // Arrange
            var model = new ProductDto { Description = "1234" }; // Less than 5 characters

            // Act
            var result = _validator.TestValidate(model);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Description)
                  .WithErrorMessage("Description must be between 5 and 500 characters.");
        }

        [Test]
        public void Should_Have_Error_When_Description_Is_String()
        {
            // Arrange
            var model = new ProductDto { Description = "string" };

            // Act
            var result = _validator.TestValidate(model);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Description)
                  .WithErrorMessage("Invalid description.");
        }

        [Test]
        public void Should_Not_Have_Error_When_Description_Is_Valid()
        {
            // Arrange
            var model = new ProductDto { Description = "Valid product description." };

            // Act
            var result = _validator.TestValidate(model);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Description);
        }

        [Test]
        public void Should_Have_Error_When_CategoryName_Is_Empty()
        {
            // Arrange
            var model = new ProductDto { CategoryName = string.Empty };

            // Act
            var result = _validator.TestValidate(model);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.CategoryName)
                  .WithErrorMessage("Category name is required.");
        }

        [Test]
        public void Should_Have_Error_When_CategoryName_Has_Invalid_Characters()
        {
            // Arrange
            var model = new ProductDto { CategoryName = "Invalid@Category!" }; // Contains invalid characters

            // Act
            var result = _validator.TestValidate(model);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.CategoryName)
                  .WithErrorMessage("This field can only contain alphanumeric characters and spaces.");
        }

        [Test]
        public void Should_Have_Error_When_ImageUrl_Is_Invalid()
        {
            // Arrange
            var model = new ProductDto { ImageUrl = "invalid-url" };

            // Act
            var result = _validator.TestValidate(model);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.ImageUrl)
                  .WithErrorMessage("Invalid image URL.");
        }

        [Test]
        public void Should_Not_Have_Error_When_ImageUrl_Is_Valid()
        {
            // Arrange
            var model = new ProductDto { ImageUrl = "http://validurl.com/image.jpg" };

            // Act
            var result = _validator.TestValidate(model);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.ImageUrl);
        }

        [Test]
        public void Should_Have_Error_When_Price_Is_Less_Than_Zero()
        {
            // Arrange
            var model = new ProductDto { Price = -5 };

            // Act
            var result = _validator.TestValidate(model);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Price)
                  .WithErrorMessage("Price must be greater than 0.");
        }

        [Test]
        public void Should_Not_Have_Error_When_Price_Is_Valid()
        {
            // Arrange
            var model = new ProductDto { Price = 10 };

            // Act
            var result = _validator.TestValidate(model);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Price);
        }
    }
}
