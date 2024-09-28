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
    public class PaginationParamsValidatorTests
    {
        private PaginationParamsValidator _validator;

        [SetUp]
        public void SetUp()
        {
            _validator = new PaginationParamsValidator();
        }

        // Тест для PageNumber
        [Test]
        public void ShouldHaveError_WhenPageNumberIsLessThanOne()
        {
            // Arrange
            var model = new PaginationParams { PageNumber = 0, PageSize = 10 };

            // Act & Assert
            var result = _validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(x => x.PageNumber)
                  .WithErrorMessage("Page number must be greater than or equal to 1.");
        }

        [Test]
        public void ShouldNotHaveError_WhenPageNumberIsValid()
        {
            // Arrange
            var model = new PaginationParams { PageNumber = 1, PageSize = 10 };

            // Act & Assert
            var result = _validator.TestValidate(model);
            result.ShouldNotHaveValidationErrorFor(x => x.PageNumber);
        }

        // Тесты для PageSize
        [Test]
        public void ShouldHaveError_WhenPageSizeIsLessThanOrEqualToZero()
        {
            // Arrange
            var model = new PaginationParams { PageNumber = 1, PageSize = 0 };

            // Act & Assert
            var result = _validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(x => x.PageSize)
                  .WithErrorMessage("Page size must be greater than 0.");
        }

        [Test]
        public void ShouldHaveError_WhenPageSizeIsGreaterThan100()
        {
            // Arrange
            var model = new PaginationParams { PageNumber = 1, PageSize = 101 };

            // Act & Assert
            var result = _validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(x => x.PageSize)
                  .WithErrorMessage("Page size must be less than or equal to 100.");
        }

        [Test]
        public void ShouldNotHaveError_WhenPageSizeIsValid()
        {
            // Arrange
            var model = new PaginationParams { PageNumber = 1, PageSize = 50 };

            // Act & Assert
            var result = _validator.TestValidate(model);
            result.ShouldNotHaveValidationErrorFor(x => x.PageSize);
        }
    }

}
