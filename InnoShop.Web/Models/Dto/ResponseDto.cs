using System.Text.Json.Serialization;

namespace InnoShop.Web.Models.Dto
{
    public class ResponseDto
    {
        public object? Result { get; set; }

        public object? Errors { get; set; }

        public bool IsSuccess { get; set; } = false;

        public string? Message { get; set; }

        public string? Token { get; set; }
    }
}
