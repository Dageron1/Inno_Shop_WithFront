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
    public class LoginRequestDtoValidatorTests
    {
        private LoginRequestDtoValidator _validator;

        [SetUp]
        public void SetUp()
        {
            _validator = new LoginRequestDtoValidator();
        }

        [Test]
        public void ShouldHaveError_WhenEmailIsEmpty()
        {
            // Arrange
            var model = new LoginRequestDto { Email = "", Password = "ValidPass1!" };

            // Act & Assert
            var result = _validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(x => x.Email)
                  .WithErrorMessage("Email is required.");
        }

        [Test]
        public void ShouldHaveError_WhenEmailIsInvalidFormat()
        {
            // Arrange
            var model = new LoginRequestDto { Email = "invalid-email", Password = "ValidPass1!" };

            // Act & Assert
            var result = _validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(x => x.Email)
                  .WithErrorMessage("Invalid email format.");
        }

        [Test]
        public void ShouldHaveError_WhenPasswordIsEmpty()
        {
            // Arrange
            var model = new LoginRequestDto { Email = "user@example.com", Password = "" };

            // Act & Assert
            var result = _validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(x => x.Password)
                  .WithErrorMessage("Password is required.");
        }

        [Test]
        public void ShouldHaveError_WhenPasswordIsTooShort()
        {
            // Arrange
            var model = new LoginRequestDto { Email = "user@example.com", Password = "Ab1!" };

            // Act & Assert
            var result = _validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(x => x.Password)
                  .WithErrorMessage("Password must be at least 6 characters long.");
        }

        [Test]
        public void ShouldHaveError_WhenPasswordHasNoUppercase()
        {
            // Arrange
            var model = new LoginRequestDto { Email = "user@example.com", Password = "validpass1!" }; 

            // Act & Assert
            var result = _validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(x => x.Password)
                  .WithErrorMessage("Password must contain at least one uppercase letter.");
        }

        [Test]
        public void ShouldHaveError_WhenPasswordHasNoLowercase()
        {
            // Arrange
            var model = new LoginRequestDto { Email = "user@example.com", Password = "VALIDPASS1!" }; 

            // Act & Assert
            var result = _validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(x => x.Password)
                  .WithErrorMessage("Password must contain at least one lowercase letter.");
        }

        [Test]
        public void ShouldHaveError_WhenPasswordHasNoDigit()
        {
            // Arrange
            var model = new LoginRequestDto { Email = "user@example.com", Password = "ValidPass!" }; 

            // Act & Assert
            var result = _validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(x => x.Password)
                  .WithErrorMessage("Password must contain at least one digit.");
        }

        [Test]
        public void ShouldHaveError_WhenPasswordHasNoSpecialCharacter()
        {
            // Arrange
            var model = new LoginRequestDto { Email = "user@example.com", Password = "ValidPass1" }; 

            // Act & Assert
            var result = _validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(x => x.Password)
                  .WithErrorMessage("Password must contain at least one special character.");
        }

        [Test]
        public void ShouldNotHaveError_WhenEmailAndPasswordAreValid()
        {
            // Arrange
            var model = new LoginRequestDto { Email = "user@example.com", Password = "ValidPass1!" };

            // Act & Assert
            var result = _validator.TestValidate(model);
            result.ShouldNotHaveValidationErrorFor(x => x.Email);
            result.ShouldNotHaveValidationErrorFor(x => x.Password);
        }
    }
}
