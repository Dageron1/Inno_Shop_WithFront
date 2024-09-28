using FluentValidation.TestHelper;
using InnoShop.Services.AuthAPI.Models.Dto;
using InnoShop.Services.AuthAPI.Validators;
using NUnit.Framework;

[TestFixture]
public class RegistrationRequestDtoValidatorTests
{
    private RegistrationRequestDtoValidator _validator;

    [SetUp]
    public void SetUp()
    {
        _validator = new RegistrationRequestDtoValidator();
    }

    [Test]
    public void ShouldHaveError_WhenEmailIsEmpty()
    {
        var model = new RegistrationRequestDto 
        { Email = "", Name = "Valid Name", PhoneNumber = "+1234567890", Password = "ValidPass1!" };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Email).WithErrorMessage("Email is required.");
    }

    [Test]
    public void ShouldHaveError_WhenEmailIsInvalidFormat()
    {
        var model = new RegistrationRequestDto 
        { Email = "invalid-email", Name = "Valid Name", PhoneNumber = "+1234567890", Password = "ValidPass1!" };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Email).WithErrorMessage("Invalid email format.");
    }

    // Name
    [Test]
    public void ShouldHaveError_WhenNameIsEmpty()
    {
        var model = new RegistrationRequestDto 
        { Email = "user@example.com", Name = "", PhoneNumber = "+1234567890", Password = "ValidPass1!" };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Name).WithErrorMessage("Name is required.");
    }

    [Test]
    public void ShouldHaveError_WhenNameIsInvalidLength()
    {
        var model = new RegistrationRequestDto
        { Email = "user@example.com", Name = "A", PhoneNumber = "+1234567890", Password = "ValidPass1!" };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Name).WithErrorMessage("Name must be between 2 and 50 characters.");
    }

    [Test]
    public void ShouldHaveError_WhenNameIsInvalid()
    {
        var model = new RegistrationRequestDto 
        { Email = "user@example.com", Name = "string", PhoneNumber = "+1234567890", Password = "ValidPass1!" };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Name).WithErrorMessage("Invalid name.");
    }

    // PhoneNumber
    [Test]
    public void ShouldHaveError_WhenPhoneNumberIsEmpty()
    {
        var model = new RegistrationRequestDto 
        { Email = "user@example.com", Name = "Valid Name", PhoneNumber = "", Password = "ValidPass1!" };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.PhoneNumber).WithErrorMessage("Phone number is required.");
    }

    [Test]
    public void ShouldHaveError_WhenPhoneNumberIsInvalid()
    {
        var model = new RegistrationRequestDto 
        { Email = "user@example.com", Name = "Valid Name", PhoneNumber = "12345", Password = "ValidPass1!" };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.PhoneNumber)
            .WithErrorMessage("Phone number must be valid and include a country code.");
    }

    [Test]
    public void ShouldHaveError_WhenPasswordIsEmpty()
    {
        var model = new RegistrationRequestDto 
        { Email = "user@example.com", Name = "Valid Name", PhoneNumber = "+1234567890", Password = "" };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Password).WithErrorMessage("Password is required.");
    }

    [Test]
    public void ShouldHaveError_WhenPasswordIsTooShort()
    {
        var model = new RegistrationRequestDto 
        { Email = "user@example.com", Name = "Valid Name", PhoneNumber = "+1234567890", Password = "Ab1!" };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Password must be at least 6 characters long.");
    }

    [Test]
    public void ShouldHaveError_WhenPasswordHasNoUppercaseLetter()
    {
        var model = new RegistrationRequestDto 
        { Email = "user@example.com", Name = "Valid Name", PhoneNumber = "+1234567890", Password = "validpass1!" };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Password must contain at least one uppercase letter.");
    }

    [Test]
    public void ShouldHaveError_WhenPasswordHasNoSpecialCharacter()
    {
        var model = new RegistrationRequestDto 
        { Email = "user@example.com", Name = "Valid Name", PhoneNumber = "+1234567890", Password = "ValidPass1" };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Password must contain at least one special character.");
    }

    [Test]
    public void ShouldNotHaveError_WhenAllFieldsAreValid()
    {
        var model = new RegistrationRequestDto 
        { Email = "user@example.com", Name = "Valid Name", PhoneNumber = "+1234567890", Password = "ValidPass1!" };
        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.Email);
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
        result.ShouldNotHaveValidationErrorFor(x => x.PhoneNumber);
        result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }
}
