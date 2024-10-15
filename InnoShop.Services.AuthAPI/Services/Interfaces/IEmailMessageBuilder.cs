namespace InnoShop.Services.AuthAPI.Services.Interfaces
{
    public interface IEmailMessageBuilder
    {
        string BuildConfirmationMessage(string token);
        string BuildPasswordResetMessage(string token, string email);
    }
}
