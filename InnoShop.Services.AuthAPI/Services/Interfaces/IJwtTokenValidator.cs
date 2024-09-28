namespace InnoShop.Services.AuthAPI.Services.Interfaces
{
    public interface IJwtTokenValidator
    {
        (string userId, string emailConfirmationToken) ValidateEmailConfirmationJwt(string token);
    }
}
