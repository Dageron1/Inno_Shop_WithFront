using System.Text.Json.Serialization;

namespace InnoShop.Services.AuthAPI.Models.Dto
{
    public class ResponseDto<TResult>
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public TResult? Result { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object? Errors { get; set; }

        public bool IsSuccess { get; set; } = false;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Message { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Token { get; set; }
        public List<Link> Links { get; set; }
    }
}
