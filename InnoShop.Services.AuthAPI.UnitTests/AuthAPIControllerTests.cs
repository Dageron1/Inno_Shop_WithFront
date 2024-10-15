using InnoShop.Services.AuthAPI.Controllers;
using InnoShop.Services.AuthAPI.Models.Dto;
using InnoShop.Services.AuthAPI.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Net;
using Newtonsoft.Json.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace InnoShop.Services.AuthAPI.UnitTests
{
    [TestFixture]
    public class AuthAPIControllerTests
    {
        private Mock<IAuthService> _authServiceMock;
        private AuthApiController _authAPIController;
        private Mock<HttpContext> _httpContextMock;
        private Mock<HttpRequest> _httpRequestMock;

        [SetUp]
        public void SetUp()
        {
            _authServiceMock = new Mock<IAuthService>();
            _authAPIController = new AuthApiController(_authServiceMock.Object);

            _httpContextMock = new Mock<HttpContext>();
            _httpRequestMock = new Mock<HttpRequest>();

            var httpContextMock = new Mock<HttpContext>();
            var requestMock = new Mock<HttpRequest>();
            requestMock.Setup(r => r.Scheme).Returns("https");
            requestMock.Setup(r => r.Host).Returns(new HostString("localhost", 5000));
            httpContextMock.Setup(c => c.Request).Returns(requestMock.Object);

            _authAPIController.ControllerContext = new ControllerContext
            {
                HttpContext = httpContextMock.Object
            };
        }

        [Test]
        public async Task Register_ShouldReturnOkWithResponseDto_WhenRegistrationIsSuccessful()
        {
            // Arrange
            var registrationRequest = new RegistrationRequestDto
            {
                Email = "test@test.com",
                Password = "password",
                Name = "Test User"
            };

            var authServiceResult = new AuthServiceResult
            {
                ErrorCode = AuthErrorCode.Success,
            };

            var expectedResponseDto = ConvertToResponseDto(authServiceResult);

            _authServiceMock.Setup(a => a.Register(It.IsAny<RegistrationRequestDto>(), It.IsAny<Func<string, string>>()))
                .ReturnsAsync(authServiceResult).Verifiable(Times.Once);

            // Act
            var result = await _authAPIController.Register(registrationRequest);

            // Assert
            var okObjectResult = result.Result as OkObjectResult;

            okObjectResult.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be(200);

            var response = okObjectResult.Value as ResponseDto;
            response.Should().BeEquivalentTo(expectedResponseDto);

            _authServiceMock.Verify();
        }

        [Test]
        public async Task Register_ShouldReturnBadRequestWithResponseDto_WhenRegistrationFails()
        {
            // Arrange
            var registrationRequest = new RegistrationRequestDto
            {
                Email = "test@test.com",
                Password = "password",
                Name = "Test User"
            };

            var authServiceResult = new AuthServiceResult
            {
                ErrorCode = AuthErrorCode.InvalidCredentials,
                Errors = "Errors"
            };

            var expectedResponseDto = ConvertToResponseDto(authServiceResult);

            _authServiceMock.Setup(a => a.Register(It.IsAny<RegistrationRequestDto>(), It.IsAny<Func<string, string>>()))
                .ReturnsAsync(authServiceResult).Verifiable(Times.Once);

            // Act
            var result = await _authAPIController.Register(registrationRequest);

            // Assert
            var badRequestObjectResult = result.Result as BadRequestObjectResult;

            badRequestObjectResult.Should().NotBeNull();
            badRequestObjectResult.StatusCode.Should().Be(400);

            var response = badRequestObjectResult.Value as ResponseDto;
            response.Should().BeEquivalentTo(expectedResponseDto);

            _authServiceMock.Verify();
        }

        [Test]
        public async Task Register_ShouldReturnConflictWithResponseDto_WhenUserAlreadyExist()
        {
            // Arrange
            var registrationRequest = new RegistrationRequestDto
            {
                Email = "test@test.com",
                Password = "password",
                Name = "Test User"
            };

            var authServiceResult = new AuthServiceResult
            {
                ErrorCode = AuthErrorCode.UserAlreadyExists,
                Errors = new[] { "User with this email already exists." }
            };

            var expectedResponseDto = ConvertToResponseDto(authServiceResult);

            _authServiceMock.Setup(a => a.Register(It.IsAny<RegistrationRequestDto>(), It.IsAny<Func<string, string>>()))
                .ReturnsAsync(authServiceResult).Verifiable(Times.Once);

            // Act
            var result = await _authAPIController.Register(registrationRequest);

            // Assert
            var conflictObjectResult = result.Result as ConflictObjectResult;

            conflictObjectResult.Should().NotBeNull();
            conflictObjectResult.StatusCode.Should().Be(409);

            var response = conflictObjectResult.Value as ResponseDto;
            response.Should().BeEquivalentTo(expectedResponseDto);

            _authServiceMock.Verify();
        }

        [Test]
        public async Task SendEmailConfirmation_ShouldReturnOkWithResponseDto_WhenEmailSentSuccessfully()
        {
            // Arrange
            var email = new EmailDto { Email = "test@test.com" };

            var authServiceResult = new AuthServiceResult
            {
                ErrorCode = AuthErrorCode.Success,
            };

            var expectedResponseDto = ConvertToResponseDto(authServiceResult);

            _authServiceMock.Setup(a => a.SendEmailConfirmationAsync(email.Email, It.IsAny<Func<string, string>>()))
                            .ReturnsAsync(authServiceResult).Verifiable(Times.Once);

            // Act
            var result = await _authAPIController.SendEmailConfirmation(email);

            // Assert
            var okObjectResult = result.Result as OkObjectResult;
            okObjectResult.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be(200);

            var response = okObjectResult.Value as ResponseDto;
            response.Should().BeEquivalentTo(expectedResponseDto);

            _authServiceMock.Verify();
        }

        [Test]
        public async Task SendEmailConfirmation_ShouldReturnBadRequest_WhenEmailResendFails()
        {
            // Arrange
            var email = new EmailDto { Email = "nonexistent@test.com" };

            var authServiceResult = new AuthServiceResult
            {
                ErrorCode = AuthErrorCode.InvalidUser,
            };

            var expectedResponseDto = ConvertToResponseDto(authServiceResult);

            _authServiceMock.Setup(a => a.SendEmailConfirmationAsync(email.Email, It.IsAny<Func<string, string>>()))
                            .ReturnsAsync(authServiceResult).Verifiable(Times.Once);

            // Act
            var result = await _authAPIController.SendEmailConfirmation(email);

            // Assert
            var badRequestObjectResult = result.Result as BadRequestObjectResult;
            badRequestObjectResult.Should().NotBeNull();
            badRequestObjectResult.StatusCode.Should().Be(400);

            var response = badRequestObjectResult.Value as ResponseDto;
            response.Should().BeEquivalentTo(expectedResponseDto);

            _authServiceMock.Verify();
        }

        [Test]
        public async Task SendEmailConfirmation_ShouldReturnBadRequest_WhenEmailAlreadyConfirmed()
        {
            // Arrange
            var email = new EmailDto { Email = "AlreadyConfirmed@test.com" };

            var authServiceResult = new AuthServiceResult
            {
                ErrorCode = AuthErrorCode.EmailAlreadyConfirmed,
            };

            var expectedResponseDto = ConvertToResponseDto(authServiceResult);

            _authServiceMock.Setup(a => a.SendEmailConfirmationAsync(email.Email, It.IsAny<Func<string, string>>()))
                            .ReturnsAsync(authServiceResult).Verifiable(Times.Once);

            // Act
            var result = await _authAPIController.SendEmailConfirmation(email);

            // Assert
            var badRequestObjectResult = result.Result as BadRequestObjectResult;
            badRequestObjectResult.Should().NotBeNull();
            badRequestObjectResult.StatusCode.Should().Be(400);

            var response = badRequestObjectResult.Value as ResponseDto;
            response.Should().BeEquivalentTo(expectedResponseDto);

            _authServiceMock.Verify();
        }

        [Test]
        public async Task ConfirmEmail_ShouldReturnOkWithResponseDto_WhenRegistrationIsSuccessful()
        {
            // Arrange
            var token = "email-confirmation-token";

            var authServiceResult = new AuthServiceResult
            {
                ErrorCode = AuthErrorCode.Success,
                Token = "JWT Token"
            };

            var expectedResponseDto = ConvertToResponseDto(authServiceResult);

            _authServiceMock.Setup(a => a.ConfirmEmailAsync(It.IsAny<string>()))
                .ReturnsAsync(authServiceResult).Verifiable(Times.Once);

            // Act
            var result = await _authAPIController.ConfirmEmail(token);

            // Assert
            var okObjectResult = result.Result as OkObjectResult;
            okObjectResult.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be(200);

            var response = okObjectResult.Value as ResponseDto;
            response.Should().BeEquivalentTo(expectedResponseDto);

            _authServiceMock.Verify();
        }

        [Test]
        public async Task ConfirmEmail_ShouldReturnBadRequestWithResponseDto_WhenConfirmationTokenInvalid()
        {
            // Arrange
            var token = "invalid-token";

            var authServiceResult = new AuthServiceResult
            {
                ErrorCode = AuthErrorCode.InvalidToken,
            };

            var expectedResponseDto = ConvertToResponseDto(authServiceResult);

            _authServiceMock.Setup(a => a.ConfirmEmailAsync(token))
                            .ReturnsAsync(authServiceResult).Verifiable(Times.Once);

            // Act
            var result = await _authAPIController.ConfirmEmail(token);

            // Assert
            var badRequestObjectResult = result.Result as BadRequestObjectResult;
            badRequestObjectResult.Should().NotBeNull();
            badRequestObjectResult.StatusCode.Should().Be(400);

            var response = badRequestObjectResult.Value as ResponseDto;
            response.Should().BeEquivalentTo(expectedResponseDto);
        }

        [Test]
        public async Task ConfirmEmail_ShouldReturnBadRequestWithResponseDto_WhenUserInvalid()
        {
            // Arrange
            var token = "invalid-token";

            var authServiceResult = new AuthServiceResult
            {
                ErrorCode = AuthErrorCode.InvalidCredentials,
            };

            var expectedResponseDto = ConvertToResponseDto(authServiceResult);

            _authServiceMock.Setup(a => a.ConfirmEmailAsync(token))
                            .ReturnsAsync(authServiceResult).Verifiable(Times.Once);

            // Act
            var result = await _authAPIController.ConfirmEmail(token);

            // Assert
            var badRequestObjectResult = result.Result as BadRequestObjectResult;
            badRequestObjectResult.Should().NotBeNull();
            badRequestObjectResult.StatusCode.Should().Be(400);

            var response = badRequestObjectResult.Value as ResponseDto;
            response.Should().BeEquivalentTo(expectedResponseDto);
        }

        [Test]
        public async Task ConfirmEmail_ShouldReturnBadRequestWithResponseDto_WhenEmailAlreadyConfirmed()
        {
            // Arrange
            var token = "valid-token";

            var authServiceResult = new AuthServiceResult
            {
                ErrorCode = AuthErrorCode.EmailAlreadyConfirmed,
                Errors = new List<string> { "Email has already been confirmed." } 
            };

            var expectedResponseDto = ConvertToResponseDto(authServiceResult);

            _authServiceMock.Setup(a => a.ConfirmEmailAsync(token))
                            .ReturnsAsync(authServiceResult).Verifiable(Times.Once);

            // Act
            var result = await _authAPIController.ConfirmEmail(token);

            // Assert
            var badRequestObjectResult = result.Result as BadRequestObjectResult;
            badRequestObjectResult.Should().NotBeNull();
            badRequestObjectResult.StatusCode.Should().Be(400);

            var response = badRequestObjectResult.Value as ResponseDto;
            response.Should().BeEquivalentTo(expectedResponseDto);
        }

        [Test]
        public async Task ConfirmEmail_ShouldReturnBadRequestWithResponseDto_WhenConfirmationFailed()
        {
            // Arrange
            var token = "token";

            var authServiceResult = new AuthServiceResult
            {
                ErrorCode = AuthErrorCode.InvalidToken,
                Errors = "Errors"
            };

            var expectedResponseDto = ConvertToResponseDto(authServiceResult);

            _authServiceMock.Setup(a => a.ConfirmEmailAsync(token))
                            .ReturnsAsync(authServiceResult).Verifiable(Times.Once);

            // Act
            var result = await _authAPIController.ConfirmEmail(token);

            // Assert
            var badRequestObjectResult = result.Result as BadRequestObjectResult;
            badRequestObjectResult.Should().NotBeNull();
            badRequestObjectResult.StatusCode.Should().Be(400);

            var response = badRequestObjectResult.Value as ResponseDto;
            response.Should().BeEquivalentTo(expectedResponseDto);
        }

        [Test]
        public async Task Login_ShouldReturnOkWithResponseDto_WhenLoginIsSuccessful()
        {
            // Arrange
            var token = "token";
            var loginRequest = new LoginRequestDto
            {
                Email = "test@gmail.com",
                Password = "Password123*",
            };

            var authServiceResult = new AuthServiceResult
            {
                ErrorCode = AuthErrorCode.Success,
                Token = token
            };

            var expectedResponseDto = ConvertToResponseDto(authServiceResult);

            _authServiceMock.Setup(a => a.Login(It.IsAny<LoginRequestDto>()))
                .ReturnsAsync(authServiceResult).Verifiable(Times.Once);

            // Act
            var result = await _authAPIController.Login(loginRequest);

            // Assert
            var okObjectResult = result.Result as OkObjectResult;

            okObjectResult.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be(200);

            var response = okObjectResult.Value as ResponseDto;
            response.Should().BeEquivalentTo(expectedResponseDto);

            _authServiceMock.Verify();
        }

        [Test]
        public async Task Login_ShouldReturnBadRequest_WhenUserEmailNotFound()
        {
            // Arrange
            var loginRequest = new LoginRequestDto
            {
                Email = "wrongemail",
                Password = "Password123*",
            };

            var authServiceResult = new AuthServiceResult
            {
                ErrorCode = AuthErrorCode.InvalidUser,
            };

            var expectedResponseDto = ConvertToResponseDto(authServiceResult);

            _authServiceMock.Setup(a => a.Login(It.IsAny<LoginRequestDto>()))
                .ReturnsAsync(authServiceResult).Verifiable(Times.Once);

            // Act
            var result = await _authAPIController.Login(loginRequest);

            // Assert
            var badRequestObjectResult = result.Result as BadRequestObjectResult;

            badRequestObjectResult.Should().NotBeNull();
            badRequestObjectResult.StatusCode.Should().Be(400);

            var response = badRequestObjectResult.Value as ResponseDto;
            response.Should().BeEquivalentTo(expectedResponseDto);

            _authServiceMock.Verify();
        }

        [Test]
        public async Task Login_ShouldReturnBadRequest_WhenPasswordIsIncorrect()
        {
            // Arrange
            var loginRequest = new LoginRequestDto
            {
                Email = "username@gmail.com",
                Password = "wrongpassword",
            };

            var authServiceResult = new AuthServiceResult
            {
                ErrorCode = AuthErrorCode.InvalidUser,
            };

            var expectedResponseDto = ConvertToResponseDto(authServiceResult);

            _authServiceMock.Setup(a => a.Login(It.IsAny<LoginRequestDto>()))
                .ReturnsAsync(authServiceResult).Verifiable(Times.Once);

            // Act
            var result = await _authAPIController.Login(loginRequest);

            // Assert
            var badRequestObjectResult = result.Result as BadRequestObjectResult;

            badRequestObjectResult.Should().NotBeNull();
            badRequestObjectResult.StatusCode.Should().Be(400);

            var response = badRequestObjectResult.Value as ResponseDto;
            response.Should().BeEquivalentTo(expectedResponseDto);

            _authServiceMock.Verify();
        }

        [Test]
        public async Task Login_ShouldReturnStatusCode403_WhenEmailNotConfirmed()
        {
            // Arrange
            var loginRequest = new LoginRequestDto
            {
                Email = "username@gmail.com",
                Password = "Password123*",
            };

            var authServiceResult = new AuthServiceResult
            {
                ErrorCode = AuthErrorCode.EmailNotConfirmed,
            };

            var expectedResponseDto = ConvertToResponseDto(authServiceResult);

            _authServiceMock.Setup(a => a.Login(It.IsAny<LoginRequestDto>()))
                .ReturnsAsync(authServiceResult).Verifiable(Times.Once);

            // Act
            var result = await _authAPIController.Login(loginRequest);

            // Assert
            var objectResult = result.Result as ObjectResult;

            objectResult.Should().NotBeNull();
            objectResult.StatusCode.Should().Be(403);

            var response = objectResult.Value as ResponseDto;
            response.Should().BeEquivalentTo(expectedResponseDto);

            _authServiceMock.Verify();
        }

        [Test]
        public async Task AddRoleToUser_ShouldReturnOkWithResponseDto_WhenRoleIsAdded()
        {
            var userId = Guid.NewGuid();

            var roleAddModel = new AddRoleRequestDto { Role = "User" };

            var authServiceResult = new AuthServiceResult
            {
                ErrorCode = AuthErrorCode.Success,
            };

            var expectedResponseDto = ConvertToResponseDto(authServiceResult);

            _authServiceMock.Setup(a => a.AssignRole(userId.ToString(), roleAddModel.Role.ToUpper()))
                .ReturnsAsync(authServiceResult).Verifiable(Times.Once);

            // Act
            var result = await _authAPIController.AddRoleToUser(userId, roleAddModel);

            // Assert
            var okObjectResult = result.Result as OkObjectResult;

            okObjectResult.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be(200);

            var response = okObjectResult.Value as ResponseDto;
            response.Should().BeEquivalentTo(expectedResponseDto);

            _authServiceMock.Verify();
        }

        [Test]
        public async Task AddRoleToUser_ShouldReturnConflictWithResponseDto_WhenRoleIsNotAdded()
        {
            var userId = Guid.NewGuid();

            var roleAddModel = new AddRoleRequestDto { Role = "User" };

            var authServiceResult = new AuthServiceResult
            {
                ErrorCode = AuthErrorCode.Conflict,
                Errors = "Errors"
            };

            var expectedResponseDto = ConvertToResponseDto(authServiceResult);

            _authServiceMock.Setup(a => a.AssignRole(userId.ToString(), roleAddModel.Role.ToUpper()))
                .ReturnsAsync(authServiceResult).Verifiable(Times.Once);

            // Act
            var result = await _authAPIController.AddRoleToUser(userId, roleAddModel);

            // Assert
            var objectResult = result.Result as ObjectResult;

            objectResult.Should().NotBeNull();
            objectResult.StatusCode.Should().Be(409);

            var response = objectResult.Value as ResponseDto;
            response.Should().BeEquivalentTo(expectedResponseDto);

            _authServiceMock.Verify();
        }

        [Test]
        public async Task AddRoleToUser_ShouldReturnBadRequestWithResponseDto_WhenUserNotFound()
        {
            var userId = Guid.NewGuid();

            var roleAddModel = new AddRoleRequestDto { Role = "User" };

            var authServiceResult = new AuthServiceResult
            {
                ErrorCode = AuthErrorCode.InvalidUser,
            };

            var expectedResponseDto = ConvertToResponseDto(authServiceResult);

            _authServiceMock.Setup(a => a.AssignRole(userId.ToString(), roleAddModel.Role.ToUpper()))
                .ReturnsAsync(authServiceResult).Verifiable(Times.Once);

            // Act
            var result = await _authAPIController.AddRoleToUser(userId, roleAddModel);

            // Assert
            var badRequestObjectResult = result.Result as BadRequestObjectResult;

            badRequestObjectResult.Should().NotBeNull();
            badRequestObjectResult.StatusCode.Should().Be(400);

            var response = badRequestObjectResult.Value as ResponseDto;
            response.Should().BeEquivalentTo(expectedResponseDto);

            _authServiceMock.Verify();
        }

        [Test]
        public async Task ForgotPassword_ShouldReturnOkWithResponseDto_WhenPasswordResetTokenIsSentSuccessfully()
        {
            // Arrange
            var email = new EmailDto
            {
                Email = "test@test.com",
            };

            var authServiceResult = new AuthServiceResult
            {
                ErrorCode = AuthErrorCode.Success,
            };

            var expectedResponseDto = ConvertToResponseDto(authServiceResult);

            _authServiceMock.Setup(a => a.GeneratePasswordResetTokenAsync(It.IsAny<string>()))
                .ReturnsAsync(authServiceResult).Verifiable(Times.Once);

            // Act
            var result = await _authAPIController.ForgotPassword(email);

            // Assert
            var okObjectResult = result.Result as OkObjectResult;

            okObjectResult.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be(200);

            var response = okObjectResult.Value as ResponseDto;
            response.Should().BeEquivalentTo(expectedResponseDto);

            _authServiceMock.Verify();
        }

        [Test]
        public async Task ForgotPassword_ShouldReturnBadRequestWithResponseDto_WhenPasswordResetTokenGenerationFails()
        {
            // Arrange
            var wrongEmail = new EmailDto
            {
                Email = "wrong@test.com",
            };

            var authServiceResult = new AuthServiceResult
            {
                ErrorCode = AuthErrorCode.InvalidCredentials,
            };

            var expectedResponseDto = ConvertToResponseDto(authServiceResult);

            _authServiceMock.Setup(a => a.GeneratePasswordResetTokenAsync(It.IsAny<string>()))
                .ReturnsAsync(authServiceResult).Verifiable(Times.Once);

            // Act
            var result = await _authAPIController.ForgotPassword(wrongEmail);

            // Assert
            var badRequestObjectResult = result.Result as BadRequestObjectResult;

            badRequestObjectResult.Should().NotBeNull();
            badRequestObjectResult.StatusCode.Should().Be(400);

            var response = badRequestObjectResult.Value as ResponseDto;
            response.Should().BeEquivalentTo(expectedResponseDto);

            _authServiceMock.Verify();
        }

        [Test]
        public async Task ResetPassword_ShouldReturnBadRequestWithResponseDto_WhenModelIsInvalid()
        {
            // Arrange
            var invalidRequest = new ResetPasswordRequestDto
            {
                Email = "test@test.com",
                Token = "",
                NewPassword = ""
            };

            var expectedResponse = new ResponseDto
            {
                IsSuccess = false,
                Message = "Invalid request.",
            };

            // Act
            var result = await _authAPIController.ResetPassword(invalidRequest);

            // Assert
            var badRequestObjectResult = result.Result as BadRequestObjectResult;

            badRequestObjectResult.Should().NotBeNull();
            badRequestObjectResult.StatusCode.Should().Be(400);

            var response = badRequestObjectResult.Value as ResponseDto;
            response.Should().BeEquivalentTo(expectedResponse);

            _authServiceMock.Verify();
        }

        [Test]
        public async Task ResetPassword_ShouldReturnBadRequestWithResponseDto_WhenPasswordResetFails()
        {
            // Arrange
            var invalidRequest = new ResetPasswordRequestDto
            {
                Email = "test@test.com",
                Token = "valid-token",
                NewPassword = "NewPassword123"
            };

            var authServiceResult = new AuthServiceResult
            {
                ErrorCode = AuthErrorCode.InvalidCredentials,
                Errors = "Errors"
            };

            var expectedResponseDto = ConvertToResponseDto(authServiceResult);

            _authServiceMock.Setup(a => a.ResetPasswordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(authServiceResult).Verifiable(Times.Once);

            // Act
            var result = await _authAPIController.ResetPassword(invalidRequest);

            // Assert
            var badRequestObjectResult = result.Result as BadRequestObjectResult;

            badRequestObjectResult.Should().NotBeNull();
            badRequestObjectResult.StatusCode.Should().Be(400);

            var response = badRequestObjectResult.Value as ResponseDto;
            response.Should().BeEquivalentTo(expectedResponseDto);

            _authServiceMock.Verify();
        }

        [Test]
        public async Task ResetPassword_ShouldReturnOkWithResponseDto_WhenPasswordIsResetSuccessfully()
        {
            // Arrange
            var resetPasswordRequest = new ResetPasswordRequestDto
            {
                Email = "test@test.com",
                Token = "valid-token",
                NewPassword = "NewPassword123"
            };

            var authServiceResult = new AuthServiceResult
            {
                ErrorCode = AuthErrorCode.Success,
            };

            var expectedResponseDto = ConvertToResponseDto(authServiceResult);

            _authServiceMock.Setup(a => a.ResetPasswordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(authServiceResult).Verifiable(Times.Once);

            // Act
            var result = await _authAPIController.ResetPassword(resetPasswordRequest);

            // Assert
            var okObjectResult = result.Result as OkObjectResult;

            okObjectResult.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be(200);

            var response = okObjectResult.Value as ResponseDto;
            response.Should().BeEquivalentTo(expectedResponseDto);

            _authServiceMock.Verify();
        }

        [Test]
        public async Task UpdateUser_ShouldReturnOkWithResponseDto_WhenUserTheSameUser()
        {
            // Arrange
            var userId = Guid.NewGuid();

            var model = new UpdateUserDto
            {
                Name = "New name"
            };

            var authServiceResult = new AuthServiceResult
            {
                ErrorCode = AuthErrorCode.Success,
            };

            var expectedResponseDto = ConvertToResponseDto(authServiceResult);

            _authServiceMock.Setup(a => a.UpdateUserAsync(userId.ToString(), model))
                .ReturnsAsync(authServiceResult).Verifiable(Times.Once);

            var userClaims = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            }, "mock"));

            _authAPIController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = userClaims }
            };

            // Act
            var result = await _authAPIController.UpdateUser(userId, model);

            // Assert
            var okObjectResult = result.Result as OkObjectResult;

            okObjectResult.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be(200);

            var response = okObjectResult.Value as ResponseDto;
            response.Should().BeEquivalentTo(expectedResponseDto);

            _authServiceMock.Verify();
        }

        [Test]
        public async Task UpdateUser_ShouldReturnOkWithResponseDto_WhenUserIsAdmin()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var role = "ADMIN";

            var model = new UpdateUserDto
            {
                Name = "New name"
            };

            var authServiceResult = new AuthServiceResult
            {
                ErrorCode = AuthErrorCode.Success,
            };

            var expectedResponseDto = ConvertToResponseDto(authServiceResult);

            _authServiceMock.Setup(a => a.UpdateUserAsync(userId.ToString(), model))
                .ReturnsAsync(authServiceResult).Verifiable(Times.Once);

            var userClaims = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.Role, role)
            }, "mock"));

            _authAPIController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = userClaims }
            };

            // Act
            var result = await _authAPIController.UpdateUser(userId, model);

            // Assert
            var okObjectResult = result.Result as OkObjectResult;

            okObjectResult.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be(200);

            var response = okObjectResult.Value as ResponseDto;
            response.Should().BeEquivalentTo(expectedResponseDto);

            _authServiceMock.Verify();
        }

        [Test]
        public async Task UpdateUser_ShouldReturnBadRequestWithResponseDto_WhenInvalidData()
        {
            // Arrange
            var userId = Guid.NewGuid();

            var model = new UpdateUserDto
            {
                Name = "New name"
            };

            var authServiceResult = new AuthServiceResult
            {
                ErrorCode = AuthErrorCode.InvalidData,
                Errors = "Errors"
            };

            var expectedResponseDto = ConvertToResponseDto(authServiceResult);

            _authServiceMock.Setup(a => a.UpdateUserAsync(userId.ToString(), model))
                .ReturnsAsync(authServiceResult).Verifiable(Times.Once);

            var userClaims = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            }, "mock"));

            _authAPIController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = userClaims }
            };

            // Act
            var result = await _authAPIController.UpdateUser(userId, model);

            // Assert
            var objectResult = result.Result as ObjectResult;

            objectResult.Should().NotBeNull();
            objectResult.StatusCode.Should().Be(400);

            var response = objectResult.Value as ResponseDto;
            response.Should().BeEquivalentTo(expectedResponseDto);

            _authServiceMock.Verify();
        }

        [Test]
        public async Task UpdateUser_ShouldReturnBadRequestWithResponseDto_WhenUserNotFound()
        {
            // Arrange
            var userId = Guid.NewGuid();

            var model = new UpdateUserDto
            {
                Name = "New name"
            };

            var authServiceResult = new AuthServiceResult
            {
                ErrorCode = AuthErrorCode.InvalidUser,
            };

            var expectedResponseDto = ConvertToResponseDto(authServiceResult);

            _authServiceMock.Setup(a => a.UpdateUserAsync(userId.ToString(), model))
                .ReturnsAsync(authServiceResult).Verifiable(Times.Once);

            var userClaims = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            }, "mock"));

            _authAPIController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = userClaims }
            };

            // Act
            var result = await _authAPIController.UpdateUser(userId, model);

            // Assert
            var badRequestObjectResult = result.Result as BadRequestObjectResult;

            badRequestObjectResult.Should().NotBeNull();
            badRequestObjectResult.StatusCode.Should().Be(400);

            var response = badRequestObjectResult.Value as ResponseDto;
            response.Should().BeEquivalentTo(expectedResponseDto);

            _authServiceMock.Verify();
        }

        [Test]
        public async Task UpdateUser_ShouldReturnForbid_WhenUserIsNotAdminOrNotTheSameUser()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var differentUserId = Guid.NewGuid();

            var model = new UpdateUserDto
            {
                Name = "New name"
            };

            _authServiceMock.Setup(a => a.UpdateUserAsync(It.IsAny<string>(), It.IsAny<UpdateUserDto>()))
                .ReturnsAsync(new AuthServiceResult()).Verifiable(Times.Never);

            var userClaims = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, differentUserId.ToString())
            }, "mock"));

            _authAPIController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = userClaims }
            };

            // Act
            var result = await _authAPIController.UpdateUser(userId, model);

            // Assert
            var forbidResult = result.Result as ForbidResult;

            forbidResult.Should().NotBeNull();
            _authServiceMock.Verify();
        }

        [Test]
        public async Task GetUsersWithPagination_ShouldReturnOkWithResponseDto_WhenRequestIsSuccessfulAndUserIsAdmin()
        {
            // Arrange
            var paginationParams = new PaginationParams { PageNumber = 1, PageSize = 10 };

            var authServiceResult = new AuthServiceResult
            {
                ErrorCode = AuthErrorCode.Success,
                Result = "List of users."
            };

            var expectedResponseDto = ConvertToResponseDto(authServiceResult);

            _authServiceMock.Setup(a => a.GetUsersWithPaginationAsync(paginationParams))
                .ReturnsAsync(authServiceResult).Verifiable(Times.Once);

            // Act
            var result = await _authAPIController.GetUsersWithPagination(paginationParams);

            // Assert
            var okObjectResult = result.Result as OkObjectResult;

            okObjectResult.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be(200);

            var response = okObjectResult.Value as ResponseDto;
            response.Should().BeEquivalentTo(expectedResponseDto);

            _authServiceMock.Verify();
        }

        [Test]
        public async Task GetUsersWithPagination_ShouldReturnBadRequestWithResponseDto_WhenServiceReturnsNoUsersFound()
        {
            // Arrange
            var paginationParams = new PaginationParams { PageNumber = 999, PageSize = 10 };

            var authServiceResult = new AuthServiceResult
            {
                ErrorCode = AuthErrorCode.NoUsersFound,
            };

            var expectedResponseDto = ConvertToResponseDto(authServiceResult);

            _authServiceMock.Setup(a => a.GetUsersWithPaginationAsync(paginationParams))
                .ReturnsAsync(authServiceResult).Verifiable(Times.Once);

            // Act
            var result = await _authAPIController.GetUsersWithPagination(paginationParams);

            // Assert
            var badRequestObjectResult = result.Result as BadRequestObjectResult;

            badRequestObjectResult.Should().NotBeNull();
            badRequestObjectResult.StatusCode.Should().Be(400);

            var response = badRequestObjectResult.Value as ResponseDto;
            response.Should().BeEquivalentTo(expectedResponseDto);

            _authServiceMock.Verify();
        }

        [Test]
        public async Task GetUserByEmail_ShouldReturnOkWithResponseDto_WhenUserIsFound()
        {
            // Arrange
            var emailDto = new EmailDto { Email = "test@test.com" };

            var authServiceResult = new AuthServiceResult
            {
                ErrorCode = AuthErrorCode.Success,
                Result = "User."
            };

            var expectedResponseDto = ConvertToResponseDto(authServiceResult);

            _authServiceMock.Setup(a => a.GetUserByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync(authServiceResult).Verifiable(Times.Once);

            // Act
            var result = await _authAPIController.GetUserByEmail(emailDto);

            // Assert
            var okObjectResult = result.Result as OkObjectResult;

            okObjectResult.Should().NotBeNull();
            okObjectResult.StatusCode.Should().Be(200);

            var response = okObjectResult.Value as ResponseDto;
            response.Should().BeEquivalentTo(expectedResponseDto);

            _authServiceMock.Verify();
        }

        [Test]
        public async Task GetUserByEmail_ShouldReturnBadRequestWithResponseDto_WhenUserNotFound()
        {
            // Arrange
            var emailDto = new EmailDto { Email = "test@test.com" };

            var authServiceResult = new AuthServiceResult
            {
                ErrorCode = AuthErrorCode.InvalidUser,
            };

            var expectedResponseDto = ConvertToResponseDto(authServiceResult);

            _authServiceMock.Setup(a => a.GetUserByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync(authServiceResult).Verifiable(Times.Once);

            // Act
            var result = await _authAPIController.GetUserByEmail(emailDto);

            // Assert
            var badRequestObjectResult = result.Result as BadRequestObjectResult;

            badRequestObjectResult.Should().NotBeNull();
            badRequestObjectResult.StatusCode.Should().Be(400);

            var response = badRequestObjectResult.Value as ResponseDto;
            response.Should().BeEquivalentTo(expectedResponseDto);
            _authServiceMock.Verify();
        }

        [Test]
        public async Task DeleteUser_ShouldReturnOkWithResponseDto_WhenDeletionIsSuccessful()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var authResult = new AuthServiceResult 
            { 
                ErrorCode = AuthErrorCode.Success,
            };
            var responseDto = ConvertToResponseDto(authResult);

            _authServiceMock.Setup(a => a.DeleteUserAsync(userId.ToString()))
                .ReturnsAsync(authResult).Verifiable(Times.Once);

            // Act
            var result = await _authAPIController.DeleteUser(userId);

            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be(200);

            var response = okResult.Value as ResponseDto;
            response.Should().BeEquivalentTo(responseDto);
            _authServiceMock.Verify();
        }

        [Test]
        public async Task DeleteUser_ShouldReturnBadRequest_WhenInternalErrorOccurs()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var authResult = new AuthServiceResult
            {
                ErrorCode = AuthErrorCode.Conflict,
                Errors = "Errors"
            };
            var responseDto = ConvertToResponseDto(authResult);

            _authServiceMock.Setup(a => a.DeleteUserAsync(userId.ToString()))
                .ReturnsAsync(authResult).Verifiable(Times.Once);

            // Act
            var result = await _authAPIController.DeleteUser(userId);

            // Assert
            var objectResult = result.Result as ObjectResult;
            objectResult.Should().NotBeNull();
            objectResult.StatusCode.Should().Be(409);

            var response = objectResult.Value as ResponseDto;
            response.Should().BeEquivalentTo(responseDto);
            _authServiceMock.Verify();
        }

        [Test]
        public async Task DeleteUser_ShouldReturnBadRequest_WhenDeletionFails()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var authResult = new AuthServiceResult
            {
                ErrorCode = AuthErrorCode.InvalidUser
            };

            var responseDto = ConvertToResponseDto(authResult);

            _authServiceMock.Setup(a => a.DeleteUserAsync(userId.ToString()))
                .ReturnsAsync(authResult).Verifiable(Times.Once);

            // Act
            var result = await _authAPIController.DeleteUser(userId);

            // Assert
            var badRequestObjectResult = result.Result as BadRequestObjectResult;
            badRequestObjectResult.Should().NotBeNull();
            badRequestObjectResult.StatusCode.Should().Be(400);

            var response = badRequestObjectResult.Value as ResponseDto;
            response.Should().BeEquivalentTo(responseDto);
            _authServiceMock.Verify();
        }

        private ResponseDto ConvertToResponseDto(AuthServiceResult authServiceResult)
        {
            return new ResponseDto
            {
                IsSuccess = authServiceResult.IsSuccess,
                Token = authServiceResult.Token,
                Errors = authServiceResult.Errors,
                Result = authServiceResult.Result,
                Message = authServiceResult.ErrorCode switch
                {
                    AuthErrorCode.Success => "Success.",
                    AuthErrorCode.InvalidUser => "User not found.",
                    AuthErrorCode.EmailNotConfirmed => "Email not confirmed.",
                    AuthErrorCode.EmailAlreadyConfirmed => "Email already confirmed.",
                    AuthErrorCode.InvalidEmailOrPassword => "Invalid Email or Password.",
                    AuthErrorCode.InvalidCredentials => "Invalid Credentials.",
                    AuthErrorCode.InternalServerError => "Internal server error.",
                    AuthErrorCode.InvalidToken => "Invalid Token.",
                    AuthErrorCode.DeletionFailed => "Failed to delete.",
                    AuthErrorCode.NoUsersFound => "User not found.",
                    AuthErrorCode.UserAlreadyExists => "User with this email already exists.",
                    AuthErrorCode.InvalidData => "Invalid data.",
                    AuthErrorCode.Conflict => "Error caused by data conflict.",
                    _ => throw new ArgumentException()
                }
            };
        }

    }
}
