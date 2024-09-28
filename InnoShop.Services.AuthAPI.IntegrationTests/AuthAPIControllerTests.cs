using FluentAssertions;
using InnoShop.Services.AuthAPI.Data;
using InnoShop.Services.AuthAPI.Models;
using InnoShop.Services.AuthAPI.Models.Dto;
using InnoShop.Services.AuthAPI.Services;
using InnoShop.Services.AuthAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Transactions;

namespace InnoShop.Services.AuthAPI.IntegrationTests
{
    [TestFixture]
    public class AuthAPIControllerTests
    {
        private WebApplicationFactory<InnoShop.Services.AuthAPI.Program> _factory;
        private HttpClient _client;
        private UserManager<ApplicationUser> _userManager;
        private RoleManager<IdentityRole> _roleManager;
        private IJwtTokenGenerator _jwtTokenGenerator;
        private IEmailSender _emailSender;

        [SetUp]
        public void SetUp()
        {
            _factory = new WebApplicationFactory<InnoShop.Services.AuthAPI.Program>()
        .WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Test");



            builder.ConfigureServices(services =>
            {
                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=InnoShop_Auth_Test;Trusted_Connection=True;TrustServerCertificate=True");
                });
                

                var serviceProvider = services.BuildServiceProvider();
                using (var scope = serviceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    dbContext.Database.EnsureDeleted();
                    dbContext.Database.Migrate();
                }
            });
        });

            _client = _factory.CreateClient();
        }

        [TearDown]
        public async Task CleanUp()
        {
            using (var scope = _factory.Services.CreateScope())
            {
                _userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                _roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                _jwtTokenGenerator = scope.ServiceProvider.GetRequiredService<IJwtTokenGenerator>();
                _emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                await dbContext.Database.EnsureDeletedAsync();
                await dbContext.Database.MigrateAsync();
            }

            //var user = await _userManager.FindByEmailAsync("testuser12@example.com");

            //if (user != null)
            //{
            //    await _userManager.DeleteAsync(user);
            //}

            _factory.Dispose();
            _client.Dispose();
        }

        [Test]
        public async Task Register_ShouldReturnOkWithResponseDto_WhenDataIsValid()
        {
            // Arrange
            var requestModel = new
            {
                Email = "testuser12@example.com",
                Password = "ValidPass123*",
                Name = "Test User7",
                PhoneNumber = "+1234567890"
            };

            var expectedResult = new
            {
                Email = requestModel.Email,
                Name = requestModel.Name,
                PhoneNumber = requestModel.PhoneNumber
            };

            var jsonContent = JsonConvert.SerializeObject(requestModel);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("api/auth/users", content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var responseString = await response.Content.ReadAsStringAsync();

            var responseDto = JsonConvert.DeserializeObject<ResponseDto>(responseString.ToString());
            responseDto.IsSuccess.Should().BeTrue();

            var returnedUser = JsonConvert.DeserializeObject<ApplicationUser>(responseDto.Result.ToString());

            returnedUser.Should().BeEquivalentTo(expectedResult, options => options
                    .Including(x => x.Email)
                    .Including(x => x.Name)
                    .Including(x => x.PhoneNumber));
        }

        [Test]
        public async Task Register_ShouldReturnBadRequestWithResonseDto_WhenDataIsInvalid()
        {
            // Arrange
            var requestModel = new
            {
                Email = "invalid-email",
                Password = "123",
                Name = "",
                PhoneNumber = "12345"
            };

            var jsonContent = JsonConvert.SerializeObject(requestModel);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("api/auth/users", content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var responseString = await response.Content.ReadAsStringAsync();
            responseString.Should().Contain("Invalid email format.");
            responseString.Should().Contain("Password must be at least 6 characters long.");

            var responseDto = JsonConvert.DeserializeObject<ResponseDto>(responseString);

            responseDto.IsSuccess.Should().BeFalse();
            responseDto.Errors.Should().NotBeNull();

            var errors = responseDto.Errors as JObject;

            var emailErrors = errors["Email"]?.ToObject<List<string>>();
            emailErrors.Should().Contain("Invalid email format.");
        }

        [Test]
        public async Task Register_ShouldReturnConflictWithResponseDto_WhenUserAlreadyExists()
        {
            // Arrange
            var requestModel = new
            {
                Email = "existinguser@example.com",
                Password = "ValidPass123*",
                Name = "Existing User",
                PhoneNumber = "+1234567890"
            };

            var jsonContent = JsonConvert.SerializeObject(requestModel);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            // Act
            await _client.PostAsync("api/auth/users", content);
            var secondResponse = await _client.PostAsync("api/auth/users", content);

            // Assert
            secondResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);

            var responseString = await secondResponse.Content.ReadAsStringAsync();
            responseString.Should().Contain("\"isSuccess\":false");
            responseString.Should().Contain("User with this email already exists.");

            var responseDto = JsonConvert.DeserializeObject<ResponseDto>(responseString.ToString());

            responseDto.IsSuccess.Should().BeFalse();
            responseDto.Errors.Should().NotBeNull();

            var errorsArray = responseDto.Errors as JArray;
            var errors = errorsArray?.Select(e => e.ToString()).ToList();

            errors.Should().Contain("User with this email already exists.");
        }

        [Test]
        public async Task SendEmailConfirmation_ShouldReturnOkWithResponseDto_WhenEmailIsValidAndNotConfirmed()
        {
            // Arrange
            using (var scope = _factory.Services.CreateScope())
            {
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

                var requestModel = new
                {
                    Email = "testuser12@example.com"
                };

                var user = new ApplicationUser
                {
                    Name = "Test User",
                    UserName = requestModel.Email,
                    Email = requestModel.Email,
                    EmailConfirmed = false,
                };
                await userManager.CreateAsync(user, "ValidPass123*");

                var expectedResult = new ResponseDto
                {
                    IsSuccess = true,
                    Message = "Success."
                };

                var jsonContent = JsonConvert.SerializeObject(requestModel);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Act
                var response = await _client.PostAsync("/api/auth/users/resend-email-confirmation", content);

                // Assert
                response.StatusCode.Should().Be(HttpStatusCode.OK);

                var responseString = await response.Content.ReadAsStringAsync();
                responseString.Should().Contain("\"isSuccess\":true");

                var responseDto = JsonConvert.DeserializeObject<ResponseDto>(responseString.ToString());

                responseDto.Should().BeEquivalentTo(expectedResult);
            }
        }

        [Test]
        public async Task SendEmailConfirmation_ShouldReturnBadRequest_WhenEmailAlreadyConfirmed()
        {
            // Arrange
            using (var scope = _factory.Services.CreateScope())
            {
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

                var email = "confirmeduser@example.com";
                var user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    Name = "Test User",
                    EmailConfirmed = true
                };

                await userManager.CreateAsync(user, "ValidPassword123*");

                var requestModel = new
                {
                    Email = email
                };

                var expectedResult = new ResponseDto
                {
                    IsSuccess = false,
                    Message = "Email already confirmed."
                };

                var jsonContent = JsonConvert.SerializeObject(requestModel);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Act
                var response = await _client.PostAsync("/api/auth/users/resend-email-confirmation", content);

                // Assert
                response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

                var responseString = await response.Content.ReadAsStringAsync();
                var responseDto = JsonConvert.DeserializeObject<ResponseDto>(responseString);

                responseDto.Should().BeEquivalentTo(expectedResult);
            }
        }

        [Test]
        public async Task SendEmailConfirmation_ShouldReturnBadRequestWithResponseDto_WhenUserNotFound()
        {
            // Arrange
            var requestModel = new
            {
                Email = "nonexistentuser@example.com"
            };

            var expectedResponseDto = new ResponseDto
            {
                IsSuccess = false,
                Message = "User not found."
            };

            var jsonContent = JsonConvert.SerializeObject(requestModel);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/auth/users/resend-email-confirmation", content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var responseString = await response.Content.ReadAsStringAsync();
            var responseDto = JsonConvert.DeserializeObject<ResponseDto>(responseString);

            responseDto.Should().BeEquivalentTo(expectedResponseDto);
        }

        [Test]
        public async Task ConfirmEmail_ShouldReturnBadRequestWithResponseDto_WhenEmailAlreadyConfirmed()
        {
            using (var scope = _factory.Services.CreateScope())
            {
                // Arrange
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var jwtTokenGenerator = scope.ServiceProvider.GetRequiredService<IJwtTokenGenerator>();

                var email = "testuser12@example.com";
                var user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    Name = "Test User",
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(user, "ValidPassword123*");

                var emailToken = await userManager.GenerateEmailConfirmationTokenAsync(user);

                var validToken = jwtTokenGenerator.GenerateEmailConfirmationTokenAsync(user.Id, emailToken);

                //var roles = await userManager.GetRolesAsync(user);

                //var jwtToken = jwtTokenGenerator.GenerateToken(user, roles);

                var expectedResponse = new ResponseDto
                {
                    IsSuccess = false,
                    Message = "Email already confirmed.",
                    Errors = new List<string> { "Email has already been confirmed." }
                };

                // Act
                var response = await _client.GetAsync($"api/auth/users/confirm-email?token={validToken}");

                // Assert
                response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

                var responseString = await response.Content.ReadAsStringAsync();
                var responseDto = JsonConvert.DeserializeObject<ResponseDto>(responseString);
                responseDto.IsSuccess.Should().BeFalse();
                var errorsArray = responseDto.Errors as JArray;
                var errors = errorsArray?.Select(e => e.ToString()).ToList();

                errors.Should().Contain("Email has already been confirmed.");
            }
        }

        [Test]
        public async Task ConfirmEmail_ShouldReturnBadRequestWithResponseDto_WhenTokenInvalid()
        {
            using (var scope = _factory.Services.CreateScope())
            {
                // Arrange
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var jwtTokenGenerator = scope.ServiceProvider.GetRequiredService<IJwtTokenGenerator>();

                var email = "testuser12@example.com";
                var user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    Name = "Test User",
                    EmailConfirmed = false
                };

                var result = await userManager.CreateAsync(user, "ValidPassword123*");

                var emailToken = "";

                var invalidToken = jwtTokenGenerator.GenerateEmailConfirmationTokenAsync(user.Id, emailToken);

                //var roles = await userManager.GetRolesAsync(user);

                //var jwtToken = jwtTokenGenerator.GenerateToken(user, roles);

                var expectedResponse = new ResponseDto
                {
                    IsSuccess = false,
                    Message = "Invalid Token.",
                };

                // Act
                var response = await _client.GetAsync($"api/auth/users/confirm-email?token={invalidToken}");

                // Assert
                response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

                var responseString = await response.Content.ReadAsStringAsync();
                var responseDto = JsonConvert.DeserializeObject<ResponseDto>(responseString);
                responseDto.Should().BeEquivalentTo(expectedResponse);
            }
        }

        [Test]
        public async Task ConfirmEmail_ShouldReturnBadRequestWithResponseDto_WhenUserNotFound()
        {

            using (var scope = _factory.Services.CreateScope())
            {
                // Arrange
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var jwtTokenGenerator = scope.ServiceProvider.GetRequiredService<IJwtTokenGenerator>();

                var email = "testuser12@example.com";
                var user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    Name = "Test User",
                    EmailConfirmed = true
                };

                var emailToken = await userManager.GenerateEmailConfirmationTokenAsync(user);

                var validToken = jwtTokenGenerator.GenerateEmailConfirmationTokenAsync(user.Id, emailToken);

                var expectedResponse = new ResponseDto
                {
                    IsSuccess = false,
                    Message = "Invalid Credentials.",
                };

                // Act
                var response = await _client.GetAsync($"api/auth/users/confirm-email?token={validToken}");

                // Assert
                response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

                var responseString = await response.Content.ReadAsStringAsync();
                var responseDto = JsonConvert.DeserializeObject<ResponseDto>(responseString);
                responseDto.Should().BeEquivalentTo(expectedResponse);
            }
        }

        [Test]
        public async Task ConfirmEmail_ShouldReturnOkWithResponseDto_WhenEmailConfirmedAndJwtTokenGeneratedSuccessfully()
        {

            using (var scope = _factory.Services.CreateScope())
            {
                // Arrange
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var jwtTokenGenerator = scope.ServiceProvider.GetRequiredService<IJwtTokenGenerator>();

                var email = "testuser12@example.com";
                var user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    Name = "Test User",
                    EmailConfirmed = false,
                };

                var result = await userManager.CreateAsync(user, "ValidPassword123*");

                var emailToken = await userManager.GenerateEmailConfirmationTokenAsync(user);

                var validToken = jwtTokenGenerator.GenerateEmailConfirmationTokenAsync(user.Id, emailToken);

                // Act
                var response = await _client.GetAsync($"api/auth/users/confirm-email?token={validToken}");

                // Assert
                response.StatusCode.Should().Be(HttpStatusCode.OK);

                var responseString = await response.Content.ReadAsStringAsync();
                var responseDto = JsonConvert.DeserializeObject<ResponseDto>(responseString);
                responseDto.IsSuccess.Should().BeTrue();
                responseDto.Token.Should().NotBeNull();

                var handler = new JwtSecurityTokenHandler();
                SecurityToken validatedToken;

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("Add your secret here and change in the future.")),

                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true
                };

                Action act = () => handler.ValidateToken(responseDto.Token, validationParameters, out validatedToken);

                act.Should().NotThrow();
            }
        }

        [Test]
        public async Task ConfirmEmail_ShouldReturnBadRequestWithResponseDto_WhenEmailConfirmationFails()
        {

            using (var scope = _factory.Services.CreateScope())
            {
                // Arrange
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var jwtTokenGenerator = scope.ServiceProvider.GetRequiredService<IJwtTokenGenerator>();

                var email = "testuser12@example.com";
                var user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    Name = "Test User"
                };

                // Создаем пользователя
                await userManager.CreateAsync(user, "ValidPassword123*");

                var emailToken = "fake-email-token";
                var validToken = jwtTokenGenerator.GenerateEmailConfirmationTokenAsync(user.Id, emailToken);

                // Act
                var response = await _client.GetAsync($"api/auth/users/confirm-email?token={validToken}");

                // Assert
                response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

                var responseString = await response.Content.ReadAsStringAsync();
                var responseDto = JsonConvert.DeserializeObject<ResponseDto>(responseString);

                responseDto.IsSuccess.Should().BeFalse();
                responseDto.Message.Should().Be("Invalid Token.");
                responseDto.Errors.Should().NotBeNull();

                var errorsArray = responseDto.Errors as JArray;
                var errors = errorsArray?.Select(e => e.ToString()).ToList();

                errors.Should().Contain("Invalid token.");
            }
        }

        [Test]
        public async Task Login_ShouldReturnOkWithResponseDtoWithToken_WhenCredentialsAreValid()
        {
            using (var scope = _factory.Services.CreateScope())
            {
                // Arrange
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

                var email = "testuser12@example.com";
                var user = new ApplicationUser
                {
                    Name = email,
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(user, "ValidPass123*");

                var requestModel = new LoginRequestDto { Email = email, Password = "ValidPass123*" };
                var jsonContent = JsonConvert.SerializeObject(requestModel);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Act
                var response = await _client.PostAsync("/api/auth/sessions", content);

                // Assert
                response.StatusCode.Should().Be(HttpStatusCode.OK);
                var responseString = await response.Content.ReadAsStringAsync();
                var responseDto = JsonConvert.DeserializeObject<ResponseDto>(responseString);

                responseDto.IsSuccess.Should().BeTrue();
                responseDto.Token.Should().NotBeNull();

                var handler = new JwtSecurityTokenHandler();
                SecurityToken validatedToken;

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("Add your secret here and change in the future.")),

                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true
                };

                Action act = () => handler.ValidateToken(responseDto.Token, validationParameters, out validatedToken);

                act.Should().NotThrow();
            }
        }

        [Test]
        public async Task Login_ShouldReturnBadRequestWithResponseDto_WhenUserDoesNotExist()
        {
            using (var scope = _factory.Services.CreateScope())
            {
                // Arrange
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

                var requestModel = new LoginRequestDto { Email = "nonexistentuser@example.com", Password = "ValidPass123*" };

                var jsonContent = JsonConvert.SerializeObject(requestModel);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Act
                var response = await _client.PostAsync("/api/auth/sessions", content);

                // Assert
                response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
                var responseString = await response.Content.ReadAsStringAsync();
                var responseDto = JsonConvert.DeserializeObject<ResponseDto>(responseString);

                responseDto.IsSuccess.Should().BeFalse();
                responseDto.Message.Should().Contain("User not found.");
            }
        }

        [Test]
        public async Task Login_ShouldReturnStatusCode403WithResponseDto_WhenEmailIsNotConfirmed()
        {
            using (var scope = _factory.Services.CreateScope())
            {
                // Arrange
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

                var email = "testuser12@example.com";
                var user = new ApplicationUser
                {
                    Name = email,
                    UserName = email,
                    Email = email,
                    EmailConfirmed = false,
                };
                await userManager.CreateAsync(user, "ValidPass123*");

                var requestModel = new LoginRequestDto { Email = email, Password = "ValidPass123*" };
                var jsonContent = JsonConvert.SerializeObject(requestModel);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Act
                var response = await _client.PostAsync("/api/auth/sessions", content);

                // Assert
                response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
                var responseString = await response.Content.ReadAsStringAsync();
                var responseDto = JsonConvert.DeserializeObject<ResponseDto>(responseString);

                responseDto.IsSuccess.Should().BeFalse();
                responseDto.Message.Should().Contain("Email not confirmed.");
            }
        }

        [Test]
        public async Task Login_ShouldReturnBadRequestWithResponseDto_WhenPasswordIsIncorrect()
        {
            using (var scope = _factory.Services.CreateScope())
            {
                // Arrange
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var roleManager = scope.ServiceProvider.GetService<RoleManager<ApplicationUser>>();

                var email = "testuser12@example.com";
                var user = new ApplicationUser
                {
                    Name = email,
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                };
                await userManager.CreateAsync(user, "ValidPass123*");

                var requestModel = new LoginRequestDto { Email = email, Password = "InvalidPass123*" };
                var jsonContent = JsonConvert.SerializeObject(requestModel);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Act
                var response = await _client.PostAsync("/api/auth/sessions", content);

                // Assert
                response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
                var responseString = await response.Content.ReadAsStringAsync();
                var responseDto = JsonConvert.DeserializeObject<ResponseDto>(responseString);

                responseDto.IsSuccess.Should().BeFalse();
                responseDto.Message.Should().Contain("Invalid Email or Password.");

            }
        }

        [Test]
        public async Task AddRoleToUser_ShouldReturnOkWithResponseDto_WhenRoleAssignedSuccessfully()
        {
            using (var scope = _factory.Services.CreateScope())
            {
                // Arrange
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var jwtTokenGenerator = scope.ServiceProvider.GetRequiredService<IJwtTokenGenerator>();

                var client = _client;

                var email = "testuser12@example.com";
                var user = new ApplicationUser
                {
                    Name = email,
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                };
                await userManager.CreateAsync(user, "ValidPass123*");

                var roles = new[] { "ADMIN" };

                var token = jwtTokenGenerator.GenerateToken(user, roles);

                var requestModel = new { Role = "User" };

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var jsonContent = JsonConvert.SerializeObject(requestModel);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Act
                var response = await _client.PostAsync($"/api/auth/users/{user.Id}/roles", content);

                // Assert
                response.StatusCode.Should().Be(HttpStatusCode.OK);
                var responseString = await response.Content.ReadAsStringAsync();
                var responseDto = JsonConvert.DeserializeObject<ResponseDto>(responseString);

                responseDto.IsSuccess.Should().BeTrue();
                responseDto.Message.Should().Contain("Success.");
            }
        }

        [Test]
        public async Task AddRoleToUser_ShouldReturnBadRequestWithResponseDto_WhenUserNotFound()
        {
            using (var scope = _factory.Services.CreateScope())
            {
                // Arrange
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var jwtTokenGenerator = scope.ServiceProvider.GetRequiredService<IJwtTokenGenerator>();

                var client = _client;

                var email = "testuser12@example.com";
                var user = new ApplicationUser
                {
                    Name = email,
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                };
                await userManager.CreateAsync(user, "ValidPass123*");

                var roles = new[] { "ADMIN" };

                var token = jwtTokenGenerator.GenerateToken(user, roles);

                var requestModel = new { Role = "User" };

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var jsonContent = JsonConvert.SerializeObject(requestModel);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Act
                var response = await _client.PostAsync($"/api/auth/users/{Guid.NewGuid()}/roles", content);

                // Assert
                response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
                var responseString = await response.Content.ReadAsStringAsync();
                var responseDto = JsonConvert.DeserializeObject<ResponseDto>(responseString);

                responseDto.IsSuccess.Should().BeFalse();
                responseDto.Message.Should().Contain("User not found.");
            }
        }

        //[Test]
        //public async Task AddRoleToUser_ShouldReturnStatusCode500WithError_WhenRoleAssignmentFails()
        //{
        //    using (var scope = _factory.Services.CreateScope())
        //    {
        //        // Arrange
        //        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        //        var jwtTokenGenerator = scope.ServiceProvider.GetRequiredService<IJwtTokenGenerator>();

        //        var client = _client;

        //        var email = "testuser12@example.com";
        //        var user = new ApplicationUser
        //        {
        //            Name = email,
        //            UserName = email,
        //            Email = email,
        //            EmailConfirmed = true,
        //        };
        //        await userManager.CreateAsync(user, "ValidPass123*");

        //        var roles = new[] { "ADMIN" };

        //        var token = jwtTokenGenerator.GenerateToken(user, roles);

        //        var requestModel = new { Role = new string('A', 257) };

        //        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        //        var jsonContent = JsonConvert.SerializeObject(requestModel);
        //        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        //        // Act
        //        var response = await _client.PostAsync($"/api/auth/users/{user.Id}/roles", content);

        //        // Assert
        //        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);

        //        var responseString = await response.Content.ReadAsStringAsync();

        //        responseString.Should().Contain("String or binary data would be truncated");
        //    }
        //}

        [Test]
        public async Task ForgotPassword_ShouldReturnOkWithResponseDto_WhenEmailIsValid()
        {
            using (var scope = _factory.Services.CreateScope())
            {
                // Arrange
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var email = "testuser12@example.com";

                var user = new ApplicationUser
                {
                    Name = "Test User",
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                };
                await userManager.CreateAsync(user, "ValidPass123*");

                var requestModel = new { Email = email };

                var jsonContent = JsonConvert.SerializeObject(requestModel);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Act
                var response = await _client.PostAsync("/api/auth/users/password/forgot", content);

                // Assert
                response.StatusCode.Should().Be(HttpStatusCode.OK);
                var responseString = await response.Content.ReadAsStringAsync();
                var responseDto = JsonConvert.DeserializeObject<ResponseDto>(responseString);

                responseDto.IsSuccess.Should().BeTrue();
                responseDto.Message.Should().Contain("Success.");
            }
        }

        [Test]
        public async Task ForgotPassword_ShouldReturnBadRequestWithResponseDto_WhenEmailIsInvalid()
        {
            // Arrange
            var requestModel = new { Email = "nonexistentuser@example.com" };

            var jsonContent = JsonConvert.SerializeObject(requestModel);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/auth/users/password/forgot", content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var responseString = await response.Content.ReadAsStringAsync();
            var responseDto = JsonConvert.DeserializeObject<ResponseDto>(responseString);

            responseDto.IsSuccess.Should().BeFalse();
            responseDto.Message.Should().Contain("Invalid Credentials");
        }

        [Test]
        public async Task ResetPassword_ShouldReturnOkWithResponseDto_WhenPasswordResetSuccessfully()
        {
            using (var scope = _factory.Services.CreateScope())
            {
                // Arrange
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var email = "testuser12@example.com";

                var user = new ApplicationUser
                {
                    Name = "Test User",
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                };
                await userManager.CreateAsync(user, "ValidPass123*");

                var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
                var requestModel = new
                {
                    Email = email,
                    Token = resetToken,
                    NewPassword = "NewValidPass123*"
                };

                var jsonContent = JsonConvert.SerializeObject(requestModel);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Act
                var response = await _client.PostAsync("/api/auth/users/password/reset", content);

                // Assert
                response.StatusCode.Should().Be(HttpStatusCode.OK);
                var responseString = await response.Content.ReadAsStringAsync();
                var responseDto = JsonConvert.DeserializeObject<ResponseDto>(responseString);

                responseDto.IsSuccess.Should().BeTrue();
                responseDto.Message.Should().Contain("Success.");
            }
        }

        [Test]
        public async Task ResetPassword_ShouldReturnBadRequestWithResponseDto_WhenInvalidRequest()
        {
            using (var scope = _factory.Services.CreateScope())
            {
                // Arrange
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var email = "testuser12@example.com";

                var user = new ApplicationUser
                {
                    Name = "Test User",
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                };
                await userManager.CreateAsync(user, "ValidPass123*");

                var resetToken = "fake-token";
                var requestModel = new
                {
                    Email = email,
                    Token = resetToken,
                    NewPassword = "NewValidPass123*"
                };

                var jsonContent = JsonConvert.SerializeObject(requestModel);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Act
                var response = await _client.PostAsync("/api/auth/users/password/reset", content);

                // Assert
                response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
                var responseString = await response.Content.ReadAsStringAsync();
                var responseDto = JsonConvert.DeserializeObject<ResponseDto>(responseString);

                responseDto.IsSuccess.Should().BeFalse();
                responseDto.Message.Should().Contain("Invalid Credentials.");
                var errorsArray = responseDto.Errors as JArray;
                var errors = errorsArray?.Select(e => e.ToString()).ToList();

                errors.Should().Contain("Invalid token.");
            }
        }

        [Test]
        public async Task ResetPassword_ShouldReturnBadRequestWithResponseDto_WhenEmailIsInvalid()
        {
            // Arrange
            var requestModel = new
            {
                Email = "nonexistentuser@example.com",  // Invalid email
                Token = "validToken",
                NewPassword = "NewValidPass123*"
            };

            var jsonContent = JsonConvert.SerializeObject(requestModel);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/auth/users/password/reset", content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var responseString = await response.Content.ReadAsStringAsync();
            var responseDto = JsonConvert.DeserializeObject<ResponseDto>(responseString);

            responseDto.IsSuccess.Should().BeFalse();
            responseDto.Message.Should().Contain("Invalid Credentials");

        }

        [Test]
        public async Task UpdateUser_ShouldReturnOkWithResponseDto_WhenUserUpdatedSuccessfully()
        {
            using (var scope = _factory.Services.CreateScope())
            {
                // Arrange
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var jwtTokenGenerator = scope.ServiceProvider.GetRequiredService<IJwtTokenGenerator>();

                var email = "testuser12@example.com";
                var user = new ApplicationUser
                {
                    Name = "Old Name",
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                };
                await userManager.CreateAsync(user, "ValidPass123*");

                var roles = new[] { "USER" };
                var token = jwtTokenGenerator.GenerateToken(user, roles);

                var client = _client;
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var updateUserDto = new
                {
                    Name = "New Name",
                    PhoneNumber = "+1234567890"
                };

                var jsonContent = JsonConvert.SerializeObject(updateUserDto);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Act
                var response = await client.PutAsync($"/api/auth/users/{user.Id}", content);

                // Assert
                response.StatusCode.Should().Be(HttpStatusCode.OK);
                var responseString = await response.Content.ReadAsStringAsync();
                var responseDto = JsonConvert.DeserializeObject<ResponseDto>(responseString);

                responseDto.IsSuccess.Should().BeTrue();
                responseDto.Message.Should().Contain("Success.");

                using (var newScope = _factory.Services.CreateScope())
                {
                    var newUserManager = newScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                    var updatedUser = await newUserManager.FindByIdAsync(user.Id.ToString());

                    updatedUser.Name.Should().Be("New Name");
                    updatedUser.PhoneNumber.Should().Be("+1234567890");
                }
            }
        }

        [Test]
        public async Task UpdateUser_ShouldReturnForbid_WhenUserIsNotOwnerOrAdmin()
        {
            using (var scope = _factory.Services.CreateScope())
            {
                // Arrange
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var jwtTokenGenerator = scope.ServiceProvider.GetRequiredService<IJwtTokenGenerator>();

                var email = "testuser12@example.com";
                var user = new ApplicationUser
                {
                    Name = "Test Name",
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                };
                await userManager.CreateAsync(user, "ValidPass123*");

                var roles = new[] { "USER" };
                var token = jwtTokenGenerator.GenerateToken(user, roles);

                var client = _client;
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var updateUserDto = new
                {
                    Name = "New Name",
                    PhoneNumber = "+1234567890"
                };

                var jsonContent = JsonConvert.SerializeObject(updateUserDto);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Act
                var response = await client.PutAsync($"/api/auth/users/{Guid.NewGuid()}", content);

                // Assert
                response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            }
        }

        [Test]
        public async Task UpdateUser_ShouldReturnBadRequestWithResponseDto_WhenUserNotFound()
        {
            using (var scope = _factory.Services.CreateScope())
            {
                // Arrange
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var jwtTokenGenerator = scope.ServiceProvider.GetRequiredService<IJwtTokenGenerator>();

                var email = "testuser12@example.com";
                var user = new ApplicationUser
                {
                    Name = "Admin Name",
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                };
                await userManager.CreateAsync(user, "ValidPass123*");

                var roles = new[] { "ADMIN" };
                var token = jwtTokenGenerator.GenerateToken(user, roles);

                var client = _client;
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var updateUserDto = new
                {
                    Name = "New Name",
                    PhoneNumber = "+1234567890"
                };

                var jsonContent = JsonConvert.SerializeObject(updateUserDto);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Act
                var response = await client.PutAsync($"/api/auth/users/{Guid.NewGuid()}", content);

                // Assert
                response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
                var responseString = await response.Content.ReadAsStringAsync();
                var responseDto = JsonConvert.DeserializeObject<ResponseDto>(responseString);

                responseDto.IsSuccess.Should().BeFalse();
                responseDto.Message.Should().Contain("User not found.");
            }
        }

        [Test]
        public async Task GetUsersWithPagination_ShouldReturnOkWithPaginatedUsers_WhenUsersExist()
        {
            using (var scope = _factory.Services.CreateScope())
            {
                // Arrange
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var jwtTokenGenerator = scope.ServiceProvider.GetRequiredService<IJwtTokenGenerator>();

                var client = _client;

                // Создаём пользователя с ролью ADMIN
                var adminEmail = "testuser12@example.com";
                var adminUser = new ApplicationUser
                {
                    Name = "New name",
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(adminUser, "ValidPass123*");

                var roles = new[] { "ADMIN" };
                var token = jwtTokenGenerator.GenerateToken(adminUser, roles);

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var pageNumber = 1;
                var pageSize = 5;

                // Act
                var response = await client.GetAsync($"/api/auth/users/paginated?pageNumber={pageNumber}&pageSize={pageSize}");

                // Assert
                response.StatusCode.Should().Be(HttpStatusCode.OK);

                var responseString = await response.Content.ReadAsStringAsync();
                var responseDto = JsonConvert.DeserializeObject<ResponseDto>(responseString);

                responseDto.IsSuccess.Should().BeTrue();
                responseDto.Result.Should().NotBeNull();

                var users = responseDto.Result as JArray;
                users.Count.Should().BeGreaterThan(0);
            }
        }

        [Test]
        public async Task GetUsersWithPagination_ShouldReturnBadRequest_WhenNoUsersFound()
        {
            using (var scope = _factory.Services.CreateScope())
            {
                // Arrange
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var jwtTokenGenerator = scope.ServiceProvider.GetRequiredService<IJwtTokenGenerator>();

                var client = _client;

                // Создаём пользователя с ролью ADMIN
                var adminEmail = "testuser12@example.com";
                var adminUser = new ApplicationUser
                {   Name = "New Name",
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(adminUser, "ValidPass123*");

                var roles = new[] { "ADMIN" };
                var token = jwtTokenGenerator.GenerateToken(adminUser, roles);

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var pageNumber = 899; 
                var pageSize = 5;

                // Act
                var response = await client.GetAsync($"/api/auth/users/paginated?pageNumber={pageNumber}&pageSize={pageSize}");

                // Assert
                response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

                var responseString = await response.Content.ReadAsStringAsync();
                var responseDto = JsonConvert.DeserializeObject<ResponseDto>(responseString);

                responseDto.IsSuccess.Should().BeFalse();

            }
        }

        [Test]
        public async Task GetUserByEmail_ShouldReturnOkWithUserDto_WhenUserExists()
        {
            using (var scope = _factory.Services.CreateScope())
            {
                // Arrange
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var jwtTokenGenerator = scope.ServiceProvider.GetRequiredService<IJwtTokenGenerator>();
                var client = _client;

                var adminEmail = "testuser12@example.com";
                var adminUser = new ApplicationUser
                {
                    Name = "Test Name",
                    UserName = adminEmail,
                    Email = adminEmail,
                    PhoneNumber = "+1234567890",
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(adminUser, "ValidPass123*");

                var roles = new[] { "ADMIN" };
                var token = jwtTokenGenerator.GenerateToken(adminUser, roles);

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var requestModel = new { Email = adminEmail };

                var jsonContent = JsonConvert.SerializeObject(requestModel);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Act
                var response = await client.PostAsync("/api/auth/users/by-email", content);

                // Assert
                response.StatusCode.Should().Be(HttpStatusCode.OK);

                var responseString = await response.Content.ReadAsStringAsync();
                var responseDto = JsonConvert.DeserializeObject<ResponseDto>(responseString);

                responseDto.IsSuccess.Should().BeTrue();
                responseDto.Result.Should().NotBeNull();

                var returnedUser = JsonConvert.DeserializeObject<UserDto>(responseDto.Result.ToString());
                returnedUser.Email.Should().Be(adminEmail);
                returnedUser.Name.Should().Be("Test Name");
                returnedUser.PhoneNumber.Should().Be("+1234567890");
            }
        }

        [Test]
        public async Task GetUserByEmail_ShouldReturnBadRequestWithUserDto_WhenUserNotExists()
        {
            using (var scope = _factory.Services.CreateScope())
            {
                // Arrange
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var jwtTokenGenerator = scope.ServiceProvider.GetRequiredService<IJwtTokenGenerator>();
                var client = _client;

                var adminEmail = "testuser12@example.com";
                var adminUser = new ApplicationUser
                {
                    Name = "Test Name",
                    UserName = adminEmail,
                    Email = adminEmail,
                    PhoneNumber = "+1234567890",
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(adminUser, "ValidPass123*");

                var roles = new[] { "ADMIN" };
                var token = jwtTokenGenerator.GenerateToken(adminUser, roles);

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var requestModel = new { Email = "fakeemail@gmail.com" };

                var jsonContent = JsonConvert.SerializeObject(requestModel);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Act
                var response = await client.PostAsync("/api/auth/users/by-email", content);

                // Assert
                response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

                var responseString = await response.Content.ReadAsStringAsync();
                var responseDto = JsonConvert.DeserializeObject<ResponseDto>(responseString);

                responseDto.IsSuccess.Should().BeFalse();
                responseDto.Message.Should().Contain("User not found.");    
            }
        }

        [Test]
        public async Task GetUserByEmail_ShouldReturnUnauthorized_WhenNotAuthenticated()
        {
            using (var scope = _factory.Services.CreateScope())
            {
                // Arrange
                var client = _client;

                var requestModel = new { Email = "adminEmail@gmail.com" };

                var jsonContent = JsonConvert.SerializeObject(requestModel);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Act
                var response = await client.PostAsync("/api/auth/users/by-email", content);

                // Assert
                response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            }
        }

        [Test]
        public async Task DeleteUser_ShouldReturnOkWithResponseDto_WhenUserIsDeletedSuccessfully()
        {
            using (var scope = _factory.Services.CreateScope())
            {
                // Arrange
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var jwtTokenGenerator = scope.ServiceProvider.GetRequiredService<IJwtTokenGenerator>();

                var email = "testuser12@example.com";
                var user = new ApplicationUser
                {
                    Name = "Test User",
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(user, "ValidPass123*");

                var roles = new[] { "ADMIN" };
                var token = jwtTokenGenerator.GenerateToken(user, roles);

                var client = _client;
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                // Act
                var response = await client.DeleteAsync($"/api/auth/users/{user.Id}");

                // Assert
                response.StatusCode.Should().Be(HttpStatusCode.OK);
                var responseString = await response.Content.ReadAsStringAsync();
                var responseDto = JsonConvert.DeserializeObject<ResponseDto>(responseString);

                responseDto.IsSuccess.Should().BeTrue();
                responseDto.Message.Should().Contain("Success");

                using (var newScope = _factory.Services.CreateScope())
                {
                    var newUserManager = newScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                    var deletedUser = await newUserManager.FindByIdAsync(user.Id.ToString());

                    deletedUser.Should().BeNull();
                }
            }
        }

        [Test]
        public async Task DeleteUser_ShouldReturnBadRequestWithResponseDto_WhenUserDoesNotExist()
        {
            using (var scope = _factory.Services.CreateScope())
            {
                // Arrange
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var jwtTokenGenerator = scope.ServiceProvider.GetRequiredService<IJwtTokenGenerator>();

                var email = "testuser12@example.com";
                var user = new ApplicationUser
                {
                    Name = "Test User",
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(user, "ValidPass123*");

                var roles = new[] { "ADMIN" };
                var token = jwtTokenGenerator.GenerateToken(user, roles);

                var client = _client;
                var nonExistentUserId = Guid.NewGuid();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                // Act
                var response = await client.DeleteAsync($"/api/auth/users/{nonExistentUserId}");

                // Assert
                response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
                var responseString = await response.Content.ReadAsStringAsync();
                var responseDto = JsonConvert.DeserializeObject<ResponseDto>(responseString);

                responseDto.IsSuccess.Should().BeFalse();
                responseDto.Message.Should().Contain("User not found.");
            }
        }

        [Test]
        public async Task DeleteUser_ShouldReturnUnauthorized_WhenNotAuthenticated()
        {
            using (var scope = _factory.Services.CreateScope())
            {
                // Arrange
                var client = _client;

                var requestModel = new { Email = "adminEmail@gmail.com" };

                var jsonContent = JsonConvert.SerializeObject(requestModel);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Act
                var response = await client.DeleteAsync($"/api/auth/users/{Guid.NewGuid()}");

                // Assert
                response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            }
        }

    }
}
