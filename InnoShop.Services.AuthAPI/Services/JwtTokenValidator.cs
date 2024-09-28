using InnoShop.Services.AuthAPI.Models;
using InnoShop.Services.AuthAPI.Services.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace InnoShop.Services.AuthAPI.Services
{
    public class JwtTokenValidator : IJwtTokenValidator
    {
        private readonly JwtOptions _jwtOptions;

        public JwtTokenValidator(IOptions<JwtOptions> jwtOptions)
        {
            _jwtOptions = jwtOptions.Value;
        }

        public (string userId, string emailConfirmationToken) ValidateEmailConfirmationJwt(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            var key = Encoding.ASCII.GetBytes(_jwtOptions.Secret);

            try
            {
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                };

                var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

                if (!(validatedToken is JwtSecurityToken jwtToken))
                {
                    return (null, null);
                }

                var userId = principal.Claims.FirstOrDefault(x => x.Type == "userId")?.Value;
                var emailConfirmationToken = principal.Claims.FirstOrDefault(x => x.Type == "emailConfirmationToken")?.Value;

                return (userId, emailConfirmationToken);
            }
            catch
            {
                return (null, null);
            }
        }
    }
}
