using InnoShop.Services.AuthAPI.Models.Dto;
using InnoShop.Services.AuthAPI.Services.Interfaces;
using InnoShop.Services.AuthAPI.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;

namespace InnoShop.Services.AuthAPI.Controllers;

[Route("api/auth")]
[ApiController]
public class AuthApiController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthApiController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("users")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ResponseDto>> Register([FromBody] RegistrationRequestDto requestModel)
    {
        var authServiceResult = await _authService.Register(requestModel);

        var responseDto = ConvertToResponseDto(authServiceResult);

        if (authServiceResult.ErrorCode == AuthErrorCode.Success)
        {
            return StatusCode(StatusCodes.Status201Created, responseDto);
        }

        if (authServiceResult.ErrorCode == AuthErrorCode.UserAlreadyExists)
        {
            return Conflict(responseDto);
        }

        return BadRequest(responseDto);
    }

    [HttpPost("users/resend-email-confirmation")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ResponseDto>> SendEmailConfirmation([FromBody] EmailDto email)
    {
        var authServiceResult = await _authService.SendEmailConfirmationAsync(email.Email);

        var responseDto = ConvertToResponseDto(authServiceResult);

        if (authServiceResult.ErrorCode == AuthErrorCode.Success)
        {
            return Ok(responseDto);
        }

        return BadRequest(responseDto);
    }

    [HttpGet("users/confirm-email")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ResponseDto>> ConfirmEmail(string token)
    {
        var authServiceResult = await _authService.ConfirmEmailAsync(token);

        var responseDto = ConvertToResponseDto(authServiceResult);

        if (authServiceResult.ErrorCode == AuthErrorCode.Success)
        {
            return Ok(responseDto);
        }

        return BadRequest(responseDto);
    }

    [HttpPost("sessions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ResponseDto>> Login([FromBody] LoginRequestDto model)
    {
        var authServiceResult = await _authService.Login(model);

        var responseDto = ConvertToResponseDto(authServiceResult);

        if (authServiceResult.ErrorCode == AuthErrorCode.Success)
        {
            return Ok(responseDto);
        }

        if (authServiceResult.ErrorCode == AuthErrorCode.EmailNotConfirmed)
        {
            return StatusCode(StatusCodes.Status403Forbidden, responseDto);
        }

        return BadRequest(responseDto);
    }

    [Authorize(Roles = Role.Admin)]
    [HttpPost("users/{id}/roles")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ResponseDto>> AddRoleToUser(Guid userId, [FromBody] AddRoleRequestDto requestModel)
    {
        var authServiceResult = await _authService.AssignRole(userId.ToString(), requestModel.Role.ToUpper());

        var responseDto = ConvertToResponseDto(authServiceResult);

        if (authServiceResult.ErrorCode == AuthErrorCode.Success)
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
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ResponseDto>> ForgotPassword([FromBody] EmailDto model)
    {
        var authServiceResult = await _authService.GeneratePasswordResetTokenAsync(model.Email);

        var responseDto = ConvertToResponseDto(authServiceResult);

        if (authServiceResult.ErrorCode == AuthErrorCode.Success)
        {
            return Ok(responseDto);
        }

        return BadRequest(responseDto);
    }

    [HttpPost("users/password/reset")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ResponseDto>> ResetPassword([FromBody] ResetPasswordRequestDto model)
    {
        var authServiceResult = await _authService.ResetPasswordAsync(model.Email, model.Token, model.NewPassword);

        var responseDto = ConvertToResponseDto(authServiceResult);

        if (authServiceResult.ErrorCode == AuthErrorCode.Success)
        {
            return Ok(responseDto);
        }

        return BadRequest(responseDto);
    }

    [Authorize]
    [HttpPost("users/password/change")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ResponseDto>> ChangePassword([FromBody] ChangePasswordDto model)
    {
        var authServiceResult = await _authService.ChangePasswordAsync(model);

        var responseDto = ConvertToResponseDto(authServiceResult);

        if (authServiceResult.ErrorCode == AuthErrorCode.Success)
        {
            return Ok(responseDto);
        }

        return BadRequest(responseDto);
    }

    [Authorize]
    [HttpPut("users/{userId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ResponseDto>> UpdateUser(Guid userId, [FromBody] UpdateUserDto model)
    {
        var authServiceResult = await _authService.UpdateUserAsync(userId.ToString(), model);

        var responseDto = ConvertToResponseDto(authServiceResult);

        if (authServiceResult.ErrorCode == AuthErrorCode.Success)
        {
            return Ok(responseDto);
        }
        if (authServiceResult.ErrorCode == AuthErrorCode.Forbid)
        {
            return Forbid();
        }

        return BadRequest(responseDto);

    }

    [Authorize(Roles = Role.Admin)]
    [HttpGet("users/paginated")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult<ResponseDto>> GetUsersWithPagination([FromQuery] PaginationParams paginationParams)
    {
        var authServiceResult = await _authService.GetUsersWithPaginationAsync(paginationParams);

        var responseDto = ConvertToResponseDto(authServiceResult);

        if (authServiceResult.ErrorCode == AuthErrorCode.Success)
        {
            return Ok(responseDto);
        }

        return StatusCode(StatusCodes.Status204NoContent, responseDto);
    }

    [Authorize(Roles = Role.Admin)]
    [HttpPost("users/by-email")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ResponseDto>> GetUserByEmail([FromBody] EmailDto email)
    {
        var authServiceResult = await _authService.GetUserByEmailAsync(email.Email);

        var responseDto = ConvertToResponseDto(authServiceResult);

        if (authServiceResult.ErrorCode == AuthErrorCode.Success)
        {
            return Ok(responseDto);
        }

        return BadRequest(responseDto);
    }

    [Authorize]
    [HttpGet("users/{userId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ResponseDto>> GetUserById(Guid userId)
    {
        var authServiceResult = await _authService.GetUserByIdAsync(userId.ToString());

        var responseDto = ConvertToResponseDto(authServiceResult);

        if (authServiceResult.ErrorCode == AuthErrorCode.Success)
        {
            return Ok(responseDto);
        }

        return Forbid();
    }

    [Authorize(Roles = Role.Admin)]
    [HttpDelete("users/{userId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ResponseDto>> DeleteUser(Guid userId)
    {
        var authServiceResult = await _authService.DeleteUserAsync(userId.ToString());

        var responseDto = ConvertToResponseDto(authServiceResult);

        if (authServiceResult.ErrorCode == AuthErrorCode.Success)
        {
            return NoContent();
        }

        if (authServiceResult.ErrorCode == AuthErrorCode.Conflict)
        {
            return Conflict(responseDto);
        }

        return BadRequest(responseDto);
    }

    private ResponseDto ConvertToResponseDto(AuthServiceResult authServiceResult)
    {
        return new ResponseDto
        {
            Result = authServiceResult.Result,
            Token = authServiceResult.Token,
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
                AuthErrorCode.Forbid => "Access denied.",
                AuthErrorCode.Conflict => "Error caused by data conflict.",
                _ => throw new ArgumentException()
            }
        };
    }
}
