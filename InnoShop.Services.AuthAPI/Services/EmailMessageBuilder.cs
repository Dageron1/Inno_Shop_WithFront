using InnoShop.Services.AuthAPI.Services.Interfaces;
using System.Net;

namespace InnoShop.Services.AuthAPI.Services
{
    public class EmailMessageBuilder : IEmailMessageBuilder
    {
        private readonly IConfiguration _configuration;

        public EmailMessageBuilder(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string BuildConfirmationMessage(string token)
        {
            var mvcHost = _configuration["MvcAppUrl"];
            var confirmationLink = $"{mvcHost}/Auth/ConfirmEmail?token={WebUtility.UrlEncode(token)}";
            return $"Please confirm your account by clicking this link: <a href='{confirmationLink}'>this link</a>.";
        }

        public string BuildPasswordResetMessage(string token, string email)
        {
            var mvcHost = _configuration["MvcAppUrl"];
            var confirmationLink = $"{mvcHost}/Auth/ResetPassword?token={WebUtility.UrlEncode(token)}&email={email}";
            return $"Please reset you password by clicking this link: <a href='{confirmationLink}'>this link</a>.";
        }
    }
}
