using InnoShop.Web.Models.Dto;
using InnoShop.Web.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity.Data;
using InnoShop.Web.Utility;
using Microsoft.AspNetCore.Mvc.Rendering;
using InnoShop.Web.Models.VM;
using System.Reflection;
using Microsoft.AspNetCore.Identity;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json;

namespace InnoShop.Web.Controllers
{
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;
        private readonly ITokenService _tokenService;

        public AuthController(IAuthService authService, ITokenService tokenService)
        {
            _authService = authService;
            _tokenService = tokenService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var responseDto = await _authService.GetUserByIdAsync(userId);

            var userProfile = JsonConvert.DeserializeObject<UserProfileViewModel>(responseDto.Result.ToString());

            return View(userProfile);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> EditProfile(UserProfileViewModel userProfileViewModel)
        {
            // change logic to EditProfile
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var responseDto = await _authService.EditAsync(userId, userProfileViewModel);

            if (responseDto.IsSuccess)
            {
                TempData["success"] = "Updated successfully";
                return RedirectToAction(nameof(Profile));
            }
            else
            {
                TempData["error"] = responseDto.Errors.ToString();
                return RedirectToAction(nameof(Profile));
            }  
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel changePasswordViewModel)
        {
            // change logic to ChangePassword
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var responseDto = await _authService.ChangePassword(changePasswordViewModel);

            if (responseDto.IsSuccess)
            {
                TempData["success"] = "Password changed";
                return RedirectToAction(nameof(Profile));
            }
            else
            {
                TempData["error"] = responseDto.Errors.ToString();
                return RedirectToAction(nameof(Profile));
            }
        }

        [HttpGet]
        public IActionResult Login()
        {
            LoginRequestDto loginRequestDto = new();
            return View(loginRequestDto);
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginRequestDto loginRequest)
        {
            // использовать TypedHttpClient
            ResponseDto responseDto = await _authService.LoginAsync(loginRequest);

            if (responseDto.IsSuccess)
            {
                var claimsPrincipal = _tokenService.CreateClaimsPrincipal(responseDto.Token, loginRequest.Email);

                await _authService.SignInUserAsync(claimsPrincipal, responseDto.Token);

                TempData["success"] = responseDto.Message;

                return RedirectToAction("Index", "Home");
            }
            else if (responseDto.Message == "Forbidden")
            {
                TempData["error"] = "Please confirm your email address to log in.";
                return RedirectToAction(nameof(Login));
            }
            else if (responseDto?.Errors is Dictionary<string, string[]> errorsDict)
            {
                AddErrorsToModelState(errorsDict);
            }
            return View(loginRequest);
        }

        [HttpGet]
        public IActionResult Register()
        {
            RegistrationRequestDto registrationRequestDto = new();
            return View(registrationRequestDto);
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegistrationRequestDto registrationRequestDto)
        {
            ResponseDto? responseDto = await _authService.RegisterAsync(registrationRequestDto);

            if (responseDto != null && responseDto.IsSuccess)
            {
                TempData["success"] = "Registration Successful";
                return RedirectToAction(nameof(Login));
            }
            else if (responseDto?.Errors is Dictionary<string, string[]> errorsDict)
            {
                AddErrorsToModelState(errorsDict);
            }
            return View(registrationRequestDto);
        }

        [HttpPost]
        public async Task<IActionResult> SendEmailConfirmation(LoginRequestDto loginRequestDto)
        {
            ResponseDto? responseDto = await _authService.SendEmailConfirmation(loginRequestDto);

            if (responseDto != null && responseDto.IsSuccess)
            {
                TempData["success"] = "A confirmation email has been sent.";
                return RedirectToAction(nameof(Login));
            }
            else
            {
                TempData["error"] = responseDto?.Message;
            }
            return RedirectToAction(nameof(Login));
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(LoginRequestDto loginRequestDto)
        {
            ResponseDto? responseDto = await _authService.ForgotPassword(loginRequestDto);

            if (responseDto != null && responseDto.IsSuccess)
            {
                TempData["success"] = "A password recovery email has been sent.";
                return RedirectToAction(nameof(Login));
            }
            else
            {
                TempData["error"] = responseDto?.Message;
            }
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult ResetPassword(string token, string email)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
            {
                TempData["Error"] = "Invalid password reset token.";
                return RedirectToAction("Login");
            }

            var model = new ResetPasswordViewModel
            {
                Token = token,
                Email = email
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel resetPasswordViewModel)
        {
            if (!ModelState.IsValid)
            {
                return View(resetPasswordViewModel);
            }

            var response = await _authService.ResetPassword(resetPasswordViewModel);

            if (response.IsSuccess)
            {
                TempData["Success"] = "Password has been reset successfully.";
                return RedirectToAction("Login");
            }

            TempData["Error"] = response.Message;
            return View(resetPasswordViewModel);
        }

        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string token)
        {
            var response = await _authService.ConfirmEmail(token);

            if (response.IsSuccess)
            {
                TempData["Success"] = "Email was confirmed successfully.";
                return RedirectToAction("Login");
            }
            TempData["Error"] = response.Message;
            return RedirectToAction("Login");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Auth");
        }

        //private async Task SignInUser(ClaimsPrincipal claimsPrincipal, string token)
        //{
        //    var authProperties = new AuthenticationProperties
        //    {
        //        IsPersistent = true,
        //        ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(60),
        //        Items = { { "Token", token } }
        //    };

        //    await HttpContext.SignInAsync(
        //        CookieAuthenticationDefaults.AuthenticationScheme,
        //        claimsPrincipal,
        //        authProperties
        //    );
        //}

        private void AddErrorsToModelState(Dictionary<string, string[]> errors)
        {
            foreach (var fieldErrors in errors)
            {
                var fieldName = fieldErrors.Key;
                var errorMessages = fieldErrors.Value;

                foreach (var error in errorMessages)
                {
                    ModelState.AddModelError(fieldName, error);
                }
            }
        }
    }
}
