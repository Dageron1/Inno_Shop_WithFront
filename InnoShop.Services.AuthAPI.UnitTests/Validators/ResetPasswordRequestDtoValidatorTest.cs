using FluentValidation.TestHelper;
using InnoShop.Services.AuthAPI.Models.Dto;
using InnoShop.Services.AuthAPI.Validators;
using NUnit.Framework;

[TestFixture]
public class ResetPasswordRequestDtoValidatorTests
{
    private ResetPasswordRequestDtoValidator _validator;

    [SetUp]
    public void SetUp()
    {
        _validator = new ResetPasswordRequestDtoValidator();
    }

    [Test]
    public void ShouldHaveError_WhenEmailIsEmpty()
    {
        var model = new ResetPasswordRequestDto 
        { Email = "", Token = "validToken", NewPassword = "ValidPass1!" };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Email).WithErrorMessage("Email is required.");
    }

    [Test]
    public void ShouldHaveError_WhenEmailIsInvalidFormat()
    {
        var model = new ResetPasswordRequestDto 
        { Email = "invalid-email", Token = "validToken", NewPassword = "ValidPass1!" };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Email).WithErrorMessage("Invalid email format.");
    }

    [Test]
    public void ShouldHaveError_WhenTokenIsEmpty()
    {
        var model = new ResetPasswordRequestDto 
        { Email = "user@example.com", Token = "", NewPassword = "ValidPass1!" };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Token).WithErrorMessage("Token is required.");
    }

    [Test]
    public void ShouldHaveError_WhenNewPasswordIsEmpty()
    {
        var model = new ResetPasswordRequestDto 
        { Email = "user@example.com", Token = "validToken", NewPassword = "" };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.NewPassword).WithErrorMessage("Password is required.");
    }

    [Test]
    public void ShouldHaveError_WhenNewPasswordIsTooShort()
    {
        var model = new ResetPasswordRequestDto 
        { Email = "user@example.com", Token = "validToken", NewPassword = "Ab1!" };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.NewPassword)
            .WithErrorMessage("Password must be at least 6 characters long.");
    }

    [Test]
    public void ShouldHaveError_WhenNewPasswordHasNoUppercaseLetter()
    {
        var model = new ResetPasswordRequestDto 
        { Email = "user@example.com", Token = "validToken", NewPassword = "validpass1!" };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.NewPassword)
            .WithErrorMessage("Password must contain at least one uppercase letter.");
    }

    [Test]
    public void ShouldHaveError_WhenNewPasswordHasNoSpecialCharacter()
    {
        var model = new ResetPasswordRequestDto 
        { Email = "user@example.com", Token = "validToken", NewPassword = "ValidPass1" };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.NewPassword)
            .WithErrorMessage("Password must contain at least one special character.");
    }

    [Test]
    public void ShouldNotHaveError_WhenAllFieldsAreValid()
    {
        var model = new ResetPasswordRequestDto 
        { Email = "user@example.com", Token = "validToken", NewPassword = "ValidPass1!" };
        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.Email);
        result.ShouldNotHaveValidationErrorFor(x => x.Token);
        result.ShouldNotHaveValidationErrorFor(x => x.NewPassword);
    }
}
