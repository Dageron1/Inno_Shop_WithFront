using FluentValidation.TestHelper;
using InnoShop.Services.ProductAPI.Validators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoShop.Services.ProductAPI.UnitTests.Validators
{
    public class IdValidatorTests
    {
        private IdValidator _validator;

        [SetUp]
        public void SetUp()
        {
            _validator = new IdValidator();
        }

        [Test]
        public void Should_HaveError_When_ValueIsZeroOrLess()
        {
            // Arrange
            var invalidValue = 0;

            // Act
            var result = _validator.TestValidate(invalidValue);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x)
                .WithErrorMessage("The value must be greater than 0.");
        }

        [Test]
        public void Should_NotHaveError_When_ValueIsGreaterThanZero()
        {
            // Arrange
            var validValue = 5;

            // Act
            var result = _validator.TestValidate(validValue);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x);
        }
    }
}
