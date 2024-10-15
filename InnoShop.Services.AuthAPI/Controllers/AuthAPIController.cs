using InnoShop.Services.AuthAPI.Models.Dto;
using InnoShop.Services.AuthAPI.Services.Interfaces;
using InnoShop.Services.AuthAPI.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;
using System.Web.Helpers;

namespace InnoShop.Services.AuthAPI.Controllers;

[Route("api/auth")]
[ApiController]
public class AuthApiController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IConfiguration _configuration;
    private readonly ILinkService _linkService;

    public AuthApiController(IAuthService authService, IConfiguration configuration, ILinkService linkService)
    {
        _authService = authService;
        _configuration = configuration;
        _linkService = linkService;
    }

    [HttpPost("users")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ResponseDto<AuthServiceResult>>> Register([FromBody] RegistrationRequestDto requestModel) 
    {
        var emailMessageBuilder = CreateEmailConfirmationMessageBuilder();

        var authServiceResult = await _authService.Register(requestModel, emailMessageBuilder);

        var responseDto = ConvertToResponseDto(authServiceResult);

        if (responseDto.IsSuccess) 
        {
            return StatusCode(201, new ResponseDto<AuthServiceResult>
            {
                Result = responseDto.Result,
                Links = _linkService.GenerateCommonEmailLinks(requestModel.Email),
            });
        }      

        if(authServiceResult.ErrorCode == AuthErrorCode.UserAlreadyExists)
        {
            return Conflict(new ResponseDto<AuthServiceResult>
            {
                Message = responseDto.Message,
                Errors = responseDto.Errors,
                Links = _linkService.GenerateCommonEmailLinks(requestModel.Email),
            });
        }

        return BadRequest(responseDto);
    }

    [HttpPost("users/resend-email-confirmation")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ResponseDto<AuthServiceResult>>> SendEmailConfirmation([FromBody] EmailDto email) 
    {
        var emailMessageBuilder = CreateEmailConfirmationMessageBuilder();

        var authServiceResult = await _authService.SendEmailConfirmationAsync(email.Email, emailMessageBuilder);

        var responseDto = ConvertToResponseDto(authServiceResult);

        if (authServiceResult.IsSuccess)
        {
            return Ok(new ResponseDto<AuthServiceResult>
            {
                Message = responseDto.Message,
                Links = _linkService.GenerateCommonEmailLinks(email.ToString()!),
            });
        }

        return BadRequest(new ResponseDto<AuthServiceResult>
        {
            Message = responseDto.Message,
            Errors = responseDto?.Errors,
            Links = _linkService.GenerateCommonEmailLinks(email.ToString()!),
        });
    }

    [HttpGet("users/confirm-email")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ResponseDto<AuthServiceResult>>> ConfirmEmail(string token)
    {
        var authServiceResult = await _authService.ConfirmEmailAsync(token);

        var responseDto = ConvertToResponseDto(authServiceResult);

        if (authServiceResult.IsSuccess)
        {
            return Ok(responseDto);
        }

        return BadRequest(responseDto);
    }

    [HttpPost("sessions")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ResponseDto<AuthServiceResult>>> Login([FromBody] LoginRequestDto model) 
    {
        var authServiceResult = await _authService.Login(model);

        var responseDto = ConvertToResponseDto(authServiceResult);

        if (authServiceResult.IsSuccess)
        {
            return Ok(responseDto);
        }
        
        if (authServiceResult.ErrorCode == AuthErrorCode.EmailNotConfirmed)
        {
            return StatusCode(403, responseDto);
        }

        return BadRequest(responseDto);
    }

    [Authorize(Roles = Role.Admin)]
    [HttpPost("users/{id}/roles")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ResponseDto<AuthServiceResult>>> AddRoleToUser(Guid id, [FromBody] AddRoleRequestDto requestModel) 
    {
        var authServiceResult = await _authService.AssignRole(id.ToString(), requestModel.Role.ToUpper());

        var responseDto = ConvertToResponseDto(authServiceResult);

        if (authServiceResult.IsSuccess)
        {
            return Ok(responseDto);
        }

        if (authServiceResult.ErrorCode == AuthErrorCode.InvalidUser)
        {
            return BadRequest(responseDto);
        }

        return Conflict(responseDto);
    }

    [HttpPost("users/password/forgot")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ResponseDto<AuthServiceResult>>> ForgotPassword([FromBody] EmailDto model) 
    {
        var emailMessageBuilder = CreatePasswordResetEmail();

        var authServiceResult = await _authService.GeneratePasswordResetTokenAsync(model.Email, emailMessageBuilder);

        var responseDto = ConvertToResponseDto(authServiceResult);

        if (authServiceResult.IsSuccess)
        {
            return Ok(responseDto);
        }

        return BadRequest(responseDto);
    }

    [HttpPost("users/password/reset")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ResponseDto<AuthServiceResult>>> ResetPassword([FromBody] ResetPasswordRequestDto model) 
    {
        if (model == null || string.IsNullOrEmpty(model.Token) || string.IsNullOrEmpty(model.NewPassword))
        {
            return BadRequest(new ResponseDto<AuthServiceResult> { IsSuccess = false, Message = "Invalid request." });
        }

        var authServiceResult = await _authService.ResetPasswordAsync(model.Email, model.Token, model.NewPassword);

        var responseDto = ConvertToResponseDto(authServiceResult);

        if (authServiceResult.IsSuccess)
        {
            return Ok(responseDto);
        }

        return BadRequest(responseDto);
    }

    [Authorize]
    [HttpPost("users/password/change")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ResponseDto<AuthServiceResult>>> ChangePassword([FromBody] ChangePasswordDto model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var authServiceResult = await _authService.ChangePasswordAsync(model, userId);

        var responseDto = ConvertToResponseDto(authServiceResult);

        if (authServiceResult.IsSuccess)
        {
            return Ok(responseDto);
        }

        return BadRequest(responseDto);
    }

    [Authorize]
    [HttpPut("users/{userId}")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ResponseDto<AuthServiceResult>>> UpdateUser(Guid userId, [FromBody] UpdateUserDto model) 
    {
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (User.IsInRole(Role.Admin) || currentUserId == userId.ToString())
        {
            var authServiceResult = await _authService.UpdateUserAsync(userId.ToString(), model);

            var responseDto = ConvertToResponseDto(authServiceResult);

            if (authServiceResult.IsSuccess)
            {
                return Ok(responseDto);
            }

            return BadRequest(responseDto);
        }
        return Forbid();
    }

    [Authorize(Roles = Role.Admin)]
    [HttpGet("users/paginated")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ResponseDto<AuthServiceResult>>> GetUsersWithPagination([FromQuery] PaginationParams paginationParams) 
    {
        var authServiceResult = await _authService.GetUsersWithPaginationAsync(paginationParams);

        var responseDto = ConvertToResponseDto(authServiceResult);

        if (responseDto.IsSuccess)
        {
            return Ok(responseDto);
        }

        return BadRequest(responseDto);
    }

    [Authorize(Roles = Role.Admin)]
    [HttpPost("users/by-email")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ResponseDto<AuthServiceResult>>> GetUserByEmail([FromBody] EmailDto email) 
    {
        var authServiceResult = await _authService.GetUserByEmailAsync(email.Email);

        var responseDto = ConvertToResponseDto(authServiceResult);

        if (responseDto.IsSuccess)
        {
            return Ok(responseDto);
        }

        return BadRequest(responseDto);
    }

    [Authorize]
    [HttpGet("users/{userId}")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ResponseDto<AuthServiceResult>>> GetUserById(Guid userId)
    {
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (User.IsInRole(Role.Admin) || currentUserId == userId.ToString())
        {
            var authServiceResult = await _authService.GetUserByIdAsync(userId.ToString());

            var responseDto = ConvertToResponseDto(authServiceResult);

            if (responseDto.IsSuccess)
            {
                return Ok(responseDto);
            }

            return BadRequest(responseDto);
        }
        return Forbid();
    }

    [Authorize(Roles = Role.Admin)]
    [HttpDelete("users/{userId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ResponseDto<AuthServiceResult>>> DeleteUser(Guid userId) 
    {
        var authServiceResult = await _authService.DeleteUserAsync(userId.ToString());

        var responseDto = ConvertToResponseDto(authServiceResult);

        if (authServiceResult.IsSuccess)
        {
            return NoContent();
        }

        if(authServiceResult.ErrorCode == AuthErrorCode.Conflict)
        {
            return Conflict(responseDto);
        }

        return BadRequest(responseDto);
    }

    private Func<string, string> CreateEmailConfirmationMessageBuilder()
    {
        var mvcHost = _configuration["MvcAppUrl"];
        return (token) =>
        {
            var confirmationLink = $"{mvcHost}/Auth/ConfirmEmail?token={WebUtility.UrlEncode(token)}";
            return $"Please confirm your account by clicking this link: <a href='{confirmationLink}'>this link</a>.";
        };
    }

    private Func<string, string, string> CreatePasswordResetEmail()
    {
        var mvcHost = _configuration["MvcAppUrl"];
        return (token, email) =>
        {
            var confirmationLink = $"{mvcHost}/Auth/ResetPassword?token={WebUtility.UrlEncode(token)}&email={email}";
            return $"Please reset you password by clicking this link: <a href='{confirmationLink}'>this link</a>.";
        };
    }

    private ResponseDto<AuthServiceResult> ConvertToResponseDto(AuthServiceResult authServiceResult) 
    {
        var result = authServiceResult.Result as AuthServiceResult;
        return new ResponseDto<AuthServiceResult> 
        { 
            IsSuccess = authServiceResult.IsSuccess, 
            Token = authServiceResult.Token,
            Errors = authServiceResult.Errors,
            Result = result ?? new AuthServiceResult(),
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
                _ => throw new ArgumentException() } 
        }; 
    }
}
