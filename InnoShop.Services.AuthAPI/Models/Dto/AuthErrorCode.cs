namespace InnoShop.Services.AuthAPI.Models.Dto
{
    public enum AuthErrorCode
    {
        Success,
        SavingError,
        InvalidUser,
        EmailNotConfirmed,
        EmailAlreadyConfirmed,
        InvalidEmailOrPassword,
        InvalidCredentials,
        InternalServerError,
        InvalidToken,
        DeletionFailed,
        NoUsersFound,
        UserAlreadyExists,
        BadRequest,
        InvalidData,
        Conflict
    }
}
