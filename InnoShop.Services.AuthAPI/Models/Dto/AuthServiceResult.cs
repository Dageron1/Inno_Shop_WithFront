using System.Text.Json.Serialization;

namespace InnoShop.Services.AuthAPI.Models.Dto
{
    public class AuthServiceResult
    {
        public AuthErrorCode ErrorCode { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Token { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object? Errors { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object? Result { get; set; }
        // если значение свойства ErrorCode равно AuthErrorCode.Success, то IsSuccess возвращает true;
        public bool IsSuccess => ErrorCode == AuthErrorCode.Success;
    }
}
