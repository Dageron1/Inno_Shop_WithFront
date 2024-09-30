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
    public class AddRoleRequestDtoValidatorTests
    {
        private AddRoleRequestDtoValidator _validator;

        [SetUp]
        public void SetUp()
        {
            _validator = new AddRoleRequestDtoValidator();
        }

        [Test]
        public void ShouldHaveError_WhenRoleIsEmpty()
        {
            // Arrange
            var model = new AddRoleRequestDto { Role = "" };

            // Act & Assert
            var result = _validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(x => x.Role)
                  .WithErrorMessage("Role is required.");
        }

        [Test]
        public void ShouldNotHaveError_WhenRoleIsProvided()
        {
            // Arrange
            var model = new AddRoleRequestDto { Role = "Admin" };

            // Act & Assert
            var result = _validator.TestValidate(model);
            result.ShouldNotHaveValidationErrorFor(x => x.Role);
        }
    }
}
