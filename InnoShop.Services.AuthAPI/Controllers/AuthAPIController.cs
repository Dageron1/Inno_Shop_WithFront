using Azure;
using InnoShop.Services.AuthAPI.Models.Dto;
using InnoShop.Services.AuthAPI.Services.Interfaces;
using InnoShop.Services.AuthAPI.Utility;
using InnoShop.Services.AuthAPI.Validators;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Net;
using System.Net.Http;
using System.Security.Claims;

namespace InnoShop.Services.AuthAPI.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthAPIController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthAPIController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("users")]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<ResponseDto>> Register([FromBody] RegistrationRequestDto requestModel) 
        {
            var emailMessageBuilder = CreateEmailConfirmationMessageBuilder();

            var authServiceResult = await _authService.Register(requestModel, emailMessageBuilder);

            var responseDto = ConvertToResponseDto(authServiceResult);

            if (authServiceResult.IsSuccess)
            {
                return Ok(responseDto);
            }

            if(authServiceResult.ErrorCode == AuthErrorCode.UserAlreadyExists)
            {
                return Conflict(responseDto);
            }

            return BadRequest(responseDto);
        }

        [HttpPost("users/resend-email-confirmation")]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ResponseDto>> SendEmailConfirmation([FromBody] EmailDto email) 
        {
            var emailMessageBuilder = CreateEmailConfirmationMessageBuilder();

            var authServiceResult = await _authService.SendEmailConfirmationAsync(email.Email, emailMessageBuilder);

            var responseDto = ConvertToResponseDto(authServiceResult);

            if (authServiceResult.IsSuccess)
            {
                return Ok(responseDto);
            }

            return BadRequest(responseDto);
        }

        [HttpGet("users/confirm-email")]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ResponseDto>> ConfirmEmail(string token)
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
        public async Task<ActionResult<ResponseDto>> Login([FromBody] LoginRequestDto model) 
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
        public async Task<ActionResult<ResponseDto>> AddRoleToUser(Guid id, [FromBody] AddRoleRequestDto requestModel) 
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
        public async Task<ActionResult<ResponseDto>> ForgotPassword([FromBody] EmailDto model) 
        {
            var authServiceResult = await _authService.GeneratePasswordResetTokenAsync(model.Email);

            var responseDto = ConvertToResponseDto(authServiceResult);

            if (authServiceResult.IsSuccess)
            {
                return Ok(responseDto);
            }

            return BadRequest(responseDto);
        }

        [HttpPost("users/password/reset")]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ResponseDto>> ResetPassword([FromBody] ResetPasswordRequestDto model) 
        {
            if (model == null || string.IsNullOrEmpty(model.Token) || string.IsNullOrEmpty(model.NewPassword))
            {
                return BadRequest(new ResponseDto { IsSuccess = false, Message = "Invalid request." });
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
        [HttpPut("users/{userId}")]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ResponseDto>> UpdateUser(Guid userId, [FromBody] UpdateUserDto model) 
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
        public async Task<ActionResult<ResponseDto>> GetUsersWithPagination([FromQuery] PaginationParams paginationParams) 
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
        public async Task<ActionResult<ResponseDto>> GetUserByEmail([FromBody] EmailDto email) 
        {
            var authServiceResult = await _authService.GetUserByEmailAsync(email.Email);

            var responseDto = ConvertToResponseDto(authServiceResult);

            if (responseDto.IsSuccess)
            {
                return Ok(responseDto);
            }

            return BadRequest(responseDto);
        }

        [Authorize(Roles = Role.Admin)]
        [HttpDelete("users/{userId}")]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<ResponseDto>> DeleteUser(Guid userId) 
        {
            var authServiceResult = await _authService.DeleteUserAsync(userId.ToString());

            var responseDto = ConvertToResponseDto(authServiceResult);

            if (authServiceResult.IsSuccess)
            {
                return Ok(responseDto);
            }

            if(authServiceResult.ErrorCode == AuthErrorCode.Conflict)
            {
                return Conflict(responseDto);
            }

            return BadRequest(responseDto);
        }

        private Func<string, string> CreateEmailConfirmationMessageBuilder()
        {
            var request = HttpContext.Request;
            var host = $"{request.Scheme}://{request.Host}";
            return (token) =>
            {
                var confirmationLink = $"{host}/api/auth/users/confirm-email?token={WebUtility.UrlEncode(token)}";
                return $"Please confirm your account by clicking this link: {confirmationLink}";
            };
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
                    _ => throw new ArgumentException() } 
            }; }
    }
}
