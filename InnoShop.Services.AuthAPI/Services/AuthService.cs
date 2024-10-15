using InnoShop.Services.AuthAPI.Models;
using InnoShop.Services.AuthAPI.Models.Dto;
using InnoShop.Services.AuthAPI.Services.Interfaces;
using InnoShop.Services.AuthAPI.Utility;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Data;

namespace InnoShop.Services.AuthAPI.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IJwtTokenValidator _jwtTokenValidator;
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration _configuration;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IJwtTokenGenerator jwtTokenGenerator,
        IJwtTokenValidator jwtTokenValidator,
        IEmailSender emailSender,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _jwtTokenGenerator = jwtTokenGenerator;
        _jwtTokenValidator = jwtTokenValidator;
        _emailSender = emailSender;
        _configuration = configuration;
    }

    public async Task<AuthServiceResult> Register(RegistrationRequestDto registrationRequestDto, Func<string, string> emailMessageBuilder)
    {
        var existingUser = await _userManager.FindByEmailAsync(registrationRequestDto.Email.ToLower());

        if (existingUser != null)
        {
            return new AuthServiceResult
            {
                ErrorCode = AuthErrorCode.UserAlreadyExists,
                Errors =  new[] { "User with this email already exists." }
            };
        }

        var roleName = Role.Admin;

        ApplicationUser applicationUser = new()
        {
            UserName = registrationRequestDto.Email,
            Email = registrationRequestDto.Email,
            NormalizedEmail = registrationRequestDto.Email.ToUpper(),
            Name = registrationRequestDto.Name,
            PhoneNumber = registrationRequestDto.PhoneNumber,
        };

        var result = await _userManager.CreateAsync(applicationUser, registrationRequestDto.Password);

        if (result.Succeeded)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                await _roleManager.CreateAsync(new IdentityRole(roleName));
            }

            await _userManager.AddToRoleAsync(applicationUser, roleName);

            await SendEmailConfirmationAsync(applicationUser.Email, emailMessageBuilder);

            //var userToReturn = await _userManager.FindByEmailAsync(applicationUser.Email.ToLower());

            var userDto = new UserDto
            {
                Email = applicationUser.Email,
                Id = applicationUser.Id,
            };

            return new AuthServiceResult
            {
                ErrorCode = AuthErrorCode.Success,
                Result = userDto
            };
        }

        return new AuthServiceResult
        {
            ErrorCode = AuthErrorCode.InvalidCredentials,
            Errors = result.Errors.Select(e => e.Description)
        };
    }

    public async Task<AuthServiceResult> SendEmailConfirmationAsync(string email, Func<string, string> emailMessageBuilder)
    {
        var user = await _userManager.FindByEmailAsync(email);

        if (user == null)
        {
            return new AuthServiceResult
            {
                ErrorCode = AuthErrorCode.InvalidUser
            };
        }

        if (await _userManager.IsEmailConfirmedAsync(user))
        {
            return new AuthServiceResult
            {
                ErrorCode = AuthErrorCode.EmailAlreadyConfirmed,
            };
        }

        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

        var emailConfirmationToken = _jwtTokenGenerator.GenerateEmailConfirmationTokenAsync(user.Id, token);

        var emailMessage = emailMessageBuilder(emailConfirmationToken);

        await _emailSender.SendEmailAsync(user.Email, "Confirm your email",
            $"{emailMessage}");

        return new AuthServiceResult
        {
            ErrorCode = AuthErrorCode.Success,
        };
    }

    public async Task<AuthServiceResult> ConfirmEmailAsync(string token)
    {
        var (userId, emailToken) = _jwtTokenValidator.ValidateEmailConfirmationJwt(token);

        if (userId.IsNullOrEmpty() || emailToken.IsNullOrEmpty())
        {
            return new AuthServiceResult
            {
                ErrorCode = AuthErrorCode.InvalidToken,
            };
        }

        var applicationUser = await _userManager.FindByIdAsync(userId);
        if (applicationUser == null)
        {
            return new AuthServiceResult
            {
                ErrorCode = AuthErrorCode.InvalidCredentials,
            };
        }

        if (applicationUser.EmailConfirmed)
        {
            return new AuthServiceResult
            {
                ErrorCode = AuthErrorCode.EmailAlreadyConfirmed,
                Errors = new[] { "Email has already been confirmed." }
            };
        }

        var result = await _userManager.ConfirmEmailAsync(applicationUser, emailToken);
        if (result.Succeeded)
        {
            var roles = await _userManager.GetRolesAsync(applicationUser);

            var jwtToken = _jwtTokenGenerator.GenerateToken(applicationUser, roles);

            return new AuthServiceResult
            {
                ErrorCode = AuthErrorCode.Success,
                Token = jwtToken
            };
        }

        return new AuthServiceResult
        {
            ErrorCode = AuthErrorCode.InvalidToken,
            Errors = result.Errors.Select(e => e.Description)
        };
    }

    public async Task<AuthServiceResult> Login(LoginRequestDto loginRequestDto)
    {
        var applicationUser = await _userManager.FindByEmailAsync(loginRequestDto.Email);

        if (applicationUser == null)
        {
            return new AuthServiceResult { ErrorCode = AuthErrorCode.InvalidUser };
        }

        if (!applicationUser.EmailConfirmed)
        {
            return new AuthServiceResult { ErrorCode = AuthErrorCode.EmailNotConfirmed };
        }

        bool isValidPassword = await _userManager.CheckPasswordAsync(applicationUser, loginRequestDto.Password);

        if (!isValidPassword)
        {
            return new AuthServiceResult { ErrorCode = AuthErrorCode.InvalidEmailOrPassword };
        }

        var roles = await _userManager.GetRolesAsync(applicationUser);

        var jwtToken = _jwtTokenGenerator.GenerateToken(applicationUser, roles);

        return new AuthServiceResult { ErrorCode = AuthErrorCode.Success, Token = jwtToken };
    }

    public async Task<AuthServiceResult> AssignRole(string id, string roleName)
    {
        var user = await _userManager.FindByIdAsync(id);

        if (user != null)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                await _roleManager.CreateAsync(new IdentityRole(roleName));
            }
            var result = await _userManager.AddToRoleAsync(user, roleName.ToUpper());
            if (result.Succeeded) 
            {
                return new AuthServiceResult
                {
                    ErrorCode = AuthErrorCode.Success,
                };
            }
            return new AuthServiceResult
            {
                ErrorCode = AuthErrorCode.Conflict,
                Errors = result.Errors.Select(e => e.Description)
            };
        }
        return new AuthServiceResult
        {
            ErrorCode = AuthErrorCode.InvalidUser,
        };
    }

    public async Task<AuthServiceResult> GeneratePasswordResetTokenAsync(string email, Func<string, string, string> emailMessageBuilder)
    {
        var user = await _userManager.FindByEmailAsync(email);

        if (user == null)
        {
            return new AuthServiceResult
            {
                ErrorCode = AuthErrorCode.InvalidCredentials,
            };
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);

        var emailMessage = emailMessageBuilder(token,email);

        await _emailSender.SendEmailAsync(email, "Password Reset", emailMessage);

        return new AuthServiceResult
        {
            ErrorCode = AuthErrorCode.Success,
        };
    }

    public async Task<AuthServiceResult> ResetPasswordAsync(string email, string token, string newPassword)
    {
        var user = await _userManager.FindByEmailAsync(email.ToLower());

        if (user == null)
        {
            return new AuthServiceResult
            {
                ErrorCode = AuthErrorCode.InvalidCredentials,
            };
        }

        var resetResult = await _userManager.ResetPasswordAsync(user, token, newPassword);

        if (!resetResult.Succeeded)
        {
            return new AuthServiceResult
            {
                ErrorCode = AuthErrorCode.InvalidCredentials,
                Errors = resetResult.Errors.Select(e => e.Description)
            };
        }

        return new AuthServiceResult
        {
            ErrorCode = AuthErrorCode.Success,
        };
    }

    public async Task<AuthServiceResult> ChangePasswordAsync(ChangePasswordDto model, string id)
    {
        var user = await _userManager.FindByIdAsync(id);

        if (user == null)
        {
            return new AuthServiceResult
            {
                ErrorCode = AuthErrorCode.InvalidCredentials,
            };
        }

        var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

        if (!result.Succeeded)
        {
            return new AuthServiceResult
            {
                ErrorCode = AuthErrorCode.InvalidCredentials,
                //Message = string.Join(", ", result.Errors.Select(e => e.Description))
                Errors = result.Errors.Select(e => e.Description)
            };
        }

        return new AuthServiceResult
        {
            ErrorCode = AuthErrorCode.Success,
        };
    }

    public async Task<AuthServiceResult> UpdateUserAsync(string userId, UpdateUserDto updateUserDto)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
        {
            return new AuthServiceResult
            {
                ErrorCode = AuthErrorCode.InvalidUser
            };
        }

        user.PhoneNumber = updateUserDto.PhoneNumber ?? user.PhoneNumber;
        user.Name = updateUserDto.Name ?? user.Name;

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            return new AuthServiceResult
            {
                ErrorCode = AuthErrorCode.InvalidData,
                Errors = result.Errors.Select(e => e.Description)
            };
        }

        return new AuthServiceResult
        {
            ErrorCode = AuthErrorCode.Success,
            
        };
    }

    public async Task<AuthServiceResult> GetUsersWithPaginationAsync(PaginationParams paginationParams)
    {
        var users = await _userManager.Users
            .Skip((paginationParams.PageNumber - 1) * paginationParams.PageSize)
            .Take(paginationParams.PageSize)
            .Select(user => new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
            }).ToListAsync();

        if (users.Count == 0)
        {
            return new AuthServiceResult
            {
                ErrorCode = AuthErrorCode.NoUsersFound,
            };
        }

        return new AuthServiceResult
        {
            ErrorCode = AuthErrorCode.Success,
            Result = users
        };
    }

    public async Task<AuthServiceResult> GetUserByEmailAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);

        if (user == null)
        {
            return new AuthServiceResult
            {
                ErrorCode = AuthErrorCode.InvalidUser,
            };
        }

        var userDto = new UserDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber
        };

        return new AuthServiceResult
        {
            ErrorCode = AuthErrorCode.Success,
            Result = user
        };
    }

    public async Task<AuthServiceResult> GetUserByIdAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);

        if (user == null)
        {
            return new AuthServiceResult
            {
                ErrorCode = AuthErrorCode.InvalidUser,
            };
        }

        var userDto = new UserDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber
        };

        return new AuthServiceResult
        {
            ErrorCode = AuthErrorCode.Success,
            Result = userDto
        };
    }

    public async Task<AuthServiceResult> DeleteUserAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
        {
            return new AuthServiceResult
            {
                ErrorCode = AuthErrorCode.NoUsersFound,
            };
        }

        var result = await _userManager.DeleteAsync(user);

        if (!result.Succeeded)
        {
            return new AuthServiceResult
            {
                ErrorCode = AuthErrorCode.Conflict,
                Errors = result.Errors.Select(e => e.Description)
            };
        }

        return new AuthServiceResult
        {
            ErrorCode = AuthErrorCode.Success
        };
    }
}
