using InnoShop.Web.Models.Dto;
using InnoShop.Web.Models.VM;
using InnoShop.Web.Services.Interfaces;
using Newtonsoft.Json;
using System.Net.Http.Json;

namespace InnoShop.Web.Services
{
    public class AuthApiClient : IAuthApiClient
    {
        private readonly HttpClient _httpClient;

        public AuthApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        private async Task<ResponseDto?> SendRequestAsync<TRequest>(string url, TRequest data, HttpMethod method)
        {
            HttpResponseMessage response;

            if (method == HttpMethod.Post)
            {
                response = await _httpClient.PostAsJsonAsync(url, data);
            }
            else if (method == HttpMethod.Put)
            {
                response = await _httpClient.PutAsJsonAsync(url, data);
            }
            else if (method == HttpMethod.Get)
            {
                response = await _httpClient.GetAsync(url);
            }
            else
            {
                throw new NotSupportedException($"HttpMethod {method} is not supported.");
            }

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ResponseDto>();
            }

            var errorResponse = new ResponseDto
            {
                IsSuccess = false,
                Message = response.StatusCode.ToString(),
            };

            var errorContent = await response.Content.ReadAsStringAsync();

            if (!string.IsNullOrEmpty(errorContent))
            {
                try
                {
                    var errorDetails = JsonConvert.DeserializeObject<ValidationErrorResponse>(errorContent);

                    if (errorDetails?.Errors != null)
                    {
                        errorResponse.Errors = errorDetails.Errors;
                    }
                }
                catch (JsonException)
                {
                    errorResponse.Errors = errorContent;
                }
            }
            else
            {
                errorResponse.Errors = "Unknown error occurred.";
            }

            return errorResponse;
        }

        public Task<ResponseDto?> LoginAsync(LoginRequestDto loginRequestDto)
        {
            return SendRequestAsync("/api/auth/sessions", loginRequestDto, HttpMethod.Post);
        }

        public Task<ResponseDto?> RegisterAsync(RegistrationRequestDto registrationRequestDto)
        {
            return SendRequestAsync("/api/auth/users", registrationRequestDto, HttpMethod.Post);
        }

        public Task<ResponseDto?> SendEmailConfirmation(LoginRequestDto loginRequestDto)
        {
            return SendRequestAsync("/api/auth/users/resend-email-confirmation", loginRequestDto, HttpMethod.Post);
        }

        public Task<ResponseDto?> ForgotPassword(LoginRequestDto loginRequestDto)
        {
            return SendRequestAsync("/api/auth/users/password/forgot", loginRequestDto, HttpMethod.Post);
        }

        public Task<ResponseDto?> ResetPassword(ResetPasswordViewModel resetPasswordViewModel)
        {
            return SendRequestAsync("/api/auth/users/password/reset", resetPasswordViewModel, HttpMethod.Post);
        }

        public Task<ResponseDto?> GetUserById(Guid id)
        {
            return SendRequestAsync($"/api/auth/users/{id}", id, HttpMethod.Get);
        }

        public Task<ResponseDto?> ConfirmEmail(string token)
        {
            var url = $"/api/auth/users/confirm-email?token={Uri.EscapeDataString(token)}";
            return SendRequestAsync<object>(url, null, HttpMethod.Get);
        }

        public Task<ResponseDto?> EditAsync(Guid id, UserProfileViewModel userProfileViewModel)
        {
            return SendRequestAsync($"/api/auth/users/{id}", userProfileViewModel, HttpMethod.Put);
        }

        public Task<ResponseDto?> ChangePassword(ChangePasswordViewModel changePasswordViewModel)
        {
            return SendRequestAsync("/api/auth/users/password/change", changePasswordViewModel, HttpMethod.Post);
        }

        public class ValidationErrorResponse
        {
            public Dictionary<string, string[]> Errors { get; set; }
        }
    }
}
