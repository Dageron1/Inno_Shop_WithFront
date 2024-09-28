using FluentValidation.TestHelper;
using InnoShop.Services.AuthAPI.Models.Dto;
using InnoShop.Services.AuthAPI.Validators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoShop.Services.AuthAPI.UnitTests.Validators
{
    [TestFixture]
    public class EmailDtoValidatorTests
    {
        private EmailDtoValidator _validator;

        [SetUp]
        public void SetUp()
        {
            _validator = new EmailDtoValidator();
        }

        [Test]
        public void ShouldHaveError_WhenEmailIsEmpty()
        {
            // Arrange
            var model = new EmailDto { Email = "" };

            // Act & Assert
            var result = _validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(x => x.Email)
                  .WithErrorMessage("Email is required.");
        }

        [Test]
        public void ShouldHaveError_WhenEmailIsInvalidFormat()
        {
            // Arrange
            var model = new EmailDto { Email = "invalid-email" };

            // Act & Assert
            var result = _validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(x => x.Email)
                  .WithErrorMessage("Invalid email format.");
        }

        [Test]
        public void ShouldNotHaveError_WhenEmailIsValid()
        {
            // Arrange
            var model = new EmailDto { Email = "user@example.com" };

            // Act & Assert
            var result = _validator.TestValidate(model);
            result.ShouldNotHaveValidationErrorFor(x => x.Email);
        }
    }
}
