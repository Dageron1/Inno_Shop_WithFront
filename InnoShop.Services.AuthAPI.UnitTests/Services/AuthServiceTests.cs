using AutoMapper;
using FluentAssertions;
using InnoShop.Services.AuthAPI.Data;
using InnoShop.Services.AuthAPI.Models;
using InnoShop.Services.AuthAPI.Models.Dto;
using InnoShop.Services.AuthAPI.Services;
using InnoShop.Services.AuthAPI.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoShop.Services.AuthAPI.UnitTests.Services
{
    [TestFixture]
    public class AuthServiceTests
    {
        private AuthService _authService;
        private Mock<UserManager<ApplicationUser>> _userManagerMock;
        private Mock<RoleManager<IdentityRole>> _roleManagerMock;
        private Mock<IJwtTokenGenerator> _jwtTokenGeneratorMock;
        private Mock<IJwtTokenValidator> _jwtTokenValidatorMock;
        private Mock<IEmailSender> _emailSenderMock;

        [SetUp]
        public void SetUp()
        {
            _userManagerMock = GetMockUserManager();
            _roleManagerMock = GetMockRoleManager();
            _jwtTokenGeneratorMock = new Mock<IJwtTokenGenerator>();
            _jwtTokenValidatorMock = new Mock<IJwtTokenValidator>();
            _emailSenderMock = new Mock<IEmailSender>();

            _authService = new AuthService(
                _userManagerMock.Object,
                _roleManagerMock.Object,
                _jwtTokenGeneratorMock.Object,
                _jwtTokenValidatorMock.Object,
                _emailSenderMock.Object
            );
        }

        private Mock<UserManager<ApplicationUser>> GetMockUserManager()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            return new Mock<UserManager<ApplicationUser>>(store.Object, null, null, null, null, null, null, null, null);
        }

        private Mock<RoleManager<IdentityRole>> GetMockRoleManager()
        {
            var roleStoreMock = new Mock<IRoleStore<IdentityRole>>();
            return new Mock<RoleManager<IdentityRole>>(
                roleStoreMock.Object,
                new IRoleValidator<IdentityRole>[0],
                new UpperInvariantLookupNormalizer(),
                new IdentityErrorDescriber(),
                new Mock<ILogger<RoleManager<IdentityRole>>>().Object
            );
        }

        [Test]
        public async Task Register_ShouldReturnAuthServiceResultWithSuccessCode_WhenUserCreatedAndRoleExist()
        {
            // Arrange
            var registrationRequest = new RegistrationRequestDto
            {
                Email = "test@test.com",
                Password = "Password123*",
                Name = "Test User",
                PhoneNumber = "+48123456789"
            };

            var applicationUser = new ApplicationUser
            {
                UserName = registrationRequest.Email,
                Email = registrationRequest.Email,
                NormalizedEmail = registrationRequest.Email.ToUpper(),
                Name = registrationRequest.Name,
                PhoneNumber = registrationRequest.PhoneNumber
            };

            var expectedResult = new AuthServiceResult
            {
                ErrorCode = AuthErrorCode.Success,
                Result = applicationUser,
            };

            _userManagerMock.Setup(um => um.CreateAsync(It.IsAny<ApplicationUser>(), registrationRequest.Password))
                .ReturnsAsync(IdentityResult.Success);

            _roleManagerMock.Setup(rm => rm.RoleExistsAsync("User"))
                .ReturnsAsync(true);

            _roleManagerMock.Setup(r => r.CreateAsync(It.IsAny<IdentityRole>()))
                .ReturnsAsync(IdentityResult.Success);

            _userManagerMock.Setup(um => um.AddToRoleAsync(It.IsAny<ApplicationUser>(), "User"))
                            .ReturnsAsync(IdentityResult.Success);

            _userManagerMock.Setup(um => um.GenerateEmailConfirmationTokenAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync("fake-confirmation-token");

            _jwtTokenGeneratorMock.Setup(jw => jw.GenerateEmailConfirmationTokenAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns("fake-jwt-confirmation-token");

            _emailSenderMock.Setup(es => es.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var findEmailSequence = new MockSequence();

            _userManagerMock.InSequence(findEmailSequence)
                .Setup(um => um.FindByEmailAsync(applicationUser.Email.ToLower()))
                .ReturnsAsync((ApplicationUser)null);

            _userManagerMock.InSequence(findEmailSequence)
                .Setup(um => um.FindByEmailAsync(applicationUser.Email.ToLower()))
                .ReturnsAsync(applicationUser);

            _userManagerMock.InSequence(findEmailSequence)
                .Setup(um => um.FindByEmailAsync(applicationUser.Email.ToLower()))
                .ReturnsAsync(applicationUser);

            Func<string, string> emailMessageBuilder = token => $"Confirmation message with token: {token}";

            // Act
            var result = await _authService.Register(registrationRequest, emailMessageBuilder);

            // Assert
            result.Should().BeEquivalentTo(expectedResult);

            _userManagerMock.Verify(um => um.CreateAsync(It.IsAny<ApplicationUser>(), registrationRequest.Password), Times.Once);
            _roleManagerMock.Verify(rm => rm.RoleExistsAsync("User"), Times.Once);
            _roleManagerMock.Verify(rm => rm.CreateAsync(It.IsAny<IdentityRole>()), Times.Never);
            _userManagerMock.Verify(um => um.AddToRoleAsync(It.IsAny<ApplicationUser>(), "User"), Times.Once);
            _userManagerMock.Verify(um => um.GenerateEmailConfirmationTokenAsync(It.IsAny<ApplicationUser>()), Times.Once);
            _jwtTokenGeneratorMock.Verify(jw => jw.GenerateEmailConfirmationTokenAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            _emailSenderMock.Verify(es => es.SendEmailAsync(applicationUser.Email, "Confirm your email",
                It.Is<string>(s => s.Contains("fake-jwt-confirmation-token"))), Times.Once);
            _userManagerMock.Verify(um => um.FindByEmailAsync(applicationUser.Email.ToLower()), Times.Exactly(3));
        }

        [Test]
        public async Task Register_ShouldReturnAuthServiceResultWithSuccessCode_WhenUserIsCreatedAndRoleDoesNotExist()
        {
            // Arrange
            var registrationRequest = new RegistrationRequestDto
            {
                Email = "test@test.com",
                Password = "password",
                Name = "Test User",
                PhoneNumber = "123456789"
            };

            var applicationUser = new ApplicationUser
            {
                UserName = registrationRequest.Email,
                Email = registrationRequest.Email,
                NormalizedEmail = registrationRequest.Email,
                Name = registrationRequest.Name,
                PhoneNumber = registrationRequest.PhoneNumber
            };

            var expectedResult = new AuthServiceResult
            {
                ErrorCode = AuthErrorCode.Success,
                Result = applicationUser,
            };

            _userManagerMock.Setup(um => um.CreateAsync(It.IsAny<ApplicationUser>(), registrationRequest.Password))
                            .ReturnsAsync(IdentityResult.Success);

            _roleManagerMock.Setup(rm => rm.RoleExistsAsync("User"))
                            .ReturnsAsync(false);

            _roleManagerMock.Setup(rm => rm.CreateAsync(It.IsAny<IdentityRole>()))
                            .ReturnsAsync(IdentityResult.Success);

            _userManagerMock.Setup(um => um.GenerateEmailConfirmationTokenAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync("fake-confirmation-token");

            _jwtTokenGeneratorMock.Setup(jw => jw.GenerateEmailConfirmationTokenAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns("fake-jwt-confirmation-token");

            _emailSenderMock.Setup(es => es.SendEmailAsync(applicationUser.Email, "Confirm your email",
                It.Is<string>(s => s.Contains("fake-jwt-confirmation-token"))))
                .Returns(Task.CompletedTask);

            _userManagerMock.Setup(um => um.AddToRoleAsync(It.IsAny<ApplicationUser>(), "User"))
                            .ReturnsAsync(IdentityResult.Success);

            var findEmailSequence = new MockSequence();

            _userManagerMock.InSequence(findEmailSequence)
                .Setup(um => um.FindByEmailAsync(It.Is<string>(email => email == applicationUser.Email.ToLower())))
                .ReturnsAsync((ApplicationUser)null);

            _userManagerMock.InSequence(findEmailSequence)
                .Setup(um => um.FindByEmailAsync(It.Is<string>(email => email == applicationUser.Email.ToLower())))
                .ReturnsAsync(applicationUser);

            _userManagerMock.InSequence(findEmailSequence)
                .Setup(um => um.FindByEmailAsync(It.Is<string>(email => email == applicationUser.Email.ToLower())))
                .ReturnsAsync(applicationUser);

            Func<string, string> emailMessageBuilder = token => $"Confirmation message with token: {token}";

            // Act
            var result = await _authService.Register(registrationRequest, emailMessageBuilder);

            // Assert
            result.Should().BeEquivalentTo(expectedResult);

            _userManagerMock.Verify(um => um.CreateAsync(It.IsAny<ApplicationUser>(), registrationRequest.Password), Times.Once);
            _roleManagerMock.Verify(rm => rm.RoleExistsAsync("User"), Times.Once);
            _roleManagerMock.Verify(rm => rm.CreateAsync(It.IsAny<IdentityRole>()), Times.Once);
            _userManagerMock.Verify(um => um.AddToRoleAsync(It.IsAny<ApplicationUser>(), "User"), Times.Once);
            _userManagerMock.Verify(um => um.GenerateEmailConfirmationTokenAsync(It.IsAny<ApplicationUser>()));
            _jwtTokenGeneratorMock.Verify(j => j.GenerateEmailConfirmationTokenAsync(It.IsAny<string>(), It.IsAny<string>()));
            _emailSenderMock.Verify(es => es.SendEmailAsync(applicationUser.Email, "Confirm your email",
                It.Is<string>(s => s.Contains("fake-jwt-confirmation-token"))));
            _userManagerMock.Verify(x => x.FindByEmailAsync(applicationUser.Email.ToLower()), Times.Exactly(3));
        }

        [Test]
        public async Task Register_ShouldReturnAuthServiceResultWithErrorCode_WhenUserCreationFails()
        {
            // Arrange
            var registrationRequest = new RegistrationRequestDto
            {
                Email = "test@test.com",
                Password = "password",
                Name = "Test User",
                PhoneNumber = "123456789"
            };

            var applicationUser = new ApplicationUser
            {
                UserName = registrationRequest.Email,
                Email = registrationRequest.Email,
                NormalizedEmail = registrationRequest.Email.ToUpper(),
                Name = registrationRequest.Name.ToLower(),
                PhoneNumber = registrationRequest.PhoneNumber
            };

            var expectedResult = new AuthServiceResult
            {
                ErrorCode = AuthErrorCode.InvalidCredentials,
                Errors = new List<string> { "User creation failed" }
            };

            _userManagerMock.Setup(um => um.CreateAsync(It.IsAny<ApplicationUser>(), registrationRequest.Password))
                            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "User creation failed" }));

            _roleManagerMock.Setup(rm => rm.RoleExistsAsync(It.IsAny<string>()));
            _userManagerMock.Setup(um => um.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()));

            Func<string, string> emailMessageBuilder = token => $"Confirmation message with token: {token}";

            // Act
            var result = await _authService.Register(registrationRequest, emailMessageBuilder);

            // Assert
            result.Should().BeEquivalentTo(expectedResult);

            _userManagerMock.Verify(um => um.CreateAsync(It.IsAny<ApplicationUser>(), registrationRequest.Password), Times.Once);
            _roleManagerMock.Verify(rm => rm.RoleExistsAsync(It.IsAny<string>()), Times.Never);
            _userManagerMock.Verify(um => um.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task Register_ShouldReturnAuthServiceResultWithErrorCode_WhenUserAlreadyExist()
        {
            // Arrange
            var registrationRequest = new RegistrationRequestDto
            {
                Email = "alreadyExistUser@test.com",
                Password = "password",
                Name = "Test User",
                PhoneNumber = "123456789"
            };

            var applicationUser = new ApplicationUser
            {
                UserName = registrationRequest.Email,
                Email = registrationRequest.Email,
                NormalizedEmail = registrationRequest.Email.ToUpper(),
                Name = registrationRequest.Name.ToLower(),
                PhoneNumber = registrationRequest.PhoneNumber
            };

            var expectedResult = new AuthServiceResult
            {
                ErrorCode = AuthErrorCode.UserAlreadyExists,
                Errors = new List<string> { "User with this email already exists." }
            };

            _userManagerMock.Setup(um => um.FindByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync(applicationUser);

            Func<string, string> emailMessageBuilder = token => $"Confirmation message with token: {token}";

            // Act
            var result = await _authService.Register(registrationRequest, emailMessageBuilder);

            // Assert
            result.Should().BeEquivalentTo(expectedResult);

            _userManagerMock.Verify(um => um.FindByEmailAsync(It.IsAny<string>()), Times.Once);
            _userManagerMock.Verify(um => um.CreateAsync(It.IsAny<ApplicationUser>(), registrationRequest.Password), Times.Never);
            _roleManagerMock.Verify(rm => rm.RoleExistsAsync(It.IsAny<string>()), Times.Never);
            _userManagerMock.Verify(um => um.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task SendEmailConfirmationAsync_ShouldReturnAuthServiceResultWithErrorCode_WhenUserNotFound()
        {
            // Arrange
            var expectedResult = new AuthServiceResult
            {
                ErrorCode = AuthErrorCode.InvalidUser,
            };

            _userManagerMock.Setup(um => um.FindByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync((ApplicationUser)null);

            var email = "test@test.com";

            Func<string, string> emailMessageBuilder = token => $"Confirmation message with token: {token}";

            // Act
            var result = await _authService.SendEmailConfirmationAsync(email, emailMessageBuilder);

            result.Should().BeEquivalentTo(expectedResult);

            _userManagerMock.Verify(um => um.FindByEmailAsync(email), Times.Once);
            _emailSenderMock.Verify(es => es.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task SendEmailConfirmationAsync_ShouldReturnAuthServiceResultWIthErrorCode_WhenEmailIsAlreadyConfirmed()
        {
            // Arrange
            var applicationUser = new ApplicationUser
            {
                Email = "test@test.com",
                Id = "user-id"
            };

            var expectedResult = new AuthServiceResult
            {
                ErrorCode = AuthErrorCode.EmailAlreadyConfirmed,
            };

            _userManagerMock.Setup(um => um.FindByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync(applicationUser);

            _userManagerMock.Setup(um => um.IsEmailConfirmedAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(true);

            var email = "test@test.com";
            Func<string, string> emailMessageBuilder = token => $"Confirmation message with token: {token}";

            // Act
            var result = await _authService.SendEmailConfirmationAsync(email, emailMessageBuilder);

            // Assert
            result.Should().BeEquivalentTo(expectedResult);

            _userManagerMock.Verify(um => um.FindByEmailAsync(email), Times.Once);
            _userManagerMock.Verify(um => um.IsEmailConfirmedAsync(applicationUser), Times.Once);
            _emailSenderMock.Verify(es => es.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task SendEmailConfirmationAsync_ShouldReturnAuthServiceResultWithSuccess_WhenUserExistAndEmailIsNotConfirmed()
        {
            // Arrange
            var applicationUser = new ApplicationUser
            {
                Email = "test@test.com",
                Id = "user-id"
            };

            var expectedResult = new AuthServiceResult
            {
                ErrorCode = AuthErrorCode.Success,
            };

            _userManagerMock.Setup(um => um.FindByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync(applicationUser);

            _userManagerMock.Setup(um => um.IsEmailConfirmedAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(false);

            _userManagerMock.Setup(um => um.GenerateEmailConfirmationTokenAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync("fake-confirmation-token");

            _jwtTokenGeneratorMock.Setup(j => j.GenerateEmailConfirmationTokenAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns("fake-jwt-token");

            _emailSenderMock.Setup(es => es.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            Func<string, string> emailMessageBuilder = token => $"Confirmation message with token: {token}";

            var email = "test@test.com";

            // Act
            var result = await _authService.SendEmailConfirmationAsync(email, emailMessageBuilder);

            // Assert
            result.Should().BeEquivalentTo(expectedResult);

            _userManagerMock.Verify(um => um.FindByEmailAsync(email), Times.Once);
            _userManagerMock.Verify(um => um.IsEmailConfirmedAsync(applicationUser), Times.Once);
            _userManagerMock.Verify(um => um.GenerateEmailConfirmationTokenAsync(applicationUser), Times.Once);
            _jwtTokenGeneratorMock.Verify(j => j.GenerateEmailConfirmationTokenAsync(applicationUser.Id, "fake-confirmation-token"), Times.Once);
            _emailSenderMock.Verify(es => es.SendEmailAsync(applicationUser.Email, "Confirm your email",
                It.Is<string>(s => s.Contains("fake-jwt-token"))), Times.Once);
        }

        [Test]
        public async Task ConfirmEmailAsync_ShouldReturnAuthServiceResultWithErrorCode_WhenTokenIsInvalid()
        {
            // Arrange
            _jwtTokenValidatorMock.Setup(j => j.ValidateEmailConfirmationJwt(It.IsAny<string>()))
                .Returns((null, null)).Verifiable(Times.Once);

            var expectedResult = new AuthServiceResult
            {
                ErrorCode = AuthErrorCode.InvalidToken,
            };

            // Act
            var result = await _authService.ConfirmEmailAsync("invalid-token");

            // Assert
            result.Should().BeEquivalentTo(expectedResult);
            _jwtTokenValidatorMock.Verify();
        }

        [Test]
        public async Task ConfirmEmailAsync_ShouldReturnAuthServiceResultWithErrorCode_WhenUserDoesNotExist()
        {
            // Arrange
            _jwtTokenValidatorMock.Setup(j => j.ValidateEmailConfirmationJwt(It.IsAny<string>()))
                .Returns(("valid-user-id", "valid-email-token")).Verifiable(Times.Once);

            _userManagerMock.Setup(um => um.FindByIdAsync("valid-user-id"))
                .ReturnsAsync((ApplicationUser)null).Verifiable(Times.Once);

            var expectedResult = new AuthServiceResult
            {
                ErrorCode = AuthErrorCode.InvalidCredentials,
            };

            // Act
            var result = await _authService.ConfirmEmailAsync("valid-token");

            // Assert
            result.Should().BeEquivalentTo(expectedResult);
            _jwtTokenValidatorMock.Verify();
            _userManagerMock.Verify();
        }

        [Test]
        public async Task ConfirmEmailAsync_ShouldReturnAuthServiceResultWithErrorCode_WhenEmailAlreadyConfirmed()
        {
            // Arrange
            var appUser = new ApplicationUser
            {
                Name = "Test User",
                Email = "confirmed@gmail.com",
                PhoneNumber = "+123456789",
                UserName = "confirmed@gmail.com",
                EmailConfirmed = true,
            };

            _jwtTokenValidatorMock.Setup(j => j.ValidateEmailConfirmationJwt(It.IsAny<string>()))
                .Returns(("valid-user-id", "valid-email-token")).Verifiable(Times.Once);

            _userManagerMock.Setup(um => um.FindByIdAsync("valid-user-id"))
                .ReturnsAsync(appUser);


            var expectedResult = new AuthServiceResult
            {
                ErrorCode = AuthErrorCode.EmailAlreadyConfirmed,
                Errors = new List<string> { "Email has already been confirmed." }
            };

            // Act
            var result = await _authService.ConfirmEmailAsync("valid-token");

            // Assert
            result.Should().BeEquivalentTo(expectedResult);
            _jwtTokenValidatorMock.Verify();
            _userManagerMock.Verify(um => um.FindByIdAsync("valid-user-id"), Times.Once);
            _userManagerMock.Verify(um => um.ConfirmEmailAsync(appUser, It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task ConfirmEmailAsync_ShouldReturnAuthServiceResultWithSuccess_WhenEmailConfirmed()
        {
            // Arrange
            var user = new ApplicationUser { Id = "valid-user-id", Email = "test@test.com" };

            var expectedResult = new AuthServiceResult
            {
                ErrorCode = AuthErrorCode.Success,
                Token = "fake-jwt-token"
            };

            _jwtTokenValidatorMock.Setup(j => j.ValidateEmailConfirmationJwt(It.IsAny<string>()))
                .Returns(("valid-user-id", "valid-email-token")).Verifiable(Times.Once);

            _jwtTokenGeneratorMock.Setup(j => j.GenerateToken(
                It.IsAny<ApplicationUser>(),
                It.IsAny<IEnumerable<string>>()))
                    .Returns("fake-jwt-token").Verifiable(Times.Once);

            _userManagerMock.Setup(um => um.FindByIdAsync("valid-user-id"))
                .ReturnsAsync(user).Verifiable(Times.Once);

            _userManagerMock.Setup(um => um.ConfirmEmailAsync(user, "valid-email-token"))
                .ReturnsAsync(IdentityResult.Success).Verifiable(Times.Once);

            // Act
            var result = await _authService.ConfirmEmailAsync("valid-token");

            // Assert
            result.Should().BeEquivalentTo(expectedResult);
            _jwtTokenValidatorMock.Verify();
            _jwtTokenGeneratorMock.Verify();
            _userManagerMock.Verify(um => um.FindByIdAsync("valid-user-id"));
            _userManagerMock.Verify(um => um.ConfirmEmailAsync(user, "valid-email-token"));
        }

        [Test]
        public async Task ConfirmEmailAsync_ShouldReturnAuthServiceResultWithErrorCode_WhenEmailConfirmationFails()
        {
            // Arrange
            var user = new ApplicationUser { Id = "valid-user-id", Email = "test@test.com" };

            var expectedResult = new AuthServiceResult
            {
                ErrorCode = AuthErrorCode.InvalidToken,
                Errors = new List<string>()
            };

            _jwtTokenValidatorMock.Setup(j => j.ValidateEmailConfirmationJwt(It.IsAny<string>()))
                .Returns(("valid-user-id", "valid-email-token")).Verifiable(Times.Once);

            _userManagerMock.Setup(um => um.FindByIdAsync("valid-user-id"))
                .ReturnsAsync(user).Verifiable(Times.Once);

            _userManagerMock.Setup(um => um.ConfirmEmailAsync(user, "valid-email-token"))
                .ReturnsAsync(IdentityResult.Failed()).Verifiable(Times.Once);

            // Act
            var result = await _authService.ConfirmEmailAsync("valid-token");

            // Assert
            result.Should().BeEquivalentTo(expectedResult);
            _jwtTokenValidatorMock.Verify();
            _userManagerMock.Verify(um => um.FindByIdAsync("valid-user-id"));
            _userManagerMock.Verify(um => um.ConfirmEmailAsync(user, "valid-email-token"));
        }

        [Test]
        public async Task Login_ShouldReturnAuthServiceResultWithErrorCode_WhenEmailIsInvalid()
        {
            // Arrange
            var user = new ApplicationUser { UserName = "testuser", Email = "test@test.com", Id = Guid.NewGuid().ToString() };

            var loginRequest = new LoginRequestDto
            {
                Email = "test@gmail.com",
                Password = "password"
            };

            _userManagerMock.Setup(um => um.FindByEmailAsync(loginRequest.Email.ToLower()))
                            .ReturnsAsync((ApplicationUser)null);

            var expectedResult = new AuthServiceResult
            {
                ErrorCode = AuthErrorCode.InvalidUser,
            };

            // Act
            var result = await _authService.Login(loginRequest);

            // Assert
            result.Should().BeEquivalentTo(expectedResult);
            _userManagerMock.Verify(x => x.FindByEmailAsync(loginRequest.Email.ToLower()), Times.Once);
            _userManagerMock.Verify(x => x.CheckPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
            _userManagerMock.Verify(x => x.GetRolesAsync(It.IsAny<ApplicationUser>()), Times.Never);
            _jwtTokenGeneratorMock.Verify(x => x.GenerateToken(It.IsAny<ApplicationUser>(), It.IsAny<IEnumerable<string>>()), Times.Never);
        }

        [Test]
        public async Task Login_ShouldReturnResponseDtoWithErrorMessage_WhenEmailNotConfirmed()
        {
            // Arrange
            var user = new ApplicationUser
            {
                UserName = "testuser",
                Email = "test@test.com",
                Id = Guid.NewGuid().ToString(),
                EmailConfirmed = false
            };

            var loginRequest = new LoginRequestDto
            {
                Email = "testuser@gmail.com",
                Password = "wrongpassword"
            };

            _userManagerMock.Setup(um => um.FindByEmailAsync(loginRequest.Email.ToLower()))
                            .ReturnsAsync(user);

            _userManagerMock.Setup(um => um.CheckPasswordAsync(user, loginRequest.Password))
                    .ReturnsAsync(true);

            var expectedResult = new AuthServiceResult
            {
                ErrorCode = AuthErrorCode.EmailNotConfirmed,
            };

            // Act
            var result = await _authService.Login(loginRequest);

            // Assert
            result.Should().BeEquivalentTo(expectedResult);
            _userManagerMock.Verify(x => x.FindByEmailAsync(loginRequest.Email.ToLower()), Times.Once);
            _userManagerMock.Verify(x => x.CheckPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
            _userManagerMock.Verify(x => x.GetRolesAsync(It.IsAny<ApplicationUser>()), Times.Never);
            _jwtTokenGeneratorMock.Verify(x => x.GenerateToken(It.IsAny<ApplicationUser>(), It.IsAny<IEnumerable<string>>()), Times.Never);
        }

        [Test]
        public async Task Login_ShouldReturnResponseDtoWithErrorMessage_WhenPasswordIsInvalid()
        {
            // Arrange
            var user = new ApplicationUser
            {
                UserName = "testuser",
                Email = "test@test.com",
                Id = Guid.NewGuid().ToString(),
                EmailConfirmed = true
            };

            var loginRequest = new LoginRequestDto
            {
                Email = "testuser@gmail.com",
                Password = "wrongpassword"
            };

            var expectedResult = new AuthServiceResult
            {
                ErrorCode = AuthErrorCode.InvalidEmailOrPassword,
            };

            _userManagerMock.Setup(um => um.FindByEmailAsync(loginRequest.Email.ToLower()))
                            .ReturnsAsync(user);
                       
            _userManagerMock.Setup(um => um.CheckPasswordAsync(user, loginRequest.Password))
                    .ReturnsAsync(false);

            // Act
            var result = await _authService.Login(loginRequest);

            // Assert
            result.Should().BeEquivalentTo(expectedResult);
            _userManagerMock.Verify(x => x.FindByEmailAsync(loginRequest.Email.ToLower()), Times.Once);
            _userManagerMock.Verify(x => x.CheckPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Once);
            _userManagerMock.Verify(x => x.GetRolesAsync(It.IsAny<ApplicationUser>()), Times.Never);
            _jwtTokenGeneratorMock.Verify(x => x.GenerateToken(It.IsAny<ApplicationUser>(), It.IsAny<IEnumerable<string>>()), Times.Never);
        }

        [Test]
        public async Task Login_ShouldReturnAuthServiceResultWithToken_WhenCredentialsAreValid()
        {
            // Arrange
            var user = new ApplicationUser { UserName = "testuser", Email = "test@test.com", Id = Guid.NewGuid().ToString(), EmailConfirmed = true };

            var loginRequest = new LoginRequestDto
            {
                Email = "testuser@gmail.com",
                Password = "Password123*"
            };

            var expectedResult = new AuthServiceResult
            {
                ErrorCode = AuthErrorCode.Success,
                Token = "fake-jwt-token"
            };

            _userManagerMock.Setup(um => um.FindByEmailAsync(loginRequest.Email.ToLower()))
                            .ReturnsAsync(user);

            _userManagerMock.Setup(um => um.CheckPasswordAsync(user, loginRequest.Password)).ReturnsAsync(true);
            _userManagerMock.Setup(um => um.GetRolesAsync(user)).ReturnsAsync(new List<string> { "User" });

            _jwtTokenGeneratorMock.Setup(j => j.GenerateToken(It.IsAny<ApplicationUser>(), It.IsAny<IEnumerable<string>>()))
                .Returns("fake-jwt-token");

            // Act
            var result = await _authService.Login(loginRequest);

            // Assert
            result.Should().BeEquivalentTo(expectedResult);
            _userManagerMock.Verify(x => x.FindByEmailAsync(loginRequest.Email.ToLower()), Times.Once);
            _userManagerMock.Verify(x => x.CheckPasswordAsync(user, loginRequest.Password), Times.Once);
            _userManagerMock.Verify(x => x.GetRolesAsync(user), Times.Once);
            _jwtTokenGeneratorMock.Verify(x => x.GenerateToken(It.IsAny<ApplicationUser>(), It.IsAny<IEnumerable<string>>()), Times.Once);
        }

        [Test]
        public async Task AssignRole_ShouldReturnAuthServiceResultWithSuccess_WhenUserExistsAndRoleExists()
        {
            // Arrange
            var userId = "user-id";
            var roleName = "Admin";
            var applicationUser = new ApplicationUser { Id = userId, UserName = "test@test.com" };

            var expectedResult = new AuthServiceResult
            {
                ErrorCode = AuthErrorCode.Success,
            };

            _userManagerMock.Setup(um => um.FindByIdAsync(userId))
                .ReturnsAsync(applicationUser);

            _roleManagerMock.Setup(rm => rm.RoleExistsAsync(roleName))
                .ReturnsAsync(true);

            _userManagerMock.Setup(um => um.AddToRoleAsync(applicationUser, roleName.ToUpper()))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _authService.AssignRole(userId, roleName);

            // Assert
            result.Should().BeEquivalentTo(expectedResult);

            _userManagerMock.Verify(um => um.FindByIdAsync(userId), Times.Once);
            _roleManagerMock.Verify(rm => rm.RoleExistsAsync(roleName), Times.Once);
            _userManagerMock.Verify(um => um.AddToRoleAsync(applicationUser, roleName.ToUpper()), Times.Once);
        }

        [Test]
        public async Task AssignRole_ShouldReturnAuthServiceResultWithSuccess_WhenRoleDoesNotExist()
        {
            // Arrange
            var userId = "user-id";
            var roleName = "NewRole";
            var applicationUser = new ApplicationUser { Id = userId, UserName = "test@test.com" };

            var expectedResult = new AuthServiceResult
            {
                ErrorCode = AuthErrorCode.Success,
            };

            _userManagerMock.Setup(um => um.FindByIdAsync(userId))
                .ReturnsAsync(applicationUser);

            _roleManagerMock.Setup(rm => rm.RoleExistsAsync(roleName))
                .ReturnsAsync(false);

            _roleManagerMock.Setup(rm => rm.CreateAsync(It.IsAny<IdentityRole>()))
                .ReturnsAsync(IdentityResult.Success);

            _userManagerMock.Setup(um => um.AddToRoleAsync(applicationUser, roleName.ToUpper()))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _authService.AssignRole(userId, roleName);

            // Assert
            result.Should().BeEquivalentTo(expectedResult);

            _userManagerMock.Verify(um => um.FindByIdAsync(userId), Times.Once);
            _roleManagerMock.Verify(rm => rm.RoleExistsAsync(roleName), Times.Once);
            _roleManagerMock.Verify(rm => rm.CreateAsync(It.IsAny<IdentityRole>()), Times.Once);
            _userManagerMock.Verify(um => um.AddToRoleAsync(applicationUser, roleName.ToUpper()), Times.Once);
        }

        [Test]
        public async Task AssignRole_ShouldReturnAuthServiceResultWithErrorCode_WhenUserNotFound()
        {
            // Arrange
            var userId = "user-id";
            var roleName = "Admin";

            var expectedResult = new AuthServiceResult
            {
                ErrorCode = AuthErrorCode.InvalidUser,
            };

            _userManagerMock.Setup(um => um.FindByIdAsync(userId))
                .ReturnsAsync((ApplicationUser)null);

            // Act
            var result = await _authService.AssignRole(userId, roleName);

            // Assert
            result.Should().BeEquivalentTo(expectedResult);

            _userManagerMock.Verify(um => um.FindByIdAsync(userId), Times.Once);
            _roleManagerMock.Verify(rm => rm.RoleExistsAsync(It.IsAny<string>()), Times.Never);
            _roleManagerMock.Verify(rm => rm.CreateAsync(It.IsAny<IdentityRole>()), Times.Never);
            _userManagerMock.Verify(um => um.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task AssignRole_ShouldReturnAuthServiceResultWithConflict_WhenAddingRoleFails()
        {
            // Arrange
            var userId = "user-id";
            var roleName = "ADMIN";
            var applicationUser = new ApplicationUser { Id = userId, UserName = "test@test.com" };

            var expectedResult = new AuthServiceResult
            {
                ErrorCode = AuthErrorCode.Conflict,
                Errors = new List<string> { "Failed to add role" }
            };

            _userManagerMock.Setup(um => um.FindByIdAsync(userId))
                .ReturnsAsync(applicationUser);

            _roleManagerMock.Setup(rm => rm.RoleExistsAsync(roleName))
                .ReturnsAsync(true);

            _userManagerMock.Setup(um => um.AddToRoleAsync(applicationUser, roleName.ToUpper()))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Failed to add role" }));

            // Act
            var result = await _authService.AssignRole(userId, roleName);

            // Assert
            result.Should().BeEquivalentTo(expectedResult);

            _userManagerMock.Verify(um => um.FindByIdAsync(userId), Times.Once);
            _roleManagerMock.Verify(rm => rm.RoleExistsAsync(roleName), Times.Once);
            _userManagerMock.Verify(um => um.AddToRoleAsync(applicationUser, roleName.ToUpper()), Times.Once);
        }

        [Test]
        public async Task GeneratePasswordResetTokenAsync_ShouldReturnAuthServiceResultWithErrorCode_WhenUserNotFound()
        {
            // Arrange
            var email = "test@test.com";

            var expectedResult = new AuthServiceResult
            {
                ErrorCode = AuthErrorCode.InvalidCredentials,
            };

            _userManagerMock.Setup(um => um.FindByEmailAsync(email))
                            .ReturnsAsync((ApplicationUser)null).Verifiable(Times.Once);

            // Act
            var result = await _authService.GeneratePasswordResetTokenAsync(email);

            // Assert
            result.Should().BeEquivalentTo(expectedResult);

            _userManagerMock.Verify(um => um.FindByEmailAsync(email), Times.Once);
            _userManagerMock.Verify(um => um.GeneratePasswordResetTokenAsync(It.IsAny<ApplicationUser>()), Times.Never);
        }

        [Test]
        public async Task GeneratePasswordResetTokenAsync_ShouldReturnAuthServiceWithSuccess_WhenUserExists()
        {
            // Arrange
            var email = "test@test.com";

            var applicationUser = new ApplicationUser { Email = email };

            var token = "resetToken123";

            var expectedResult = new AuthServiceResult
            {
                ErrorCode = AuthErrorCode.Success,
            };

            _userManagerMock.Setup(um => um.FindByEmailAsync(email))
                .ReturnsAsync(applicationUser);

            _userManagerMock.Setup(um => um.GeneratePasswordResetTokenAsync(applicationUser))
                .ReturnsAsync(token);

            _emailSenderMock.Setup(em => em.SendEmailAsync(email, "Password Reset", It.Is<string>(s => s.Contains(token))))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _authService.GeneratePasswordResetTokenAsync(email);

            // Assert
            result.Should().BeEquivalentTo(expectedResult);

            _userManagerMock.Verify(um => um.FindByEmailAsync(email), Times.Once);
            _userManagerMock.Verify(um => um.GeneratePasswordResetTokenAsync(applicationUser), Times.Once);
            _emailSenderMock.Verify(em => em.SendEmailAsync(email, "Password Reset", It.Is<string>(s => s.Contains(token))), Times.Once);
        }

        [Test]
        public async Task ResetPasswordAsync_ShouldReturnAuthServiceResultWithErrorCode_WhenUserNotFound()
        {
            // Arrange
            var email = "test@test.com";
            var token = "someToken";
            var newPassword = "NewPassword123";

            var expectedResult = new AuthServiceResult
            {
                ErrorCode = AuthErrorCode.InvalidCredentials,
            };

            _userManagerMock.Setup(um => um.FindByEmailAsync(email))
                            .ReturnsAsync((ApplicationUser)null);

            // Act
            var result = await _authService.ResetPasswordAsync(email, token, newPassword);

            // Assert
            result.Should().BeEquivalentTo(expectedResult);

            _userManagerMock.Verify(um => um.FindByEmailAsync(email), Times.Once);
            _userManagerMock.Verify(um => um.ResetPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task ResetPasswordAsync_ShouldReturnAuthServiceResultWithErrorCode_WhenResetPasswordFails()
        {
            // Arrange
            var email = "test@test.com";
            var token = "someToken";
            var newPassword = "NewPassword123";

            var applicationUser = new ApplicationUser { Email = email };

            var expectedResult = new AuthServiceResult
            {
                ErrorCode = AuthErrorCode.InvalidCredentials,
                Errors = new List<string> { "Reset Failed" }
            };

            _userManagerMock.Setup(um => um.FindByEmailAsync(email))
                .ReturnsAsync(applicationUser);

            _userManagerMock.Setup(um => um.ResetPasswordAsync(applicationUser, token, newPassword))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Reset Failed" }));

            // Act
            var result = await _authService.ResetPasswordAsync(email, token, newPassword);

            // Assert
            result.Should().BeEquivalentTo(expectedResult);

            _userManagerMock.Verify(um => um.FindByEmailAsync(email), Times.Once);
            _userManagerMock.Verify(um => um.ResetPasswordAsync(applicationUser, token, newPassword), Times.Once);
        }

        [Test]
        public async Task ResetPasswordAsync_ShouldReturnAuthServiceResultWithSuccess_WhenResetComplete()
        {
            // Arrange
            var email = "test@test.com";
            var token = "someToken";
            var newPassword = "NewPassword123";

            var applicationUser = new ApplicationUser { Email = email };

            var expectedResult = new AuthServiceResult
            {
                ErrorCode = AuthErrorCode.Success,
            };

            _userManagerMock.Setup(um => um.FindByEmailAsync(email))
                .ReturnsAsync(applicationUser);

            _userManagerMock.Setup(um => um.ResetPasswordAsync(applicationUser, token, newPassword))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _authService.ResetPasswordAsync(email, token, newPassword);

            // Assert
            result.Should().BeEquivalentTo(expectedResult);

            _userManagerMock.Verify(um => um.FindByEmailAsync(email), Times.Once);
            _userManagerMock.Verify(um => um.ResetPasswordAsync(applicationUser, token, newPassword), Times.Once);
        }

        [Test]
        public async Task UpdateUserAsync_ShouldReturnAuthServiceResultWithErrorCode_WhenUserNotFound()
        {
            // Arrange
            var userId = "non-existent-user-id";
            var updateUserDto = new UpdateUserDto
            {
                PhoneNumber = "+123456789",
                Name = "New Name"
            };

            var expectedResult = new AuthServiceResult
            {
                ErrorCode = AuthErrorCode.InvalidUser,
            };

            _userManagerMock.Setup(um => um.FindByIdAsync(It.IsAny<string>()))
                .ReturnsAsync((ApplicationUser)null);

            // Act
            var result = await _authService.UpdateUserAsync(userId, updateUserDto);

            // Assert
            result.Should().BeEquivalentTo(expectedResult);

            _userManagerMock.Verify(um => um.FindByIdAsync(userId), Times.Once);
            _userManagerMock.Verify(um => um.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
        }

        [Test]
        public async Task UpdateUserAsync_ShouldReturnAuthServiceResultWithSuccess_WhenUpdateIsSuccessful()
        {
            // Arrange
            var userId = "existing-user-id";
            var updateUserDto = new UpdateUserDto
            {
                PhoneNumber = "+123456789",
                Name = "New Name"
            };

            var applicationUser = new ApplicationUser
            {
                Id = userId,
                PhoneNumber = "+987654321",
                Name = "Old Name"
            };

            var expectedResult = new AuthServiceResult
            {
                ErrorCode = AuthErrorCode.Success,
            };

            _userManagerMock.Setup(um => um.FindByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(applicationUser);

            _userManagerMock.Setup(um => um.UpdateAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _authService.UpdateUserAsync(userId, updateUserDto);

            // Assert
            result.Should().BeEquivalentTo(expectedResult);

            applicationUser.PhoneNumber.Should().Be(updateUserDto.PhoneNumber);
            applicationUser.Name.Should().Be(updateUserDto.Name);

            _userManagerMock.Verify(um => um.FindByIdAsync(userId), Times.Once);
            _userManagerMock.Verify(um => um.UpdateAsync(applicationUser), Times.Once);
        }

        [Test]
        public async Task UpdateUserAsync_ShouldReturnAuthServiceResultWithErrorCode_WhenUpdateFails()
        {
            // Arrange
            var userId = "existing-user-id";
            var updateUserDto = new UpdateUserDto
            {
                PhoneNumber = "+123456789",
                Name = "New Name"
            };

            var applicationUser = new ApplicationUser
            {
                Id = userId,
                PhoneNumber = "+987654321",
                Name = "Old Name"
            };

            var expectedResult = new AuthServiceResult
            {
                ErrorCode = AuthErrorCode.InvalidData,
                Errors = new List<string> { "Update failed" }
            };

            _userManagerMock.Setup(um => um.FindByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(applicationUser);

            _userManagerMock.Setup(um => um.UpdateAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Update failed" }));

            // Act
            var result = await _authService.UpdateUserAsync(userId, updateUserDto);

            // Assert
            result.Should().BeEquivalentTo(expectedResult);

            _userManagerMock.Verify(um => um.FindByIdAsync(userId), Times.Once);
            _userManagerMock.Verify(um => um.UpdateAsync(applicationUser), Times.Once);
        }

        [Test]
        public async Task GetUserByEmailAsync_ShouldReturnAuthServiceResultWithSuccess_WhenUserExists()
        {
            // Arrange
            var email = "user1@test.com";
            var user = new ApplicationUser
            {
                Id = "1",
                Name = "User1",
                Email = email,
                PhoneNumber = "+1234545513"
            };

            _userManagerMock.Setup(m => m.FindByEmailAsync(email)).ReturnsAsync(user).Verifiable(Times.Once);

            var expectedResult = new AuthServiceResult
            {
                ErrorCode = AuthErrorCode.Success,
                Result = new UserDto
                {
                    Id = user.Id,
                    Name = user.Name,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber
                }
            };

            // Act
            var result = await _authService.GetUserByEmailAsync(email);

            // Assert
            result.Should().BeEquivalentTo(expectedResult);

            _userManagerMock.Verify();
        }

        [Test]
        public async Task GetUserByEmailAsync_ShouldReturnAuthServiceResultWithErrorCode_WhenUserDoesNotExist()
        {
            // Arrange
            var email = "nonexistent@test.com";

            var expectedResult = new AuthServiceResult
            {
                ErrorCode = AuthErrorCode.InvalidUser,
            };

            _userManagerMock.Setup(m => m.FindByEmailAsync(email)).ReturnsAsync((ApplicationUser)null).Verifiable(Times.Once);

            // Act
            var result = await _authService.GetUserByEmailAsync(email);

            // Assert
            result.Should().BeEquivalentTo(expectedResult);

            _userManagerMock.Verify();
        }

        [Test]
        public async Task DeleteUserAsync_ShouldReturnAuthServiceResultWithSuccess_WhenUserIsDeleted()
        {
            // Arrange
            var userId = "1";
            var user = new ApplicationUser { Id = userId, UserName = "User1", Email = "user1@test.com" };

            var expectedResult = new AuthServiceResult
            {
                ErrorCode = AuthErrorCode.Success,
            };

            _userManagerMock.Setup(m => m.FindByIdAsync(userId)).ReturnsAsync(user);

            _userManagerMock.Setup(m => m.DeleteAsync(user)).ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _authService.DeleteUserAsync(userId);

            // Assert
            result.Should().BeEquivalentTo(expectedResult);

            _userManagerMock.Verify(um => um.FindByIdAsync(userId), Times.Once);
            _userManagerMock.Verify(um => um.DeleteAsync(user), Times.Once);
        }

        [Test]
        public async Task DeleteUserAsync_ShouldReturnAuthServiceResultWithErrorCode_WhenUserDoesNotExist()
        {
            // Arrange
            var userId = "nonexistentUserId";

            var expectedResult = new AuthServiceResult
            {
                ErrorCode = AuthErrorCode.NoUsersFound,
            };

            _userManagerMock.Setup(m => m.FindByIdAsync(userId)).ReturnsAsync((ApplicationUser)null).Verifiable(Times.Once);

            // Act
            var result = await _authService.DeleteUserAsync(userId);

            // Assert
            result.Should().BeEquivalentTo(expectedResult);

            _userManagerMock.Verify();
        }

        [Test]
        public async Task DeleteUserAsync_ShouldReturnAuthServiceResultErrorCode_WhenDeletionFails()
        {
            // Arrange
            var userId = "1";
            var user = new ApplicationUser { Id = userId, UserName = "User1", Email = "user1@test.com" };

            var expectedResult = new AuthServiceResult
            {
                ErrorCode = AuthErrorCode.Conflict,
                Errors = new List<string> { "Deletion failed" }
            };
            var identityErrors =  new IdentityError { Description = "Deletion failed" };
            _userManagerMock.Setup(m => m.FindByIdAsync(userId)).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.DeleteAsync(user)).ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Deletion failed" }));

            // Act
            var result = await _authService.DeleteUserAsync(userId);

            // Assert
            result.Should().BeEquivalentTo(expectedResult);

            _userManagerMock.Verify(um => um.FindByIdAsync(userId), Times.Once);
            _userManagerMock.Verify(um => um.DeleteAsync(user), Times.Once);
        }
    }
}
