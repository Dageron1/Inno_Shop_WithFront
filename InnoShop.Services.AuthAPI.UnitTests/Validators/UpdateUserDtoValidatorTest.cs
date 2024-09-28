using FluentValidation.TestHelper;
using InnoShop.Services.AuthAPI.Models.Dto;
using InnoShop.Services.AuthAPI.Validators;
using NUnit.Framework;

[TestFixture]
public class UpdateUserDtoValidatorTests
{
    private UpdateUserDtoValidator _validator;

    [SetUp]
    public void SetUp()
    {
        _validator = new UpdateUserDtoValidator();
    }

    // Name
    [Test]
    public void ShouldHaveError_WhenNameIsEmpty()
    {
        var model = new UpdateUserDto { Name = "", PhoneNumber = "+1234567890" };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Name).WithErrorMessage("Name is required.");
    }

    [Test]
    public void ShouldHaveError_WhenNameIsTooShort()
    {
        var model = new UpdateUserDto { Name = "A", PhoneNumber = "+1234567890" };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Name must be between 2 and 50 characters.");
    }

    [Test]
    public void ShouldHaveError_WhenNameIsInvalid()
    {
        var model = new UpdateUserDto { Name = "string", PhoneNumber = "+1234567890" };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Name).WithErrorMessage("Invalid name.");
    }

    // PhoneNumber
    [Test]
    public void ShouldHaveError_WhenPhoneNumberIsEmpty()
    {
        var model = new UpdateUserDto { Name = "Valid Name", PhoneNumber = "" };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.PhoneNumber)
            .WithErrorMessage("Phone number is required.");
    }

    [Test]
    public void ShouldHaveError_WhenPhoneNumberIsInvalid()
    {
        var model = new UpdateUserDto { Name = "Valid Name", PhoneNumber = "12345" };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.PhoneNumber)
            .WithErrorMessage("Phone number must be valid and include a country code.");
    }

    [Test]
    public void ShouldNotHaveError_WhenAllFieldsAreValid()
    {
        var model = new UpdateUserDto { Name = "Valid Name", PhoneNumber = "+1234567890" };
        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
        result.ShouldNotHaveValidationErrorFor(x => x.PhoneNumber);
    }
}
