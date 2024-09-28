using FluentValidation.TestHelper;
using InnoShop.Services.ProductAPI.Validators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoShop.Services.ProductAPI.UnitTests.Validators
{
    public class StringValidatorTests
    {
        private StringValidator _validator;

        [SetUp]
        public void Setup()
        {
            _validator = new StringValidator();
        }

        [Test]
        public void Should_Have_Error_When_Field_Is_Empty()
        {
            // Arrange
            var input = "";

            // Act
            var result = _validator.TestValidate(input);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x)
                .WithErrorMessage("The field cannot be empty.");
        }

        [Test]
        public void Should_Have_Error_When_Length_Is_Less_Than_4()
        {
            // Arrange
            var input = "abc";

            // Act
            var result = _validator.TestValidate(input);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x)
                .WithErrorMessage("The field must be between 4 and 20 characters.");
        }

        [Test]
        public void Should_Have_Error_When_Length_Is_Greater_Than_20()
        {
            // Arrange
            var input = "abcdefghijklmnopqrstuvw";

            // Act
            var result = _validator.TestValidate(input);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x)
                .WithErrorMessage("The field must be between 4 and 20 characters.");
        }

        [Test]
        public void Should_Have_Error_When_Input_Contains_Invalid_Characters()
        {
            // Arrange
            var input = "abc$%^";

            // Act
            var result = _validator.TestValidate(input);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x)
                .WithErrorMessage("Only alphanumeric characters, spaces, and hyphens are allowed");
        }

        [Test]
        public void Should_Not_Have_Error_When_Input_Is_Valid()
        {
            // Arrange
            var input = "Valid-Input";

            // Act
            var result = _validator.TestValidate(input);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x);
        }
    }
}

